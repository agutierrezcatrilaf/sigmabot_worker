using SigmabotSync.Application.Common;
using SigmabotSync.Domain.Config;
using SigmabotSync.Domain.Configuration;
using SigmabotSync.Domain.Entities;
using SigmabotSync.Domain.Ports;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SigmabotSync.Application.FileExtraction
{
    /// <summary>
    /// Worker para FileUploadWithMetadata (DataLake): <c>DocumentosMetadata</c> + <c>DocumentosPath</c> → Register Document en Aconex.
    /// </summary>
    public class FileUploadWithMetadataWorker
    {
        /// <summary>
        /// Si es true, no se envía <c>DocumentNumber</c> en el XML y se envía <c>AutoNumber</c>=true para que Aconex asigne el número.
        /// Más adelante puede enlazarse a <c>TrabajoConfiguracion</c>.
        /// </summary>
        private const bool RegisterDocumentUseAconexAutoNumber = true;

        /// <summary>Valor por defecto de <c>TipoDocumento</c> → <c>TipoDeDocumento_singleSelect</c> si la fila no trae dato.</summary>
        private const string DefaultTipoDeDocumentoSingleSelectValue = "Certificado";

        /// <summary>Nombre en <c>Doctype</c>/<c>TiposDocumentos</c> para <c>DocumentTypeId</c> si la fila no trae <c>doctype</c>.</summary>
        private const string DefaultDocumentTypeName = "Documento Interno";

        /// <summary>Autor Aconex (<c>Author</c>) si la fila no trae <c>Author</c>/<c>CreadoPor</c>. Alineado con default SQL de DocumentosMetadata.</summary>
        private const string DefaultAuthorName = "SALFAMontajes";

        private const string XmlNameTipoDeDocumentoSingleSelect = "TipoDeDocumento_singleSelect";

        private readonly TrabajoConfiguracion _trabajoConfig;
        private readonly Credencial _credAconex;
        private readonly Credencial _credBd;
        private readonly FileExtractionConfig _aconexConfig;
        private readonly IAconexRegisterWritePort _registerWritePort;

        public event Action<int, int> OnProgress;
        public event Action<string> OnStatus;

        public FileUploadWithMetadataWorker(
            TrabajoConfiguracion trabajoConfig,
            Credencial credAconex,
            Credencial credBd,
            IAconexRegisterWritePort registerWritePort)
        {
            _trabajoConfig = trabajoConfig ?? throw new ArgumentNullException(nameof(trabajoConfig));
            _credAconex = credAconex ?? throw new ArgumentNullException(nameof(credAconex));
            _credBd = credBd ?? throw new ArgumentNullException(nameof(credBd));
            _registerWritePort = registerWritePort ?? throw new ArgumentNullException(nameof(registerWritePort));
            _aconexConfig = FileExtractionConfig.FromCredencial(credAconex, trabajoConfig.IdProyecto ?? "", null);
        }

        /// <summary>Columnas de proyecto DataLake → elemento XML <c>*_singleSelect</c> en Register Document.</summary>
        private static readonly (string ColumnaMetadata, string XmlSingleSelect)[] DataLakeProjectFieldMap =
        {
            ("CWA", "Cwa_singleSelect"),
            ("CWP", "Cwp_singleSelect"),
            ("EWP", "Ewp_singleSelect"),
            ("PWP", "Pwp_singleSelect"),
            ("CMA", "Cma_singleSelect"),
            ("Discipline", "Discipline_singleSelect"),
            ("TipoDocumento", "TipoDeDocumento_singleSelect"),
            ("Proceso", "Proceso_singleSelect"),
            ("EstatusBim", "EstatusBim_singleSelect"),
        };

        /// <summary>Columnas internas del JOIN DataLake que no deben mapearse a identificadores XML de Aconex.</summary>
        private static readonly HashSet<string> InternalDataLakeColumnNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Id", "Procesado", "PathFisico", "HashArchivo", "Size", "Extension", "PathId",
                "CreadoEn", "DocumentoId", "ACXProjectId", "NumeroTransmittal"
            };

        public async Task RunAsync()
        {
            string connectionStringBd = _credBd.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionStringBd))
            {
                throw new InvalidOperationException("FileUploadWithMetadata requiere CredencialBD con Servidor y BaseDatos configurados.");
            }

            string tablaMetadata = FileUploadWithMetadataDefaults.ResolverTablaMetadata(_trabajoConfig.TablaMetadata);
            string tablaPaths = FileUploadWithMetadataDefaults.ResolverTablaPaths(_trabajoConfig.TablaPaths);

            if (!string.Equals(tablaMetadata, FileUploadWithMetadataDefaults.TablaMetadata, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(tablaPaths, FileUploadWithMetadataDefaults.TablaPaths, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "FileUploadWithMetadata solo admite TablaMetadata="
                    + FileUploadWithMetadataDefaults.TablaMetadata
                    + " y TablaPaths="
                    + FileUploadWithMetadataDefaults.TablaPaths + ".");
            }

            OnStatus?.Invoke($"Leyendo {tablaMetadata} + {tablaPaths}...");
            DataTable metadata = LeerMetadataConPaths(connectionStringBd, tablaMetadata, tablaPaths);
            if (metadata == null || metadata.Rows.Count == 0)
            {
                OnStatus?.Invoke("No hay registros en la tabla de metadata.");
                return;
            }

            string columnaRutaArchivo = ResolverColumnaPathFisico(metadata);
            if (columnaRutaArchivo == null)
            {
                throw new InvalidOperationException(
                    "El JOIN metadata/paths debe incluir columna PathFisico. Columnas: "
                    + string.Join(", ", metadata.Columns.Cast<DataColumn>().Select(c => c.ColumnName)));
            }

            string columnaProcesado = ResolverColumnaProcesado(metadata);
            string columnaId = ResolverColumnaId(metadata);
            var idsProcesadosExitosamente = new List<string>();

            int total = metadata.Rows.Count;
            int procesados = 0;
            int enviados = 0;
            int omitidosYaProcesados = 0;

            OnStatus?.Invoke($"Procesando {total} registro(s) de metadata...");

            OnStatus?.Invoke("Obteniendo schema Register Document desde Aconex...");
            AconexRegisterSchemaSnapshot registerSchema = await ObtenerSchemaRegistroAconexAsync();

            OnStatus?.Invoke("Cargando TiposDocumentos y EstatusDocumentos en memoria...");
            (IReadOnlyDictionary<string, string> mapTipos, IReadOnlyDictionary<string, string> mapEstatus) =
                CargarMapasTiposYEstatusDocumentos(connectionStringBd);

            for (int i = 0; i < metadata.Rows.Count; i++)
            {
                DataRow row = metadata.Rows[i];
                if (FilaYaProcesada(row, metadata.Columns, columnaProcesado))
                {
                    string refArchivo = ObtenerReferenciaArchivoFila(row, metadata.Columns, columnaRutaArchivo);
                    Utilities.Wlog($"FileUploadWithMetadata: Fila {i + 1} ya procesada (Procesado=1), se omite. Ref={refArchivo}", 1);
                    omitidosYaProcesados++;
                    procesados++;
                    OnProgress?.Invoke(procesados, total);
                    continue;
                }

                string filePath = ResolverRutaArchivoDesdePathFisico(row, metadata.Columns, columnaRutaArchivo);
                string refNom = ObtenerReferenciaArchivoFila(row, metadata.Columns, columnaRutaArchivo);

                if (string.IsNullOrEmpty(filePath))
                {
                    string msgArchivo = $"Fila {i + 1}, ref={refNom}: archivo no encontrado.";
                    Utilities.Wlog($"FileUploadWithMetadata: {msgArchivo}", 1);
                    throw new InvalidOperationException(msgArchivo);
                }

                try
                {
                    await EnviarDocumentoAconexAsync(filePath, row, metadata.Columns, registerSchema, mapTipos, mapEstatus);
                    enviados++;
                    AcumularFilaProcesadaParaUpdate(row, columnaId, idsProcesadosExitosamente);
                    OnStatus?.Invoke($"Enviado: {Path.GetFileName(filePath)}");
                }
                catch (Exception ex)
                {
                    Utilities.Wlog($"FileUploadWithMetadata: Error enviando ref={refNom}: {ex.Message}", 0);
                    throw;
                }

                procesados++;
                OnProgress?.Invoke(procesados, total);
            }

            int marcados = MarcarFilasComoProcesadas(
                connectionStringBd,
                tablaMetadata,
                columnaProcesado,
                columnaId,
                idsProcesadosExitosamente);

            OnStatus?.Invoke($"Completado: {enviados} enviado(s), {omitidosYaProcesados} omitido(s) ya procesado(s), {marcados} marcado(s) con Procesado=1.");
        }

        /// <summary>JOIN metadata + paths: una fila por archivo a subir.</summary>
        private static DataTable LeerMetadataConPaths(string connectionString, string tablaMetadata, string tablaPaths)
        {
            string metaEsc = "[" + tablaMetadata.Replace("]", "]]") + "]";
            string pathsEsc = "[" + tablaPaths.Replace("]", "]]") + "]";
            string sql = $@"
                SELECT m.*, p.[PathFisico], p.[HashArchivo], p.[Size], p.[Extension], p.[Id] AS [PathId]
                FROM {metaEsc} m
                INNER JOIN {pathsEsc} p ON p.[DocumentoId] = m.[Id]
                ORDER BY m.[Id], p.[Id]";

            var dt = new DataTable();
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(dt);
                }
            }
            return dt;
        }

        private static string ObtenerReferenciaArchivoFila(
            DataRow row,
            DataColumnCollection columnas,
            string columnaPathFisico)
        {
            if (!string.IsNullOrWhiteSpace(columnaPathFisico))
            {
                object o = row[columnaPathFisico];
                if (o != null && o != DBNull.Value)
                {
                    string s = o.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(s)) return s;
                }
            }
            return GetValueFromRow(row, columnas, "PathFisico") ?? "";
        }

        private static string ResolverRutaArchivoDesdePathFisico(
            DataRow row,
            DataColumnCollection columnas,
            string columnaPathFisico)
        {
            string pathFisico = row[columnaPathFisico]?.ToString()?.Trim();
            if (string.IsNullOrEmpty(pathFisico))
                pathFisico = GetValueFromRow(row, columnas, "PathFisico");
            if (string.IsNullOrWhiteSpace(pathFisico))
                return null;

            pathFisico = pathFisico.Trim();
            return File.Exists(pathFisico) ? pathFisico : null;
        }

        private static string ResolverColumnaPathFisico(DataTable metadata)
        {
            foreach (DataColumn c in metadata.Columns)
            {
                if (string.Equals(c.ColumnName, "PathFisico", StringComparison.OrdinalIgnoreCase))
                    return c.ColumnName;
            }
            return null;
        }

        /// <summary>
        /// Carga en memoria <c>Nombre</c> → <c>idTipo</c> / <c>idEstatus</c> (una sola lectura por ejecución del trabajo).
        /// </summary>
        private static (IReadOnlyDictionary<string, string> IdTipoPorNombre, IReadOnlyDictionary<string, string> IdEstatusPorNombre)
            CargarMapasTiposYEstatusDocumentos(string connectionString)
        {
            var tipos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var estatus = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(connectionString))
                return (tipos, estatus);

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                try
                {
                    using (var cmd = new SqlCommand("SELECT [Nombre], [idTipo] FROM [TiposDocumentos]", cn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string nombre = r[0] == DBNull.Value ? null : r[0].ToString()?.Trim();
                            if (string.IsNullOrEmpty(nombre)) continue;
                            string id = r[1] == DBNull.Value ? null : r[1].ToString()?.Trim();
                            if (string.IsNullOrEmpty(id)) continue;
                            if (!tipos.ContainsKey(nombre))
                                tipos[nombre] = id;
                        }
                    }
                }
                catch (SqlException ex)
                {
                    Utilities.Wlog($"FileUploadWithMetadata: TiposDocumentos no disponible en BD ({ex.Message}); se usará solo schema Aconex.", 1);
                }

                try
                {
                    using (var cmd = new SqlCommand("SELECT [Nombre], [idEstatus] FROM [EstatusDocumentos]", cn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string nombre = r[0] == DBNull.Value ? null : r[0].ToString()?.Trim();
                            if (string.IsNullOrEmpty(nombre)) continue;
                            string id = r[1] == DBNull.Value ? null : r[1].ToString()?.Trim();
                            if (string.IsNullOrEmpty(id)) continue;
                            if (!estatus.ContainsKey(nombre))
                                estatus[nombre] = id;
                        }
                    }
                }
                catch (SqlException ex)
                {
                    Utilities.Wlog($"FileUploadWithMetadata: EstatusDocumentos no disponible en BD ({ex.Message}); se usará solo schema Aconex.", 1);
                }
            }

            return (tipos, estatus);
        }

        /// <summary>
        /// Resuelve <c>idTipo</c> desde el mapa precargado (equivalente a buscar por <c>Nombre</c> en <c>TiposDocumentos</c>).
        /// </summary>
        private static string ResolveIdTipoFromTiposDocumentos(IReadOnlyDictionary<string, string> idTipoPorNombre, string nombreTipo)
        {
            if (idTipoPorNombre == null || string.IsNullOrWhiteSpace(nombreTipo))
                return null;
            string key = nombreTipo.Trim();
            return idTipoPorNombre.TryGetValue(key, out string id) ? id : null;
        }

        /// <summary>
        /// Resuelve <c>idEstatus</c> desde el mapa precargado (equivalente a buscar por <c>Nombre</c> en <c>EstatusDocumentos</c>).
        /// </summary>
        private static string ResolveIdEstatusFromEstatusDocumentos(IReadOnlyDictionary<string, string> idEstatusPorNombre, string nombreEstatus)
        {
            if (idEstatusPorNombre == null || string.IsNullOrWhiteSpace(nombreEstatus))
                return null;
            string key = nombreEstatus.Trim();
            return idEstatusPorNombre.TryGetValue(key, out string id) ? id : null;
        }

        private static string ResolverColumnaProcesado(DataTable metadata)
        {
            foreach (DataColumn c in metadata.Columns)
            {
                if (string.Equals(c.ColumnName, "Procesado", StringComparison.OrdinalIgnoreCase))
                    return c.ColumnName;
            }
            return null;
        }

        private static string ResolverColumnaId(DataTable metadata)
        {
            foreach (DataColumn c in metadata.Columns)
            {
                if (string.Equals(c.ColumnName, "Id", StringComparison.OrdinalIgnoreCase))
                    return c.ColumnName;
            }
            return null;
        }

        private static bool FilaYaProcesada(DataRow row, DataColumnCollection columnas, string columnaProcesado)
        {
            if (string.IsNullOrWhiteSpace(columnaProcesado)) return false;
            DataColumn col = null;
            foreach (DataColumn c in columnas)
            {
                if (string.Equals(c.ColumnName, columnaProcesado, StringComparison.OrdinalIgnoreCase))
                {
                    col = c;
                    break;
                }
            }
            if (col == null) return false;

            object o = row[col];
            if (o == null || o == DBNull.Value) return false;
            if (o is bool b) return b;
            if (o is byte bt) return bt != 0;
            if (o is short s) return s != 0;
            if (o is int i) return i != 0;
            string t = o.ToString()?.Trim() ?? "";
            return t == "1" || string.Equals(t, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static void AcumularFilaProcesadaParaUpdate(
            DataRow row,
            string columnaId,
            List<string> idsProcesadosExitosamente)
        {
            if (string.IsNullOrWhiteSpace(columnaId)) return;
            object oid = row[columnaId];
            if (oid == null || oid == DBNull.Value) return;
            string idStr = oid.ToString()?.Trim();
            if (!string.IsNullOrEmpty(idStr))
                idsProcesadosExitosamente.Add(idStr);
        }

        private static int MarcarFilasComoProcesadas(
            string connectionString,
            string nombreTabla,
            string columnaProcesado,
            string columnaId,
            IReadOnlyList<string> idsProcesadosExitosamente)
        {
            if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(nombreTabla))
                return 0;
            if (string.IsNullOrWhiteSpace(columnaProcesado))
            {
                Utilities.Wlog("FileUploadWithMetadata: no existe columna Procesado en metadata, no se pudo marcar Procesado=1.", 1);
                return 0;
            }

            if (string.IsNullOrWhiteSpace(columnaId) || idsProcesadosExitosamente == null || idsProcesadosExitosamente.Count == 0)
                return 0;

            string tablaEsc = "[" + nombreTabla.Replace("]", "]]") + "]";
            string colProcesadoEsc = "[" + columnaProcesado.Replace("]", "]]") + "]";
            string colIdEsc = "[" + columnaId.Replace("]", "]]") + "]";
            int totalActualizados = 0;

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                const int batchSize = 500;
                for (int start = 0; start < idsProcesadosExitosamente.Count; start += batchSize)
                {
                    int count = Math.Min(batchSize, idsProcesadosExitosamente.Count - start);
                    var paramNames = new List<string>(count);
                    using (var cmd = new SqlCommand())
                    {
                        cmd.Connection = cn;
                        for (int j = 0; j < count; j++)
                        {
                            string p = "@p" + j;
                            paramNames.Add(p);
                            cmd.Parameters.AddWithValue(p, idsProcesadosExitosamente[start + j]);
                        }

                        cmd.CommandText = "UPDATE " + tablaEsc + " SET " + colProcesadoEsc + " = 1 WHERE " + colIdEsc + " IN (" + string.Join(", ", paramNames) + ")";
                        totalActualizados += cmd.ExecuteNonQuery();
                    }
                }
            }

            return totalActualizados;
        }

        /// <summary>
        /// Construye el body con la metadata (columnas de la fila) y el archivo en base64. Listo para serializar a JSON y enviar.
        /// </summary>
        /// <param name="filePath">Ruta física del archivo.</param>
        /// <param name="metadataRow">Fila de metadata.</param>
        /// <param name="columnas">Columnas de la tabla de metadata.</param>
        /// <returns>Objeto con Metadata (diccionario nombre columna -> valor) y FileBase64, FileName.</returns>
        private static FileUploadWithMetadataBody BuildBodyWithMetadataAndFileBase64(string filePath, DataRow metadataRow, DataColumnCollection columnas)
        {
            var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (DataColumn col in columnas)
            {
                object val = metadataRow[col];
                if (val == null || val == DBNull.Value)
                {
                    metadata[col.ColumnName] = null;
                    continue;
                }
                if (val is DateTime dt)
                {
                    metadata[col.ColumnName] = dt.ToString("o");
                    continue;
                }
                if (val is byte[] bytes)
                {
                    metadata[col.ColumnName] = Convert.ToBase64String(bytes);
                    continue;
                }
                metadata[col.ColumnName] = val;
            }

            byte[] fileBytes = File.ReadAllBytes(filePath);
            string fileBase64 = Convert.ToBase64String(fileBytes);
            string fileName = Path.GetFileName(filePath);

            return new FileUploadWithMetadataBody
            {
                Metadata = metadata,
                FileBase64 = fileBase64,
                FileName = fileName
            };
        }

        /// <summary>
        /// GET <c>/api/projects/{{projectId}}/register/schema</c>: campos de creación según configuración del proyecto.
        /// </summary>
        private async Task<AconexRegisterSchemaSnapshot> ObtenerSchemaRegistroAconexAsync()
        {
            string projectId = _trabajoConfig.IdProyecto ?? _aconexConfig.ProjectId ?? "";
            if (string.IsNullOrWhiteSpace(projectId))
                throw new InvalidOperationException("IdProyecto es requerido para Register Document.");

            string baseUrl = string.IsNullOrWhiteSpace(_aconexConfig.AconexBaseUrl) ? "https://us1.aconex.com" : _aconexConfig.AconexBaseUrl.TrimEnd('/');

            string responseText;
            try
            {
                responseText = await _registerWritePort.GetRegisterSchemaXmlAsync(
                    baseUrl,
                    projectId,
                    _aconexConfig.AuthorizationHeader,
                    _aconexConfig.IntegrationId,
                    default).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                Utilities.Wlog($"FileUploadWithMetadata: GET register/schema falló. {ex.Message}", 0);
                throw;
            }

            AconexRegisterSchemaSnapshot snapshot = AconexRegisterSchemaParser.ParseSnapshot(responseText);
            if (snapshot.Fields == null || snapshot.Fields.Count == 0)
            {
                throw new InvalidOperationException(
                    "El XML de register/schema no contiene campos en EntityCreationSchemaFields (o no se pudieron leer). Revise la respuesta del endpoint.");
            }

            return snapshot;
        }

        /// <summary>
        /// Envía el archivo y la metadata a Aconex mediante el API Register Document (multipart/mixed: XML + archivo base64).
        /// Ver: https://help.aconex.com/apis/api-guide-documents/#Register-Document
        /// </summary>
        private async Task EnviarDocumentoAconexAsync(
            string filePath,
            DataRow metadataRow,
            DataColumnCollection columnas,
            AconexRegisterSchemaSnapshot registerSchema,
            IReadOnlyDictionary<string, string> idTipoPorNombre,
            IReadOnlyDictionary<string, string> idEstatusPorNombre)
        {
            FileUploadWithMetadataBody body = BuildBodyWithMetadataAndFileBase64(filePath, metadataRow, columnas);
            string projectId = _trabajoConfig.IdProyecto ?? _aconexConfig.ProjectId ?? "";
            if (string.IsNullOrWhiteSpace(projectId))
                throw new InvalidOperationException("IdProyecto es requerido para Register Document.");

            string xmlDocument = BuildAconexRegisterXml(
                metadataRow, columnas, body.FileName, registerSchema, idTipoPorNombre, idEstatusPorNombre);
            Utilities.Wlog("FileUploadWithMetadata: XML Register Document (cuerpo multipart 1): " + xmlDocument, 1);

            string boundary = AconexRegisterMultipart.ExampleBoundary;
            string multipartBody = AconexRegisterMultipart.BuildRegisterBody(xmlDocument, body.FileName, body.FileBase64, boundary);

            string baseUrl = string.IsNullOrWhiteSpace(_aconexConfig.AconexBaseUrl) ? "https://us1.aconex.com" : _aconexConfig.AconexBaseUrl.TrimEnd('/');

            AconexRawHttpResponse raw = await _registerWritePort.PostRegisterDocumentAsync(
                baseUrl,
                projectId,
                _aconexConfig.AuthorizationHeader,
                _aconexConfig.IntegrationId,
                multipartBody,
                boundary,
                default).ConfigureAwait(false);

            string responseText = raw.Body ?? "";

            if (!raw.IsSuccessStatusCode)
            {
                Utilities.Wlog($"FileUploadWithMetadata: Register Document falló. Status={raw.StatusCode}, Response={responseText}", 0);
                if (ResponseIndicatesFieldValueAlreadyExists(responseText))
                {
                    string refArchivo = GetValueFromRow(metadataRow, columnas, "PathFisico") ?? Path.GetFileName(filePath) ?? "";
                    throw new InvalidOperationException(
                        "Aconex indica FIELD_VALUE_ALREADY_EXISTS (p. ej. documento o valor único ya existente). " +
                        "Register Document solo crea documentos nuevos. Opciones: excluir esa fila si ya se cargó, " +
                        "o usar en Aconex el flujo de nueva revisión / Supersede según su proceso. " +
                        $"PathFisico={refArchivo}. Respuesta: {responseText}");
                }

                throw new InvalidOperationException(FormatAconexRegisterFailureMessage(raw.StatusCode, responseText));
            }

            string documentId = ParseRegisterDocumentResponse(responseText);
            string logArchivo = GetValueFromRow(metadataRow, columnas, "PathFisico") ?? Path.GetFileName(filePath) ?? "";
            Utilities.Wlog($"FileUploadWithMetadata: Documento registrado. PathFisico={logArchivo}, DocumentId={documentId}", 1);
            OnStatus?.Invoke($"Registrado en Aconex: {body.FileName} (Id={documentId})");
        }

        /// <summary>
        /// Obtiene el <c>DocumentStatusId</c> válido para el proyecto: primero contra el schema (nombre o Id de Aconex), luego <c>EstatusDocumentos</c>.
        /// Los IDs deben existir en el schema; un <c>idEstatus</c> local que no coincida con Aconex provoca <c>INVALID_FIELD_VALUE</c>.
        /// </summary>
        private string ResolveDocumentStatusIdForAconex(
            DataRow row,
            DataColumnCollection columnas,
            IReadOnlyDictionary<string, IReadOnlyList<AconexSchemaValueOption>> picklists,
            IReadOnlyDictionary<string, string> idEstatusPorNombre)
        {
            string raw = GetValueFromRow(row, columnas, "docstatus", "statusid", "DocumentStatusId", "Status");
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException(
                    "El estado del documento es obligatorio para Register Document: indique docstatus, statusid o DocumentStatusId en la tabla de metadata.");

            string trimmed = raw.Trim();

            if (TryResolveIdFromAconexPicklist(picklists, "DocumentStatusId", trimmed, out string fromSchema))
                return fromSchema;

            string fromSql = ResolveIdEstatusFromEstatusDocumentos(idEstatusPorNombre, trimmed);
            if (string.IsNullOrWhiteSpace(fromSql))
                throw new InvalidOperationException(
                    $"No se encontró estado para '{trimmed}' ni en el schema de Aconex (SchemaValue) ni en EstatusDocumentos (Nombre).");

            if (PicklistDefinesOptions(picklists, "DocumentStatusId") &&
                !IsIdInPicklist(picklists, "DocumentStatusId", fromSql))
            {
                throw new InvalidOperationException(
                    $"El id de estado '{fromSql}' (tabla EstatusDocumentos) no es un DocumentStatusId válido para este proyecto en Aconex. " +
                    "Use el texto exacto del estado como en la interfaz o el Id que aparece en GET .../register/schema para ese estado.");
            }

            return fromSql;
        }

        /// <summary>
        /// Resuelve <c>DocumentTypeId</c> desde el schema (preferido) o <c>TiposDocumentos</c>.
        /// </summary>
        private string ResolveDocumentTypeIdForAconex(
            DataRow row,
            DataColumnCollection columnas,
            IReadOnlyDictionary<string, IReadOnlyList<AconexSchemaValueOption>> picklists,
            IReadOnlyDictionary<string, string> idTipoPorNombre)
        {
            string docTypeNombre = GetValueFromRow(row, columnas, "doctype", "Doctype", "DocumentTypeId");
            if (string.IsNullOrWhiteSpace(docTypeNombre))
                docTypeNombre = DefaultDocumentTypeName;

            string trimmed = docTypeNombre.Trim();

            if (TryResolveIdFromAconexPicklist(picklists, "DocumentTypeId", trimmed, out string fromSchema))
                return fromSchema;

            string fromSql = ResolveIdTipoFromTiposDocumentos(idTipoPorNombre, trimmed);
            if (string.IsNullOrWhiteSpace(fromSql))
                throw new InvalidOperationException(
                    $"No se encontró tipo de documento para '{trimmed}' ni en el schema de Aconex ni en TiposDocumentos (Nombre).");

            if (PicklistDefinesOptions(picklists, "DocumentTypeId") &&
                !IsIdInPicklist(picklists, "DocumentTypeId", fromSql))
            {
                throw new InvalidOperationException(
                    $"El id de tipo '{fromSql}' (tabla TiposDocumentos) no es un DocumentTypeId válido para este proyecto en Aconex. " +
                    "Use el nombre del tipo como en Aconex o el Id del schema (GET .../register/schema).");
            }

            return fromSql;
        }

        /// <summary>
        /// Resuelve <c>Author</c> desde la fila DataLake (<c>CreadoPor</c>) o el default SQL.
        /// </summary>
        private static string ResolveAuthorForAconex(DataRow row, DataColumnCollection columnas)
        {
            string author = GetValueFromRow(row, columnas, "Author", "author", "CreadoPor");
            return string.IsNullOrWhiteSpace(author) ? DefaultAuthorName : author.Trim();
        }

        private static bool PicklistDefinesOptions(
            IReadOnlyDictionary<string, IReadOnlyList<AconexSchemaValueOption>> picklists,
            string identifier)
        {
            if (picklists == null || string.IsNullOrEmpty(identifier)) return false;
            return picklists.TryGetValue(identifier, out var opts) && opts != null && opts.Count > 0;
        }

        private static bool TryResolveIdFromAconexPicklist(
            IReadOnlyDictionary<string, IReadOnlyList<AconexSchemaValueOption>> picklists,
            string fieldIdentifier,
            string userInput,
            out string aconexId)
        {
            aconexId = null;
            if (picklists == null || string.IsNullOrWhiteSpace(userInput) || string.IsNullOrWhiteSpace(fieldIdentifier))
                return false;
            if (!picklists.TryGetValue(fieldIdentifier, out var options) || options == null || options.Count == 0)
                return false;

            string t = userInput.Trim();
            foreach (AconexSchemaValueOption o in options)
            {
                if (o == null) continue;
                if (!string.IsNullOrWhiteSpace(o.Id) && string.Equals(o.Id.Trim(), t, StringComparison.OrdinalIgnoreCase))
                {
                    aconexId = o.Id.Trim();
                    return true;
                }
            }

            foreach (AconexSchemaValueOption o in options)
            {
                if (o?.Value == null) continue;
                if (string.Equals(o.Value.Trim(), t, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(o.Id))
                    {
                        aconexId = o.Id.Trim();
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsIdInPicklist(
            IReadOnlyDictionary<string, IReadOnlyList<AconexSchemaValueOption>> picklists,
            string fieldIdentifier,
            string id)
        {
            if (picklists == null || string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(fieldIdentifier))
                return false;
            if (!picklists.TryGetValue(fieldIdentifier, out var options) || options == null)
                return false;
            foreach (AconexSchemaValueOption o in options)
            {
                if (o != null && !string.IsNullOrWhiteSpace(o.Id) && string.Equals(o.Id.Trim(), id.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Construye el XML <c>Document</c> según <paramref name="registerSchema"/> (GET register/schema).
        /// Tipo Aconex: <c>Doctype</c>/<c>doctype</c> → <c>DocumentTypeId</c> vía <c>TiposDocumentos</c> (default Documento Interno).
        /// Campo proyecto: <c>TipoDocumento</c> → <c>TipoDeDocumento_singleSelect</c> (default Certificado). <c>Status</c> → <c>DocumentStatusId</c>.
        /// El resto de identificadores se toman de columnas cuyo nombre coincide con el identificador o alias (p. ej. <c>Discipline</c>/<c>discipline</c>).
        /// Con autonumeración (<see cref="RegisterDocumentUseAconexAutoNumber"/>), no se envía <c>DocumentNumber</c>; el archivo se toma de <c>PathFisico</c> en <c>DocumentosPath</c>.
        /// Campos de proyecto <c>*_singleSelect</c> se envían solo si hay valor en la fila; <c>TipoDeDocumento_singleSelect</c> siempre (default Certificado).
        /// </summary>
        private string BuildAconexRegisterXml(
            DataRow row,
            DataColumnCollection columnas,
            string uploadFileName,
            AconexRegisterSchemaSnapshot registerSchema,
            IReadOnlyDictionary<string, string> idTipoPorNombre,
            IReadOnlyDictionary<string, string> idEstatusPorNombre)
        {
            if (registerSchema?.Fields == null || registerSchema.Fields.Count == 0)
                throw new ArgumentException("registerSchema no puede estar vacío.", nameof(registerSchema));

            bool useAutoNumber = RegisterDocumentUseAconexAutoNumber;
            string docNumber = GetValueFromRow(row, columnas, "docno", "DocumentNumber", "NumeroDocumento") ?? "";
            if (!useAutoNumber && string.IsNullOrWhiteSpace(docNumber))
                throw new InvalidOperationException("docno/DocumentNumber es obligatorio en la tabla de metadata para Register Document (o active autonumeración en Aconex y en este worker).");

            string title = GetValueFromRow(row, columnas, "title", "Title", "Titulo") ?? "";
            if (string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(uploadFileName))
                title = Path.GetFileNameWithoutExtension(uploadFileName);
            if (string.IsNullOrWhiteSpace(title))
            {
                if (!string.IsNullOrWhiteSpace(docNumber))
                    title = docNumber;
                else
                {
                    string na = GetValueFromRow(row, columnas, "Titulo", "PathFisico");
                    title = !string.IsNullOrWhiteSpace(na) ? Path.GetFileNameWithoutExtension(na) : "Documento";
                }
            }

            string revision = GetValueFromRow(row, columnas, "revision", "Revision") ?? "A";

            IReadOnlyDictionary<string, IReadOnlyList<AconexSchemaValueOption>> picklists = registerSchema.PicklistsByIdentifier;

            string docTypeId = ResolveDocumentTypeIdForAconex(row, columnas, picklists, idTipoPorNombre);
            string docStatusId = ResolveDocumentStatusIdForAconex(row, columnas, picklists, idEstatusPorNombre);

            var sb = new StringBuilder();
            sb.Append("<Document>");
            if (useAutoNumber)
                sb.Append("<AutoNumber>true</AutoNumber>");

            bool emittedHasFile = false;
            var emittedXmlIdentifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (useAutoNumber)
                emittedXmlIdentifiers.Add("DocumentNumber");

            foreach (AconexRegisterSchemaField field in registerSchema.Fields)
            {
                if (field == null || string.IsNullOrWhiteSpace(field.Identifier))
                    continue;

                string id = field.Identifier.Trim();
                if (useAutoNumber && string.Equals(id, "DocumentNumber", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!ShouldEmitRegisterSchemaIdentifier(id))
                    continue;
                if (ShouldSkipStandardRegisterIdentifierInFavorOfProjectSingleSelect(id))
                    continue;

                string mandatory = field.MandatoryStatus ?? "";
                bool isMandatory = string.Equals(mandatory, "MANDATORY", StringComparison.OrdinalIgnoreCase);

                string value = ResolveRegisterFieldValueForSchema(
                    row, columnas, field.DataType,
                    id, docNumber, title, revision, docTypeId, docStatusId);

                if (string.IsNullOrEmpty(value))
                {
                    if (isMandatory)
                        throw new InvalidOperationException(
                            $"Campo obligatorio según schema de Aconex sin valor: {id}. Añada la columna en la tabla de metadata o el dato requerido.");
                    continue;
                }

                sb.Append("<").Append(id).Append(">").Append(EscapeXml(value)).Append("</").Append(id).Append(">");
                emittedXmlIdentifiers.Add(id);
                if (string.Equals(id, "HasFile", StringComparison.OrdinalIgnoreCase))
                    emittedHasFile = true;
            }

            AppendRegisterXmlFromExtraMetadataColumns(sb, row, columnas, emittedXmlIdentifiers, registerSchema, useAutoNumber);

            AppendProjectSingleSelectFieldsFromRow(sb, row, columnas, registerSchema);

            if (!emittedHasFile)
                sb.Append("<HasFile>true</HasFile>");

            sb.Append("</Document>");
            return sb.ToString();
        }

        /// <summary>
        /// Emite <c>*_singleSelect</c> solo con datos presentes en la fila (sin defaults), excepto
        /// <see cref="XmlNameTipoDeDocumentoSingleSelect"/> que siempre se envía (default <see cref="DefaultTipoDeDocumentoSingleSelectValue"/>).
        /// </summary>
        private static void AppendProjectSingleSelectFieldsFromRow(
            StringBuilder sb,
            DataRow row,
            DataColumnCollection columnas,
            AconexRegisterSchemaSnapshot registerSchema)
        {
            var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach ((string columnaMetadata, string xmlSingleSelect) in DataLakeProjectFieldMap)
            {
                if (string.Equals(xmlSingleSelect, XmlNameTipoDeDocumentoSingleSelect, StringComparison.OrdinalIgnoreCase))
                    continue;

                string value = ResolveProjectSingleSelectValue(row, columnas, xmlSingleSelect);
                if (string.IsNullOrWhiteSpace(value)) continue;
                if (!emitted.Add(xmlSingleSelect)) continue;
                sb.Append("<").Append(xmlSingleSelect).Append(">")
                    .Append(EscapeXml(value.Trim()))
                    .Append("</").Append(xmlSingleSelect).Append(">");
            }

            foreach (DataColumn c in columnas)
            {
                string colName = c.ColumnName;
                if (!IsProjectFieldSingleSelectColumn(colName)) continue;
                if (string.Equals(colName, XmlNameTipoDeDocumentoSingleSelect, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!emitted.Add(colName)) continue;

                string value = ResolveProjectSingleSelectValue(row, columnas, colName);
                if (string.IsNullOrEmpty(value)) continue;

                sb.Append("<").Append(colName).Append(">")
                    .Append(EscapeXml(value))
                    .Append("</").Append(colName).Append(">");
            }

            string tipoDeDocumento = ResolveProjectSingleSelectValue(row, columnas, XmlNameTipoDeDocumentoSingleSelect);
            if (string.IsNullOrWhiteSpace(tipoDeDocumento))
                tipoDeDocumento = DefaultTipoDeDocumentoSingleSelectValue;

            sb.Append("<").Append(XmlNameTipoDeDocumentoSingleSelect).Append(">")
                .Append(EscapeXml(tipoDeDocumento.Trim()))
                .Append("</").Append(XmlNameTipoDeDocumentoSingleSelect).Append(">");

            ValidateMandatoryProjectSingleSelectFields(row, columnas, registerSchema);
        }

        /// <summary>Obtiene el valor DataLake para un campo de proyecto <c>*_singleSelect</c> (p. ej. columna <c>PWP</c> → <c>Pwp_singleSelect</c>).</summary>
        private static string ResolveProjectSingleSelectValue(
            DataRow row,
            DataColumnCollection columnas,
            string xmlSingleSelect)
        {
            if (string.IsNullOrWhiteSpace(xmlSingleSelect)) return null;

            string dataLakeColumn = TryGetDataLakeColumnForXmlSingleSelect(xmlSingleSelect);
            if (!string.IsNullOrWhiteSpace(dataLakeColumn))
            {
                string fromMapped = GetValueFromRow(row, columnas, dataLakeColumn);
                if (!string.IsNullOrWhiteSpace(fromMapped)) return fromMapped.Trim();
            }

            return GetValueFromRow(row, columnas, xmlSingleSelect);
        }

        private static string TryGetDataLakeColumnForXmlSingleSelect(string xmlSingleSelect)
        {
            if (string.IsNullOrWhiteSpace(xmlSingleSelect)) return null;
            foreach ((string columnaMetadata, string xml) in DataLakeProjectFieldMap)
            {
                if (string.Equals(xml, xmlSingleSelect, StringComparison.OrdinalIgnoreCase))
                    return columnaMetadata;
            }
            return null;
        }

        /// <summary>Sugiere columna en <c>DocumentosMetadata</c> para un identificador XML de proyecto.</summary>
        private static string SuggestDataLakeColumnForXmlField(string xmlField)
        {
            string fromMap = TryGetDataLakeColumnForXmlSingleSelect(xmlField);
            if (!string.IsNullOrWhiteSpace(fromMap)) return fromMap;

            const string suffix = "_singleSelect";
            if (!string.IsNullOrWhiteSpace(xmlField)
                && xmlField.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                && xmlField.Length > suffix.Length)
            {
                string baseName = xmlField.Substring(0, xmlField.Length - suffix.Length);
                if (baseName.Length == 1)
                    return baseName.ToUpperInvariant();
                return char.ToUpperInvariant(baseName[0]) + baseName.Substring(1);
            }

            return xmlField;
        }

        private static void ValidateMandatoryProjectSingleSelectFields(
            DataRow row,
            DataColumnCollection columnas,
            AconexRegisterSchemaSnapshot registerSchema)
        {
            if (registerSchema?.Fields == null) return;

            foreach (AconexRegisterSchemaField field in registerSchema.Fields)
            {
                if (field == null || string.IsNullOrWhiteSpace(field.Identifier)) continue;

                string id = field.Identifier.Trim();
                if (!IsProjectFieldSingleSelectColumn(id)) continue;
                if (!string.Equals(field.MandatoryStatus, "MANDATORY", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(id, XmlNameTipoDeDocumentoSingleSelect, StringComparison.OrdinalIgnoreCase)) continue;

                string value = ResolveProjectSingleSelectValue(row, columnas, id);
                if (!string.IsNullOrWhiteSpace(value)) continue;

                string hint = SuggestDataLakeColumnForXmlField(id);
                throw new InvalidOperationException(
                    $"Campo obligatorio según schema de Aconex sin valor: {id}. Añada la columna '{hint}' en DocumentosMetadata con un valor válido del proyecto.");
            }
        }

        /// <summary>
        /// Detecta columnas de metadata cuyo nombre termina en <c>_singleSelect</c> (convención acordada con Aconex).
        /// </summary>
        private static bool IsProjectFieldSingleSelectColumn(string columnName) =>
            !string.IsNullOrWhiteSpace(columnName)
            && columnName.EndsWith("_singleSelect", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Register Document usa identificadores del schema del proyecto; los campos <c>*_singleSelect</c> se emiten aparte (ver <see cref="AppendProjectSingleSelectFieldsFromRow"/>). El prefijo <c>RegisterXml_</c> sigue limitado a identificadores conocidos de la guía.
        /// 1) Columnas <c>RegisterXml_ProjectField1</c> (prefijo <c>RegisterXml_</c> + identificador permitido): emiten <c>&lt;ProjectField1&gt;…&lt;/ProjectField1&gt;</c>.
        /// 2) Identificadores conocidos aún no emitidos: se rellenan por alias (p. ej. columna <c>ProjectField1</c>).
        /// Revise en GET <c>register/schema</c> qué <c>FieldName</c> corresponde a cada <c>ProjectField1</c>…<c>3</c> en su proyecto.
        /// </summary>
        private static void AppendRegisterXmlFromExtraMetadataColumns(
            StringBuilder sb,
            DataRow row,
            DataColumnCollection columnas,
            HashSet<string> emittedXmlIdentifiers,
            AconexRegisterSchemaSnapshot registerSchema,
            bool useAutoNumber)
        {
            foreach (DataColumn c in columnas)
            {
                string colName = c.ColumnName;
                if (string.IsNullOrWhiteSpace(colName)) continue;
                if (!TryParseRegisterXmlPrefixedColumn(colName, out string xmlId)) continue;
                if (!IsKnownRegisterDocumentIdentifier(xmlId)) continue;
                if (useAutoNumber && string.Equals(xmlId, "DocumentNumber", StringComparison.OrdinalIgnoreCase)) continue;
                if (emittedXmlIdentifiers.Contains(xmlId)) continue;
                if (ShouldSkipStandardRegisterIdentifierInFavorOfProjectSingleSelect(xmlId)) continue;

                object o = row[c];
                if (o == null || o == DBNull.Value) continue;

                string dt = GetDataTypeForRegisterIdentifier(xmlId, registerSchema);
                string value = FormatRegisterValue(o, dt, xmlId);
                if (string.IsNullOrEmpty(value)) continue;

                sb.Append("<").Append(xmlId).Append(">").Append(EscapeXml(value)).Append("</").Append(xmlId).Append(">");
                emittedXmlIdentifiers.Add(xmlId);
            }

            foreach (string id in KnownRegisterDocumentFieldIdentifiers)
            {
                if (emittedXmlIdentifiers.Contains(id)) continue;
                if (useAutoNumber && string.Equals(id, "DocumentNumber", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(id, "HasFile", StringComparison.OrdinalIgnoreCase)) continue;
                if (IsSpecialCasedRegisterResolveIdentifier(id)) continue;
                if (ShouldSkipStandardRegisterIdentifierInFavorOfProjectSingleSelect(id)) continue;

                string dt = GetDataTypeForRegisterIdentifier(id, registerSchema);
                string value = GetGenericRegisterFieldValue(row, columnas, id, dt);
                if (string.IsNullOrEmpty(value)) continue;

                sb.Append("<").Append(id).Append(">").Append(EscapeXml(value)).Append("</").Append(id).Append(">");
                emittedXmlIdentifiers.Add(id);
            }
        }

        /// <summary>Identificadores de creación admitidos por la API Register Document (subset de la guía Oracle Aconex).</summary>
        private static readonly string[] KnownRegisterDocumentFieldIdentifiers =
        {
            "DocumentTypeId", "DocumentStatusId", "Discipline", "Attribute1", "Attribute2", "Attribute3", "Attribute4",
            "ReviewStatusId", "Vdrcode", "Category", "PackageNumber", "ContractNumber",
            "DocumentNumber", "Revision", "DateCreated", "Title", "AuthorisedBy", "Comments", "Comments2",
            "PrintSize", "PercentComplete", "Reference", "Author", "Scale", "AccessList", "DateApproved",
            "DateForReview", "DateReviewed", "ToClientDate", "RevisionDate", "PlannedSubmissionDate",
            "MilestoneDate", "TagNumber", "VendorDocumentNumber", "VendorRev", "ContractorDocumentNumber",
            "ContractorRev", "AsBuiltRequired", "ContractDeliverable", "ProjectField1", "ProjectField2",
            "ProjectField3", "Check1", "Check2", "Date1", "Date2", "HasFile"
        };

        private static readonly HashSet<string> KnownRegisterDocumentIdentifierSet =
            new HashSet<string>(KnownRegisterDocumentFieldIdentifiers, StringComparer.OrdinalIgnoreCase);

        private static bool IsKnownRegisterDocumentIdentifier(string id) =>
            !string.IsNullOrWhiteSpace(id) && KnownRegisterDocumentIdentifierSet.Contains(id);

        private static bool IsAconexProjectFieldColumn(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName)) return false;
            return columnName.EndsWith("_singleSelect", StringComparison.OrdinalIgnoreCase)
                || columnName.EndsWith("_multiLineText", StringComparison.OrdinalIgnoreCase)
                || columnName.EndsWith("_singleLineText", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Solo emite campos del GET register/schema que son identificadores válidos de Register Document
        /// o campos de proyecto Aconex (<c>*_singleSelect</c>, etc.). Evita colisiones con columnas internas (p. ej. <c>Id</c>).
        /// </summary>
        private static bool ShouldEmitRegisterSchemaIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return false;
            if (string.Equals(identifier, "id", StringComparison.OrdinalIgnoreCase)) return false;
            if (IsKnownRegisterDocumentIdentifier(identifier)) return true;
            return IsAconexProjectFieldColumn(identifier);
        }

        private static bool IsInternalDataLakeColumn(string columnName) =>
            !string.IsNullOrWhiteSpace(columnName) && InternalDataLakeColumnNames.Contains(columnName);

        /// <summary>
        /// Si una columna DataLake alimenta <c>Discipline_singleSelect</c>, no emitir también el identificador estándar inactivo <c>Discipline</c>.
        /// </summary>
        private static bool ShouldSkipStandardRegisterIdentifierInFavorOfProjectSingleSelect(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier) || IsProjectFieldSingleSelectColumn(identifier))
                return false;

            string projectXml = identifier.Trim() + "_singleSelect";
            foreach ((string _, string xml) in DataLakeProjectFieldMap)
            {
                if (string.Equals(xml, projectXml, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>Identificadores resueltos en <see cref="ResolveRegisterFieldValueForSchema"/>; no rellenar con <see cref="GetGenericRegisterFieldValue"/> en la pasada extra.</summary>
        private static bool IsSpecialCasedRegisterResolveIdentifier(string id)
        {
            switch (id)
            {
                case "DocumentNumber":
                case "Title":
                case "Revision":
                case "DocumentTypeId":
                case "DocumentStatusId":
                case "Author":
                case "HasFile":
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryParseRegisterXmlPrefixedColumn(string columnName, out string registerIdentifier)
        {
            registerIdentifier = null;
            if (string.IsNullOrWhiteSpace(columnName)) return false;
            const string prefix = "RegisterXml_";
            if (!columnName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
            registerIdentifier = columnName.Substring(prefix.Length).Trim();
            return registerIdentifier.Length > 0;
        }

        private static string GetDataTypeForRegisterIdentifier(string id, AconexRegisterSchemaSnapshot registerSchema)
        {
            if (registerSchema?.Fields != null)
            {
                foreach (AconexRegisterSchemaField f in registerSchema.Fields)
                {
                    if (f != null && string.Equals(f.Identifier, id, StringComparison.OrdinalIgnoreCase))
                        return string.IsNullOrWhiteSpace(f.DataType) ? "STRING" : f.DataType.Trim();
                }
            }

            switch (id)
            {
                case "Date1":
                case "Date2":
                case "DateCreated":
                case "DateApproved":
                case "DateForReview":
                case "DateReviewed":
                case "ToClientDate":
                case "RevisionDate":
                case "PlannedSubmissionDate":
                case "MilestoneDate":
                    return "DATE";
                case "Check1":
                case "Check2":
                case "AsBuiltRequired":
                case "ContractDeliverable":
                case "HasFile":
                    return "BOOLEAN";
                case "PercentComplete":
                case "AccessList":
                    return "INTEGER";
                default:
                    return "STRING";
            }
        }

        /// <summary>
        /// Resuelve el valor XML para un <see cref="AconexRegisterSchemaField.Identifier"/>.
        /// </summary>
        private static string ResolveRegisterFieldValueForSchema(
            DataRow row,
            DataColumnCollection columnas,
            string dataType,
            string identifier,
            string docNumber,
            string title,
            string revision,
            string docTypeId,
            string docStatusId)
        {
            switch (identifier)
            {
                case "DocumentNumber":
                    return docNumber;
                case "Title":
                    return title;
                case "Revision":
                    return revision;
                case "DocumentTypeId":
                    return docTypeId;
                case "DocumentStatusId":
                    return docStatusId;
                case "Author":
                    return ResolveAuthorForAconex(row, columnas);
                case "HasFile":
                    return "true";
                default:
                    return GetGenericRegisterFieldValue(row, columnas, identifier, dataType);
            }
        }

        private static string GetGenericRegisterFieldValue(DataRow row, DataColumnCollection columnas, string identifier, string dataType)
        {
            if (ShouldSkipStandardRegisterIdentifierInFavorOfProjectSingleSelect(identifier))
                return null;

            foreach (string alias in GetColumnAliasesForIdentifier(identifier))
            {
                if (IsInternalDataLakeColumn(alias)) continue;
                foreach (DataColumn c in columnas)
                {
                    if (!string.Equals(c.ColumnName, alias, StringComparison.OrdinalIgnoreCase)) continue;
                    if (IsInternalDataLakeColumn(c.ColumnName)) continue;
                    object o = row[c];
                    if (o == null || o == DBNull.Value) break;
                    return FormatRegisterValue(o, dataType, identifier);
                }
            }

            if (string.Equals(identifier, "Author", StringComparison.OrdinalIgnoreCase))
                return ResolveAuthorForAconex(row, columnas);

            if (IsProjectFieldSingleSelectColumn(identifier))
                return ResolveProjectSingleSelectValue(row, columnas, identifier);

            return null;
        }

        /// <summary>
        /// Campos de fecha en Register Document (DataType DATE en el schema). Mismo patrón que el search (ISO UTC con <c>Z</c>).
        /// </summary>
        private static bool IsAconexDateOnlyXmlField(string xmlFieldIdentifier)
        {
            if (string.IsNullOrEmpty(xmlFieldIdentifier)) return false;
            switch (xmlFieldIdentifier)
            {
                case "RevisionDate":
                case "DateCreated":
                case "DateApproved":
                case "DateForReview":
                case "DateReviewed":
                case "ToClientDate":
                case "PlannedSubmissionDate":
                case "MilestoneDate":
                case "Date1":
                case "Date2":
                    return true;
                default:
                    return false;
            }
        }

        private static DateTime? TryCoerceToDateTime(object o)
        {
            if (o == null || o == DBNull.Value) return null;
            if (o is DateTime d) return d;
            if (o is DateTimeOffset dto) return dto.UtcDateTime;
            if (o is string s && !string.IsNullOrWhiteSpace(s))
            {
                if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces, out var t))
                    return t;
                if (DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out t))
                    return t;
            }
            try
            {
                return Convert.ToDateTime(o, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        private static string[] GetColumnAliasesForIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return Array.Empty<string>();

            switch (identifier)
            {
                case "DocumentNumber":
                    return new[] { "docno", "DocumentNumber", "NumeroDocumento" };
                case "Title":
                    return new[] { "title", "Title", "Titulo" };
                case "DocumentTypeId":
                    return new[] { "doctype", "Doctype", "DocumentTypeId" };
                case "DocumentStatusId":
                    return new[] { "docstatus", "statusid", "DocumentStatusId", "Status" };
                case "Revision":
                    return new[] { "revision", "Revision" };
                case "RevisionDate":
                    return new[] { "RevisionDate", "revisionDate", "revisiondate", "FechaRevision" };
                case "PackageNumber":
                    return new[] { "PackageNumber", "packagenumber" };
                case "ContractNumber":
                    return new[] { "ContractNumber", "contractnumber" };
                case "VendorDocumentNumber":
                    return new[] { "VendorDocumentNumber", "vendordocumentnumber" };
                case "ContractorDocumentNumber":
                    return new[] { "ContractorDocumentNumber", "contractordocumentnumber" };
                case "TagNumber":
                    return new[] { "TagNumber", "tagNumber" };
                case "Discipline":
                    return new[] { "Discipline", "discipline" };
                case "Author":
                    return new[] { "Author", "author", "CreadoPor" };
                case "AuthorisedBy":
                    return new[] { "AuthorisedBy", "authorisedBy" };
                case "Comments":
                    return new[] { "Comments", "comments" };
                case "Comments2":
                    return new[] { "Comments2", "comments2" };
                case "Reference":
                    return new[] { "Reference", "reference" };
                case "Category":
                    return new[] { "Category", "category" };
                case "VendorRev":
                    return new[] { "VendorRev", "vendorrev" };
                case "ContractorRev":
                    return new[] { "ContractorRev", "contractorrev" };
                case "Vdrcode":
                    return new[] { "Vdrcode", "vdrcode" };
                case "PrintSize":
                    return new[] { "PrintSize", "printSize" };
                case "Attribute1":
                    return new[] { "Attribute1", "attribute1" };
                case "Attribute2":
                    return new[] { "Attribute2", "attribute2" };
                case "Attribute3":
                    return new[] { "Attribute3", "attribute3" };
                case "Attribute4":
                    return new[] { "Attribute4", "attribute4" };
                case "ProjectField1":
                    return new[] { "ProjectField1", "projectField1" };
                case "ProjectField2":
                    return new[] { "ProjectField2", "projectField2" };
                case "ProjectField3":
                    return new[] { "ProjectField3", "projectField3" };
                case "ReviewStatusId":
                    return new[] { "ReviewStatusId", "reviewstatus", "reviewStatus" };
                default:
                    if (IsProjectFieldSingleSelectColumn(identifier))
                    {
                        var aliases = new List<string>
                        {
                            identifier,
                            identifier.Length > 1
                                ? char.ToLowerInvariant(identifier[0]) + identifier.Substring(1)
                                : identifier.ToLowerInvariant()
                        };
                        string dataLakeColumn = TryGetDataLakeColumnForXmlSingleSelect(identifier);
                        if (!string.IsNullOrWhiteSpace(dataLakeColumn))
                            aliases.Add(dataLakeColumn);
                        return aliases.ToArray();
                    }

                    return new[]
                    {
                        identifier,
                        identifier.Length > 1
                            ? char.ToLowerInvariant(identifier[0]) + identifier.Substring(1)
                            : identifier.ToLowerInvariant()
                    };
            }
        }

        /// <summary>
        /// Fechas para Register Document: mismo estilo que el search (ej. <c>"revisionDate": "2025-11-17T05:00:00.000Z"</c>).
        /// ISO 8601 con milisegundos y sufijo Z (UTC).
        /// </summary>
        private static string FormatAconexRegisterDateXml(DateTime date)
        {
            DateTime utc;
            switch (date.Kind)
            {
                case DateTimeKind.Utc:
                    utc = date;
                    break;
                case DateTimeKind.Local:
                    utc = date.ToUniversalTime();
                    break;
                default:
                    // Sin zona (típico de SQL Server): interpretar como instante UTC para alinear con Aconex.
                    utc = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                    break;
            }

            return utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        }

        private static string FormatRegisterValue(object o, string dataType, string xmlFieldIdentifier = null)
        {
            if (o == null || o == DBNull.Value) return null;
            string dt = string.IsNullOrWhiteSpace(dataType) ? "STRING" : dataType.Trim();

            if (string.Equals(dt, "BOOLEAN", StringComparison.OrdinalIgnoreCase))
            {
                if (o is bool b) return b ? "true" : "false";
                string s = o.ToString().Trim();
                if (string.Equals(s, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase))
                    return "true";
                if (string.Equals(s, "0", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "false", StringComparison.OrdinalIgnoreCase))
                    return "false";
                return s;
            }

            if (string.Equals(dt, "INTEGER", StringComparison.OrdinalIgnoreCase) || string.Equals(dt, "LONG", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return Convert.ToInt64(o, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                }
                catch
                {
                    return o.ToString().Trim();
                }
            }

            if (string.Equals(dt, "DOUBLE", StringComparison.OrdinalIgnoreCase) || string.Equals(dt, "RATIO", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return Convert.ToDouble(o, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                }
                catch
                {
                    return o.ToString().Trim();
                }
            }

            bool dateOnly =
                string.Equals(dt, "DATE", StringComparison.OrdinalIgnoreCase)
                || IsAconexDateOnlyXmlField(xmlFieldIdentifier);

            DateTime? maybeDate = TryCoerceToDateTime(o);
            if (maybeDate.HasValue)
            {
                if (dateOnly)
                    return FormatAconexRegisterDateXml(maybeDate.Value);
                return maybeDate.Value.ToString("o", CultureInfo.InvariantCulture);
            }

            return o.ToString().Trim();
        }

        private static string GetValueFromRow(DataRow row, DataColumnCollection columnas, params string[] columnNames)
        {
            foreach (string name in columnNames)
            {
                foreach (DataColumn c in columnas)
                {
                    if (!string.Equals(c.ColumnName, name, StringComparison.OrdinalIgnoreCase)) continue;
                    object o = row[c];
                    if (o == null || o == DBNull.Value) break;
                    string s = o.ToString().Trim();
                    if (s.Length > 0) return s;
                    break;
                }
            }
            return null;
        }

        private static string EscapeXml(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        /// <summary>
        /// Register Document no permite dos documentos con el mismo <c>DocumentNumber</c> en el proyecto.
        /// </summary>
        private static bool ResponseIndicatesFieldValueAlreadyExists(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText)) return false;
            return responseText.IndexOf("FIELD_VALUE_ALREADY_EXISTS", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FormatAconexRegisterFailureMessage(int statusCode, string responseText)
        {
            if (TryParseMissingMandatoryField(responseText, out string xmlField))
            {
                string hint = SuggestDataLakeColumnForXmlField(xmlField);
                return
                    $"Aconex Register Document falló: {statusCode}. Campo obligatorio sin valor: {xmlField}. " +
                    $"Añada la columna '{hint}' en DocumentosMetadata con un valor válido del proyecto. Respuesta: {responseText}";
            }

            return $"Aconex Register Document falló: {statusCode}. {responseText}";
        }

        private static bool TryParseMissingMandatoryField(string responseText, out string xmlField)
        {
            xmlField = null;
            if (string.IsNullOrWhiteSpace(responseText)) return false;

            const string marker = "Mandatory field ";
            int idx = responseText.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return false;

            int start = idx + marker.Length;
            int end = responseText.IndexOf('<', start);
            if (end < 0) end = responseText.Length;

            xmlField = responseText.Substring(start, end - start).Trim();
            return !string.IsNullOrWhiteSpace(xmlField);
        }

        /// <summary>
        /// Parsea la respuesta XML del Register Document y devuelve el documentId (RegisterDocumentResult).
        /// </summary>
        private static string ParseRegisterDocumentResponse(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText)) return null;
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(responseText);
                XmlNode node = doc.SelectSingleNode("//RegisterDocumentResult") ?? doc.SelectSingleNode("/*[local-name()='RegisterDocumentResult']");
                if (node != null)
                    return node.InnerText.Trim();
                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Body para envío: metadata del documento + archivo en base64. Serializable a JSON.
    /// </summary>
    public class FileUploadWithMetadataBody
    {
        /// <summary>Metadata del documento (nombre de columna -> valor). Fechas en ISO8601, bytes en base64.</summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>Contenido del archivo codificado en base64.</summary>
        public string FileBase64 { get; set; }

        /// <summary>Nombre del archivo (ej. documento.pdf).</summary>
        public string FileName { get; set; }
    }

    /// <summary>
    /// Un campo del schema <c>EntityCreationSchemaFields</c> del GET register/schema de Aconex.
    /// </summary>
    public sealed class AconexRegisterSchemaField
    {
        public string Identifier { get; set; }
        public string MandatoryStatus { get; set; }
        public string DataType { get; set; }
        public bool IsMultiValue { get; set; }
    }

    /// <summary>
    /// Par Id/Value de un campo tipo lista en el schema (p. ej. estados y tipos de documento del proyecto).
    /// </summary>
    public sealed class AconexSchemaValueOption
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// Resultado del parseo de <c>register/schema</c>: campos de creación + listas de valores permitidos por identificador.
    /// </summary>
    public sealed class AconexRegisterSchemaSnapshot
    {
        /// <summary>Atributo <c>autoNumberingEnabled</c> del nodo <c>RegisterSchema</c> en GET register/schema.</summary>
        public bool AutoNumberingEnabled { get; set; }

        public IReadOnlyList<AconexRegisterSchemaField> Fields { get; set; }
        /// <summary>Clave = <see cref="AconexRegisterSchemaField.Identifier"/> (ej. DocumentStatusId, DocumentTypeId).</summary>
        public IReadOnlyDictionary<string, IReadOnlyList<AconexSchemaValueOption>> PicklistsByIdentifier { get; set; }
    }

    /// <summary>
    /// Parsea el XML del endpoint <c>GET /api/projects/{{id}}/register/schema</c> y extrae los campos de creación de documento.
    /// </summary>
    public static class AconexRegisterSchemaParser
    {
        /// <summary>
        /// Parsea campos de creación y listas SchemaValue (Id/Value) por identificador.
        /// </summary>
        public static AconexRegisterSchemaSnapshot ParseSnapshot(string schemaXml)
        {
            var fields = ParseEntityCreationFields(schemaXml);
            var picklists = ParsePicklistValuesByIdentifier(schemaXml);
            return new AconexRegisterSchemaSnapshot
            {
                AutoNumberingEnabled = ParseAutoNumberingEnabled(schemaXml),
                Fields = fields,
                PicklistsByIdentifier = picklists
            };
        }

        private static bool ParseAutoNumberingEnabled(string schemaXml)
        {
            if (string.IsNullOrWhiteSpace(schemaXml)) return false;
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(schemaXml);
                XmlNode root = doc.SelectSingleNode("//*[local-name()='RegisterSchema']");
                if (root?.Attributes == null) return false;
                XmlNode a = root.Attributes.GetNamedItem("autoNumberingEnabled");
                return a != null && string.Equals(a.Value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Por cada campo bajo EntityCreationSchemaFields, recoge los <c>SchemaValue</c> (Id + Value) agrupados por <c>Identifier</c>.
        /// </summary>
        public static IReadOnlyDictionary<string, IReadOnlyList<AconexSchemaValueOption>> ParsePicklistValuesByIdentifier(string schemaXml)
        {
            var lists = new Dictionary<string, List<AconexSchemaValueOption>>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(schemaXml))
                return EmptyPicklists();

            var doc = new XmlDocument();
            doc.LoadXml(schemaXml);

            XmlNode container = doc.SelectSingleNode("//*[local-name()='EntityCreationSchemaFields']");
            if (container == null)
                return EmptyPicklists();

            XmlNodeList nodes = container.SelectNodes(".//*[local-name()='SingleValueSchemaField' or local-name()='MultiValueSchemaField']");
            if (nodes == null || nodes.Count == 0)
                return EmptyPicklists();

            foreach (XmlNode n in nodes)
            {
                string id = GetChildText(n, "Identifier");
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                id = id.Trim();

                if (!lists.TryGetValue(id, out List<AconexSchemaValueOption> list))
                {
                    list = new List<AconexSchemaValueOption>();
                    lists[id] = list;
                }

                XmlNodeList schemaValues = n.SelectNodes(".//*[local-name()='SchemaValue']");
                if (schemaValues == null) continue;

                foreach (XmlNode sv in schemaValues)
                {
                    string vid = GetChildText(sv, "Id");
                    string vval = GetChildText(sv, "Value");
                    if (string.IsNullOrWhiteSpace(vid) && string.IsNullOrWhiteSpace(vval))
                        continue;
                    list.Add(new AconexSchemaValueOption { Id = vid?.Trim(), Value = vval?.Trim() });
                }
            }

            var result = new Dictionary<string, IReadOnlyList<AconexSchemaValueOption>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in lists)
                result[kv.Key] = kv.Value;

            return result;
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<AconexSchemaValueOption>> EmptyPicklists()
        {
            return new Dictionary<string, IReadOnlyList<AconexSchemaValueOption>>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Extrae <see cref="SingleValueSchemaField"/> y <see cref="MultiValueSchemaField"/> bajo <c>EntityCreationSchemaFields</c>, en orden de aparición.
        /// </summary>
        public static IReadOnlyList<AconexRegisterSchemaField> ParseEntityCreationFields(string schemaXml)
        {
            if (string.IsNullOrWhiteSpace(schemaXml))
                return Array.Empty<AconexRegisterSchemaField>();

            var doc = new XmlDocument();
            doc.LoadXml(schemaXml);

            XmlNode container = doc.SelectSingleNode("//*[local-name()='EntityCreationSchemaFields']");
            if (container == null)
                return Array.Empty<AconexRegisterSchemaField>();

            XmlNodeList nodes = container.SelectNodes(".//*[local-name()='SingleValueSchemaField' or local-name()='MultiValueSchemaField']");
            if (nodes == null || nodes.Count == 0)
                return Array.Empty<AconexRegisterSchemaField>();

            var list = new List<AconexRegisterSchemaField>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (XmlNode n in nodes)
            {
                string id = GetChildText(n, "Identifier");
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                id = id.Trim();
                if (seen.Contains(id))
                    continue;
                seen.Add(id);

                bool isMulti = string.Equals(n.LocalName, "MultiValueSchemaField", StringComparison.OrdinalIgnoreCase);
                string mandatory = GetMandatoryStatus(n);
                string dataType = GetChildText(n, "DataType") ?? "STRING";

                list.Add(new AconexRegisterSchemaField
                {
                    Identifier = id,
                    MandatoryStatus = mandatory ?? "NOT_MANDATORY",
                    DataType = dataType.Trim(),
                    IsMultiValue = isMulti
                });
            }

            return list;
        }

        private static string GetMandatoryStatus(XmlNode fieldNode)
        {
            XmlNode m = fieldNode.SelectSingleNode(".//*[local-name()='MandatoryStatus']");
            if (m != null && !string.IsNullOrWhiteSpace(m.InnerText))
                return m.InnerText.Trim();

            if (fieldNode.Attributes != null)
            {
                foreach (XmlAttribute a in fieldNode.Attributes)
                {
                    if (string.Equals(a.LocalName, "MandatoryStatus", StringComparison.OrdinalIgnoreCase))
                        return a.Value?.Trim();
                }
            }

            XmlNode attrs = fieldNode.SelectSingleNode(".//*[local-name()='Attributes']");
            if (attrs != null)
            {
                m = attrs.SelectSingleNode(".//*[local-name()='MandatoryStatus']");
                if (m != null && !string.IsNullOrWhiteSpace(m.InnerText))
                    return m.InnerText.Trim();
            }

            return null;
        }

        private static string GetChildText(XmlNode parent, string localName)
        {
            if (parent == null) return null;
            XmlNode n = parent.SelectSingleNode("./*[local-name()='" + localName + "']");
            if (n == null || string.IsNullOrWhiteSpace(n.InnerText))
                return null;
            return n.InnerText.Trim();
        }
    }
}
