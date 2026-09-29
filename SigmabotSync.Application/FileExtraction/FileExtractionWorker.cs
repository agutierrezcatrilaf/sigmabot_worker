using SigmabotSync.Application.Common;
using SigmabotSync.Domain.Config;
using SigmabotSync.Domain.Execution;
using SigmabotSync.Domain.Models.Extraction;
using SigmabotSync.Domain.Ports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SigmabotSync.Application.FileExtraction
{
    /// <summary>
    /// Worker para extracción de archivos de documentos desde Aconex
    /// </summary>
    public class FileExtractionWorker : IDisposable
    {
        private const int MaxConcurrentDownloads = 6;

        // Campos mínimos que FileExtraction necesita siempre para funcionar,
        // independientemente de lo que venga en TrabajosConfiguracion.CamposConsulta.
        private static readonly string[] RequiredReturnFields = new[]
        {
            "docno",
            "TipoDeDocumento_singleSelect",
            "filename",
            "trackingid",
            "versionnumber"
        };

        private readonly FileExtractionConfig _config;
        private readonly IAconexRegisterSearchPort _searchPort;
        private readonly IAconexRegisterDocumentContentPort _contentPort;

        private int _countSaved;
        private int _countOmittedNoDocument;  // sin filename o documento vacio (CANNOT_DOWNLOAD_EMPTY_DOCUMENT)
        private int _countOmittedAlreadyExists;
        private int _countErrors;

        /// <summary>Contadores del último <see cref="ProcessAllPagesAsync"/> completado.</summary>
        public FileExtractionResumen LastRunSummary { get; private set; }

        public event Action<int, int> OnProgress;
        /// <summary>Estado del worker. Segundo parámetro: 0=Info, 1=Debug.</summary>
        public event Action<string, int> OnStatus;

        private enum FileDownloadResult { Saved, Omitted, Error }

        public FileExtractionWorker(
            FileExtractionConfig config,
            IAconexRegisterSearchPort searchPort,
            IAconexRegisterDocumentContentPort contentPort)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _searchPort = searchPort ?? throw new ArgumentNullException(nameof(searchPort));
            _contentPort = contentPort ?? throw new ArgumentNullException(nameof(contentPort));
        }

        /// <summary>
        /// Procesa todas las páginas de documentos
        /// </summary>
        public async Task ProcessAllPagesAsync()
        {
            Action<string, int> log = (msg, nivel) => OnStatus?.Invoke(msg, nivel);

            SyncLog.Info(log, "Obteniendo información de páginas...");

                // Obtener primera página para conocer el total
                var firstPage = await GetPageAsync(1, log);

                if (firstPage == null)
                {
                    LogErrorTrabajo(log, "no se pudo obtener la página 1 del registro", null);
                    LastRunSummary = BuildRunSummary(0);
                    return;
                }

                int totalPages = firstPage.totalNumberOfPages;
                long totalDocuments = firstPage.totalResultsCount;

                SyncLog.Info(log, $"Total de documentos: {totalDocuments} en {totalPages} páginas");

                if (totalPages == 0)
                {
                    SyncLog.Info(log, "No hay documentos para procesar");
                    LastRunSummary = BuildRunSummary(0);
                    return;
                }

                _countSaved = 0;
                _countOmittedNoDocument = 0;
                _countOmittedAlreadyExists = 0;
                _countErrors = 0;

                int processedPages = 0;
                long processedDocuments = 0;

                for (int page = 1; page <= totalPages; page++)
                {
                    SyncLog.Debug(log, $"Procesando página {page} de {totalPages}...");

                    Rootobject pageData = page == 1
                        ? firstPage
                        : await GetPageAsync(page, log);

                    if (pageData != null && pageData.searchResults != null)
                    {
                        processedDocuments += pageData.searchResults.Count;

                        // Descargas en paralelo con límite de concurrencia para no saturar Aconex
                        var semaphore = new SemaphoreSlim(MaxConcurrentDownloads);
                        var downloadTasks = pageData.searchResults.Select(async doc =>
                        {
                            await semaphore.WaitAsync();
                            try
                            {
                                await ProcessDocumentAsync(doc, log);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        });
                        await Task.WhenAll(downloadTasks);

                        processedPages++;
                    }

                    OnProgress?.Invoke(page, totalPages);
                }

                int totalOmitted = _countOmittedNoDocument + _countOmittedAlreadyExists;
                string projectFolder = string.IsNullOrWhiteSpace(_config.ProjectName) ? _config.ProjectId : _config.ProjectName;
                projectFolder = string.Join("_", (projectFolder ?? "").Split(Path.GetInvalidFileNameChars()));
                string destinoArchivos = Path.Combine(_config.BasePath ?? "", projectFolder);

                SyncLog.Info(log,
                    $"Completado: páginas={processedPages}, procesados={processedDocuments}, " +
                    $"guardados={_countSaved}, errores={_countErrors}, " +
                    $"omitidos={totalOmitted} (sin archivo={_countOmittedNoDocument}, ya existían={_countOmittedAlreadyExists})");
                SyncLog.Info(log,
                    $"Destino archivos: {destinoArchivos}  (subcarpetas TipoDocumento\\DocNo\\Version)");
                LastRunSummary = BuildRunSummary(processedDocuments);
        }

        /// <summary>
        /// Obtiene una página específica de documentos
        /// </summary>
        private async Task<Rootobject> GetPageAsync(int pageNumber, Action<string, int> log)
        {
            string baseUrl = string.IsNullOrWhiteSpace(_config.AconexBaseUrl) ? "https://us1.aconex.com" : _config.AconexBaseUrl.TrimEnd('/');

            // Respetar CamposConsulta de TrabajosConfiguracion (_config.ReturnFields),
            // pero garantizando siempre los mínimos que FileExtraction necesita.
            var fields = _config.ReturnFields ?? new List<string>();
            foreach (var campo in RequiredReturnFields)
            {
                if (!fields.Contains(campo, StringComparer.OrdinalIgnoreCase))
                    fields.Add(campo);
            }

            try
            {
                return await Utilities.EjecutarConReintentosAsync(
                    async () =>
                    {
                        var result = await _searchPort.SearchRegisterPageAsync(
                            baseUrl,
                            _config.ProjectId,
                            _config.OrgId,
                            _config.UserId,
                            _config.AuthorizationHeader,
                            fields,
                            _config.ResultSize,
                            pageNumber,
                            throwIfNotSuccess: true,
                            CancellationToken.None).ConfigureAwait(false);
                        return result.Page;
                    },
                    $"FileExtraction: Error al obtener página {pageNumber}"
                );
            }
            catch (Exception ex)
            {
                LogErrorTrabajo(log, "no se pudo obtener la página " + pageNumber + " del registro", ex?.Message);
                throw;
            }
        }

        /// <summary>
        /// Procesa un documento individual y descarga su archivo. Devuelve el resultado para el resumen.
        /// </summary>
        private async Task<FileDownloadResult> ProcessDocumentAsync(Searchresult document, Action<string, int> log)
        {
            try
            {
                return await DownloadDocumentFileAsync(document, log);
            }
            catch (Exception ex)
            {
                LogErrorDocumento(
                    log,
                    document?.DocumentNumber,
                    document?.Id.ToString(),
                    null,
                    "no se pudo descargar el archivo",
                    null,
                    ex?.Message);
                Interlocked.Increment(ref _countErrors);
                return FileDownloadResult.Error;
            }
        }

        /// <summary>
        /// Descarga el archivo de un documento desde Aconex. Devuelve Saved, Omitted o Error (errores se registran en log).
        /// </summary>
        private async Task<FileDownloadResult> DownloadDocumentFileAsync(Searchresult document, Action<string, int> log)
        {
            string documentId = document?.Id.ToString() ?? "?";
            string documentNumber = document?.DocumentNumber ?? "?";
            string fileName = null;
            string filePath = null;
            try
            {
                string version = document.GetDynamicValue("versionNumber") ?? "0";
                string documentType = GetProjectFieldValue(document, "TipoDeDocumento_singleSelect");

                string filenameFromMeta = document.GetDynamicValue("filename")?.ToString();
                if (string.IsNullOrWhiteSpace(filenameFromMeta))
                {
                    Interlocked.Increment(ref _countOmittedNoDocument);
                    SyncLog.Debug(log, $"DocNo={documentNumber} Id={documentId}: omitido (sin filename)");
                    return FileDownloadResult.Omitted;
                }

                string folderName = documentNumber;
                if (!string.IsNullOrEmpty(folderName) && folderName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    folderName = folderName.Substring(0, folderName.Length - 4);

                string projectFolder = string.IsNullOrWhiteSpace(_config.ProjectName) ? _config.ProjectId : _config.ProjectName;
                projectFolder = string.Join("_", projectFolder.Split(Path.GetInvalidFileNameChars()));
                string documentTypeFolder = string.IsNullOrWhiteSpace(documentType) ? "SinTipoDocumento" : documentType.Trim();
                documentTypeFolder = string.Join("_", documentTypeFolder.Split(Path.GetInvalidFileNameChars()));

                string documentPath = Path.Combine(
                    _config.BasePath,
                    projectFolder,
                    documentTypeFolder,
                    folderName,
                    version
                );

                fileName = string.Join("_", filenameFromMeta.Split(Path.GetInvalidFileNameChars()));
                filePath = Path.Combine(documentPath, fileName);

                if (File.Exists(filePath))
                {
                    Interlocked.Increment(ref _countOmittedAlreadyExists);
                    SyncLog.Debug(log, $"DocNo={documentNumber} Id={documentId}: omitido (ya existía) {fileName}");
                    return FileDownloadResult.Omitted;
                }

                Directory.CreateDirectory(documentPath);

                string baseUrl = string.IsNullOrWhiteSpace(_config.AconexBaseUrl) ? "https://us1.aconex.com" : _config.AconexBaseUrl.TrimEnd('/');

                var result = await _contentPort.DownloadToFileAsync(
                    baseUrl,
                    _config.ProjectId,
                    documentId,
                    filePath,
                    _config.AuthorizationHeader,
                    CancellationToken.None);

                switch (result.Status)
                {
                    case AconexRegisterDocumentDownloadStatus.Saved:
                        Interlocked.Increment(ref _countSaved);
                        SyncLog.Debug(log, $"OK DocNo={documentNumber} Id={documentId} archivo={fileName}");
                        return FileDownloadResult.Saved;
                    case AconexRegisterDocumentDownloadStatus.OmittedEmptyDocument:
                        Interlocked.Increment(ref _countOmittedNoDocument);
                        SyncLog.Debug(log, $"DocNo={documentNumber} Id={documentId}: omitido (documento vacío)");
                        return FileDownloadResult.Omitted;
                    default:
                        LogErrorDocumento(
                            log,
                            documentNumber,
                            documentId,
                            fileName,
                            FormatShortDownloadError(result.Message),
                            filePath,
                            result.Message ?? result.Status.ToString());
                        Interlocked.Increment(ref _countErrors);
                        return FileDownloadResult.Error;
                }
            }
            catch (Exception ex)
            {
                LogErrorDocumento(
                    log,
                    documentNumber,
                    documentId,
                    fileName,
                    FormatShortDownloadError(ex?.Message),
                    filePath,
                    ex?.Message);
                Interlocked.Increment(ref _countErrors);
                return FileDownloadResult.Error;
            }
        }

        private static string FormatShortDownloadError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "no se pudo descargar el archivo";
            if (message.IndexOf("Timeout", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("cancelación", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("canceled", StringComparison.OrdinalIgnoreCase) >= 0)
                return "se agotó el tiempo de descarga";
            if (message.IndexOf("CANNOT_DOWNLOAD_EMPTY_DOCUMENT", StringComparison.OrdinalIgnoreCase) >= 0)
                return "documento sin archivo en Aconex";
            if (message.IndexOf("401", StringComparison.Ordinal) >= 0 ||
                message.IndexOf("403", StringComparison.Ordinal) >= 0 ||
                message.IndexOf("Unauthorized", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("Forbidden", StringComparison.OrdinalIgnoreCase) >= 0)
                return "sin autorización para descargar";
            return "no se pudo descargar el archivo";
        }

        private static void LogErrorDocumento(
            Action<string, int> log,
            string docNo,
            string documentId,
            string archivo,
            string motivo,
            string ruta,
            string detalle)
        {
            string doc = string.IsNullOrWhiteSpace(docNo) ? "?" : docNo.Trim();
            string id = string.IsNullOrWhiteSpace(documentId) ? "?" : documentId.Trim();
            string archivoTxt = string.IsNullOrWhiteSpace(archivo) ? "?" : archivo.Trim();
            string rutaTxt = string.IsNullOrWhiteSpace(ruta) ? "(sin ruta)" : ruta.Trim();
            SyncLog.Info(log, $"ERROR DocNo={doc} Id={id} archivo={archivoTxt}: {motivo}. Ruta={rutaTxt}");
            string extra = ExtraerDetalleParaLog(detalle, motivo);
            if (!string.IsNullOrWhiteSpace(extra))
                SyncLog.Info(log, "  Detalle: " + extra);
        }

        private static void LogErrorTrabajo(Action<string, int> log, string motivo, string detalle)
        {
            SyncLog.Info(log, "ERROR: " + motivo);
            string extra = ExtraerDetalleParaLog(detalle, motivo);
            if (!string.IsNullOrWhiteSpace(extra))
                SyncLog.Info(log, "  Detalle: " + extra);
        }

        private static string ExtraerDetalleParaLog(string message, string motivo)
        {
            if (string.IsNullOrWhiteSpace(message))
                return null;

            string detalle = LimpiarTextoLog(message);
            if (string.IsNullOrWhiteSpace(detalle))
                return null;
            if (!string.IsNullOrWhiteSpace(motivo)
                && (string.Equals(detalle, motivo, StringComparison.OrdinalIgnoreCase)
                    || detalle.StartsWith(motivo, StringComparison.OrdinalIgnoreCase)
                    || motivo.StartsWith(detalle, StringComparison.OrdinalIgnoreCase)))
                return null;

            return TruncateForLog(detalle, 300);
        }

        private static string LimpiarTextoLog(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            var sb = new StringBuilder(text.Length);
            bool inTag = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '<')
                {
                    inTag = true;
                    continue;
                }
                if (c == '>')
                {
                    inTag = false;
                    sb.Append(' ');
                    continue;
                }
                if (!inTag)
                    sb.Append(c);
            }

            string limpio = sb.ToString().Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
            while (limpio.IndexOf("  ", StringComparison.Ordinal) >= 0)
                limpio = limpio.Replace("  ", " ");
            return limpio.Trim();
        }

        private FileExtractionResumen BuildRunSummary(long totalProcesados)
        {
            int totalOmitted = _countOmittedNoDocument + _countOmittedAlreadyExists;
            return new FileExtractionResumen
            {
                TotalProcesados = totalProcesados,
                Guardados = _countSaved,
                Omitidos = totalOmitted,
                OmitidosSinDocumento = _countOmittedNoDocument,
                OmitidosYaExistian = _countOmittedAlreadyExists,
                Errores = _countErrors
            };
        }

        private static string TruncateForLog(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max)
                return text ?? "";
            return text.Substring(0, max) + "...";
        }

        /// <summary>
        /// Libera recursos
        /// </summary>
        public void Dispose()
        {
            (_searchPort as IDisposable)?.Dispose();
            (_contentPort as IDisposable)?.Dispose();
        }

        private static string GetProjectFieldValue(Searchresult document, string fieldName)
        {
            if (document?.ProjectFields == null || string.IsNullOrWhiteSpace(fieldName))
                return null;

            var field = document.ProjectFields.FirstOrDefault(p =>
                string.Equals(p.Name, fieldName, StringComparison.OrdinalIgnoreCase));

            return field?.Value;
        }
    }
}
