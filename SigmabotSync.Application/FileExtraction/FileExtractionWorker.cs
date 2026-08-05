using SigmabotSync.Application.Common;
using SigmabotSync.Domain.Config;
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

        public event Action<int, int> OnProgress;
        public event Action<string> OnStatus;

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
            try
            {
                OnStatus?.Invoke("Obteniendo información de páginas...");

                // Obtener primera página para conocer el total
                var firstPage = await GetPageAsync(1);
                
                if (firstPage == null)
                {
                    OnStatus?.Invoke("No se pudo obtener la primera página");
                    return;
                }

                int totalPages = firstPage.totalNumberOfPages;
                long totalDocuments = firstPage.totalResultsCount;

                OnStatus?.Invoke($"Total de documentos: {totalDocuments} en {totalPages} páginas");

                if (totalPages == 0)
                {
                    OnStatus?.Invoke("No hay documentos para procesar");
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
                    OnStatus?.Invoke($"Procesando página {page} de {totalPages}...");

                    Rootobject pageData = page == 1 
                        ? firstPage 
                        : await GetPageAsync(page);

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
                                await ProcessDocumentAsync(doc);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        });
                        await Task.WhenAll(downloadTasks);

                        processedPages++;
                    }

                    int progress = (int)((page * 100) / totalPages);
                    OnProgress?.Invoke(page, totalPages);
                }

                OnStatus?.Invoke($"Proceso completado: {processedPages} páginas, {processedDocuments} documentos procesados");
                int totalOmitted = _countOmittedNoDocument + _countOmittedAlreadyExists;
                Utilities.Wlog($"FileExtraction resumen: Total procesados={processedDocuments}, Guardados={_countSaved}, Omitidos={totalOmitted} (sin documento/archivo={_countOmittedNoDocument}, ya existían={_countOmittedAlreadyExists}), Errores={_countErrors}", 0);
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"FileExtraction: ERROR en ProcessAllPagesAsync: {ex.Message}", 0);
                OnStatus?.Invoke($"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene una página específica de documentos
        /// </summary>
        private async Task<Rootobject> GetPageAsync(int pageNumber)
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
                Utilities.Wlog($"FileExtraction: ERROR en GetPageAsync página {pageNumber}: {ex.Message}", 0);
                throw;
            }
        }

        /// <summary>
        /// Procesa un documento individual y descarga su archivo. Devuelve el resultado para el resumen.
        /// </summary>
        private async Task<FileDownloadResult> ProcessDocumentAsync(Searchresult document)
        {
            try
            {
                return await DownloadDocumentFileAsync(document);
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"FileExtraction: ERROR procesando documento {document.Id}: {ex.Message}", 0);
                Interlocked.Increment(ref _countErrors);
                return FileDownloadResult.Error;
            }
        }

        /// <summary>
        /// Descarga el archivo de un documento desde Aconex. Devuelve Saved, Omitted o Error (errores se registran en log).
        /// </summary>
        private async Task<FileDownloadResult> DownloadDocumentFileAsync(Searchresult document)
        {
            try
            {
                string documentId = document.Id.ToString();
                string version = document.GetDynamicValue("versionNumber") ?? "0";
                string documentNumber = document.DocumentNumber ?? "";
                string documentType = GetProjectFieldValue(document, "TipoDeDocumento_singleSelect");

                string filenameFromMeta = document.GetDynamicValue("filename")?.ToString();
                if (string.IsNullOrWhiteSpace(filenameFromMeta))
                {
                    Interlocked.Increment(ref _countOmittedNoDocument);
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

                string fileName = string.Join("_", filenameFromMeta.Split(Path.GetInvalidFileNameChars()));
                string filePath = Path.Combine(documentPath, fileName);

                if (File.Exists(filePath))
                {
                    Interlocked.Increment(ref _countOmittedAlreadyExists);
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
                        return FileDownloadResult.Saved;
                    case AconexRegisterDocumentDownloadStatus.OmittedEmptyDocument:
                        Interlocked.Increment(ref _countOmittedNoDocument);
                        return FileDownloadResult.Omitted;
                    default:
                        if (!string.IsNullOrEmpty(result.Message))
                        {
                            if (result.Message.IndexOf("Timeout", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                result.Message.IndexOf("cancelación", StringComparison.OrdinalIgnoreCase) >= 0)
                                Utilities.Wlog($"FileExtraction: Timeout o cancelación al descargar documento {document.Id} (DocNo={document.DocumentNumber}). Se omite y se continúa.", 0);
                            else
                                Utilities.Wlog($"FileExtraction: ERROR descargando archivo del documento {document.Id}: {result.Message}", 0);
                        }
                        else
                            Utilities.Wlog($"FileExtraction: ERROR descargando archivo del documento {document.Id}", 0);
                        Interlocked.Increment(ref _countErrors);
                        return FileDownloadResult.Error;
                }
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"FileExtraction: ERROR descargando archivo del documento {document.Id}: {ex.Message}", 0);
                Interlocked.Increment(ref _countErrors);
                return FileDownloadResult.Error;
            }
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
