using SigmabotSync.Domain.Config;
using SigmabotSync.Domain.Models.Extraction;
using SigmabotSync.Domain.Ports;
using SigmabotSync.Application.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SigmabotSync.Application.Extraction
{
    public class DocumentExtractionWorker
    {
        /// <summary>Mappings por defecto cuando DocumentFieldMappings no está configurado.</summary>
        private static readonly List<DocumentFieldMapping> DefaultFieldMappings = new List<DocumentFieldMapping>
        {
            new DocumentFieldMapping { ApiField = "docno", JsonProperty = "documentNumber", DbColumn = "DocumentNumber" },
            new DocumentFieldMapping { ApiField = "title", JsonProperty = "title", DbColumn = "Title" },
            new DocumentFieldMapping { ApiField = "revision", JsonProperty = "revision", DbColumn = "Revision" },
            new DocumentFieldMapping { ApiField = "versionnumber", JsonProperty = "versionNumber", DbColumn = "VersionNumber" }
        };

        private readonly Dictionary<string, string> _config;
        private readonly SqlConnection _dbConDocs;
        private readonly List<DocumentFieldMapping> _fieldMappings;
        private readonly IAconexRegisterSearchPort _registerSearchPort;

        private DataTable DocumentosTmp;
        private DataTable Metadatatmp;

        public DocumentExtractionWorker(
            Dictionary<string, string> config,
            string connectionString,
            IAconexRegisterSearchPort registerSearchPort)
        {
            _config = config;
            _dbConDocs = new SqlConnection(connectionString);
            _fieldMappings = GetConfiguredFieldMappings(config);
            _registerSearchPort = registerSearchPort ?? throw new ArgumentNullException(nameof(registerSearchPort));
        }

        /// <summary>Obtiene los mappings desde config (JSON). Si no hay o falla el parse, usa DefaultFieldMappings.</summary>
        private static List<DocumentFieldMapping> GetConfiguredFieldMappings(Dictionary<string, string> config)
        {
            string raw;
            if (config == null || !config.TryGetValue("DocumentFieldMappings", out raw) || string.IsNullOrWhiteSpace(raw))
                return new List<DocumentFieldMapping>(DefaultFieldMappings);
            try
            {
                var list = JsonConvert.DeserializeObject<List<DocumentFieldMapping>>(raw);
                if (list != null && list.Count > 0)
                    return list;
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Documentos: no se pudo deserializar DocumentFieldMappings, usando valores por defecto: {ex.Message}", 0);
            }
            return new List<DocumentFieldMapping>(DefaultFieldMappings);
        }

        public void Documentos(string proyectID)
        {
            dbchecktmpTables();
            dbcleartmptables();
            datosactuales(proyectID);
            GetACXDocumentsAsync(proyectID)
                .GetAwaiter()
                .GetResult();
            dbUpdateProjectData(proyectID);
        }

        private void dbchecktmpTables()
        {
            DocumentosTmp?.Clear();
            DocumentosTmp = null;

            Metadatatmp?.Clear();
            Metadatatmp = null;

            DocumentosTmp = new DataTable("Documentos_tmp");

            // Siempre: Id, ACXProjectId, TrackingId (no entran por settings)
            DocumentosTmp.Columns.Add("Id", typeof(long));
            DocumentosTmp.Columns.Add("ACXProjectId", typeof(string));
            DocumentosTmp.Columns.Add("TrackingId", typeof(long));

            // Columnas según DocumentFieldMappings (DbColumn), todas como string
            foreach (var mapping in _fieldMappings)
            {
                if (string.IsNullOrWhiteSpace(mapping?.DbColumn)) continue;
                if (!DocumentosTmp.Columns.Contains(mapping.DbColumn))
                    DocumentosTmp.Columns.Add(mapping.DbColumn, typeof(string));
            }

            if (_dbConDocs.State == ConnectionState.Closed)
                _dbConDocs.Open();

            _dbConDocs.Close();
        }


        private void dbcleartmptables()
        {
            // Si DocumentosTmp no es null, limpiar filas
            if (DocumentosTmp != null)
            {
                DocumentosTmp.Clear();
            }
        }

        private void datosactuales(string projectId)
        {
            try
            {
                var actualDB = new DataTable();

                if (_dbConDocs.State == ConnectionState.Closed)
                {
                    _dbConDocs.Open();  // ? usar la misma conexi�n
                }

                using (var da = new SqlDataAdapter(
                    "SELECT COUNT(*) AS total FROM Documentos WHERE [ACXProjectId] = @projid", _dbConDocs))
                {
                    da.SelectCommand.Parameters.AddWithValue("@projid", projectId);
                    da.Fill(actualDB);
                }

                if (actualDB.Rows.Count > 0)
                {
                    var totaldocs = actualDB.Rows[0]["total"].ToString();
                    Utilities.Wlog($"Documentos: Total de documentos antes del proceso {totaldocs}", 1);
                }
                else
                {
                    Utilities.Wlog("Documentos: Total de documentos antes del proceso (sin filas devueltas)", 1);
                }
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Documentos: ERROR {{datos actuales}}: {ex.Message}", 0);
            }
        }


        private async Task GetACXDocumentsAsync(string projectID)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            var stopwatch = new System.Diagnostics.Stopwatch();

            try
            {
                string authcode = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(_config["ACXUser"] + ":" + _config["ACXPass"]));


                Utilities.Wlog($"Inicio GetACXDocuments para {projectID}", 1);
                stopwatch.Restart();
                await GetDocumentsAllAsync(projectID, authcode);
                stopwatch.Stop();
                Utilities.Wlog($"[Documents] {DateTime.Now} Finaliz� GetACXDocuments para {projectID} en {stopwatch.Elapsed.Minutes:D2}:{stopwatch.Elapsed.Seconds:D2} (mm:ss)", 1);
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Documentos: ERROR {{GetACXDocuments}}: {_config["NombrePrj"]} ({projectID}) Mensaje: {ex.Message}", 0);
            }
        }

        public void dbUpdateProjectData(string projid)
        {
            if (_dbConDocs.State == ConnectionState.Closed)
            {
                _dbConDocs.Open();
            }

            SqlTransaction transaction = null;

            try
            {
                if (DocumentosTmp.Rows.Count > 0)
                {
                    Utilities.Wlog($"Documentos: {DocumentosTmp.Rows.Count} documentos rescatados", 1);
                    AppState.totDoctosDescar = DocumentosTmp.Rows.Count;

                    using (SqlBulkCopy s = new SqlBulkCopy(_dbConDocs))
                    {
                        s.DestinationTableName = "Documentos_tmp";
                        s.ColumnMappings.Clear();

                        // Mapear cada columna por nombre para que no dependa del orden
                        foreach (DataColumn col in DocumentosTmp.Columns)
                        {
                            s.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                        }

                        s.WriteToServer(DocumentosTmp);
                        s.Close();
                    }

                    // Delete old data
                    transaction = _dbConDocs.BeginTransaction("BorraDocumentos");
                    using (SqlCommand sc = new SqlCommand($"DELETE Documentos WHERE ACXProjectId='{projid}'", _dbConDocs, transaction))
                    {
                        sc.ExecuteNonQuery();
                        transaction.Commit();
                    }

                    // Copy from tmp to final (columnas: Id, ACXProjectId, TrackingId + DbColumn de DocumentFieldMappings)
                    var cols = DocumentosTmp.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
                    var colList = string.Join(", ", cols);
                    transaction = _dbConDocs.BeginTransaction("CopiaDocumentos");
                    var insertSql = string.Format(
                        "INSERT INTO [Documentos] ({0}) SELECT {0} FROM Documentos_tmp",
                        colList);
                    using (SqlCommand sc = new SqlCommand(insertSql, _dbConDocs, transaction))
                    {
                        sc.ExecuteNonQuery();
                        transaction.Commit();
                    }


                    // Truncate temp table
                    transaction = _dbConDocs.BeginTransaction("borradocumentostemp");
                    using (SqlCommand sc = new SqlCommand("TRUNCATE TABLE Documentos_tmp", _dbConDocs, transaction))
                    {
                        sc.ExecuteNonQuery();
                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Documentos: ERROR {{dbUpdateProjectData}}: proyecto: {_config["NombrePrj"]}: {ex.Message}", 0);
            }

            _dbConDocs.Close();
        }

        private readonly object _lockDocTmp = new object();

        public async Task<bool> GetDocumentsAllAsync(string projid, string authcode)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            string baseUrl = _config.ContainsKey("AconexBaseUrl") && !string.IsNullOrWhiteSpace(_config["AconexBaseUrl"])
                ? _config["AconexBaseUrl"].TrimEnd('/')
                : "https://us1.aconex.com";

            try
            {
                // --- Obtener la primera p�gina con reintento ---
                var firstPage = await Utilities.EjecutarConReintentosAsync(
                    () => GetPageAsync(projid, 1, authcode, baseUrl),
                    $"Documentos: Error al obtener primera p�gina del proyecto {projid}"
                );

                if (firstPage == null)
                    return false;

                int totalPages = firstPage.totalNumberOfPages;
                long allDocs = firstPage.totalResultsCount;
                long processedDocs = 0;

                var semaphore = new SemaphoreSlim(5);
                var tasks = new List<Task>();

                // --- Bucle general (1..totalPages) ---
                for (int page = 1; page <= totalPages; page++)
                {
                    await semaphore.WaitAsync();
                    int currentPage = page;

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var pageData = currentPage == 1
                                ? firstPage
                                : await Utilities.EjecutarConReintentosAsync(
                                    () => GetPageAsync(projid, currentPage, authcode, baseUrl),
                                    $"Documentos: Error al obtener p�gina {currentPage} del proyecto {projid}"
                                  );

                            if (pageData != null)
                            {
                                lock (_lockDocTmp)
                                {
                                    foreach (var doc in pageData.searchResults)
                                    {
                                        try
                                        {
                                            AgregaDocumentoNuevo(doc, projid);
                                            processedDocs++;
                                        }
                                        catch (Exception ex)
                                        {
                                            Utilities.Wlog($"Documentos: ERROR al procesar doc {doc.Id} en proyecto {projid}, p�gina {currentPage}: {ex.Message}", 0);
                                        }
                                    }
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            Utilities.Wlog($"Documentos: ERROR al obtener la p�gina {currentPage} del proyecto {projid}: {ex.Message}", 0);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                await Task.WhenAll(tasks);

                AppState.TotDoctosAconex = processedDocs;
                return true;
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Documentos: ERROR general en proyecto {projid}: {ex.Message}", 0);
                return false;
            }
        }

        private async Task<Rootobject> GetPageAsync(string projid, int page, string authcode, string baseUrl)
        {
            // trackingid siempre; el resto según ApiField de los mappings
            var returnFieldList = new List<string> { "trackingid" };
            foreach (var m in _fieldMappings)
            {
                if (!string.IsNullOrWhiteSpace(m?.ApiField))
                    returnFieldList.Add(m.ApiField);
            }
            string[] returnFields = returnFieldList.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            string orgid = _config["OrgId"];
            string userId = _config["userid"];

            var result = await _registerSearchPort.SearchRegisterPageAsync(
                baseUrl,
                projid,
                orgid,
                userId,
                authcode,
                returnFields,
                300,
                page,
                throwIfNotSuccess: false,
                CancellationToken.None).ConfigureAwait(false);
            return result?.Page;
        }

        public void AgregaDocumentoNuevo(Searchresult mdoc, string projectId)
        {
            try
            {
                var row = DocumentosTmp.NewRow();
                row["Id"] = mdoc.Id;
                row["ACXProjectId"] = projectId;
                row["TrackingId"] = mdoc.TrackingId;

                foreach (var mapping in _fieldMappings)
                {
                    if (mapping == null || string.IsNullOrWhiteSpace(mapping.DbColumn) || !DocumentosTmp.Columns.Contains(mapping.DbColumn))
                        continue;
                    object val = GetMappedValue(mapping, mdoc);
                    if (val != null && val != DBNull.Value)
                        row[mapping.DbColumn] = val;
                }
                DocumentosTmp.Rows.Add(row);
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Documentos: ERROR {{AgregaDocumentoNuevo}}:{projectId}:{ex.Message}", 0);
            }
        }

        /// <summary>Obtiene el valor del documento según el mapping: propiedades directas, projectFields o ExtensionData.</summary>
        private static object GetMappedValue(DocumentFieldMapping mapping, Searchresult mdoc)
        {
            if (mapping == null || mdoc == null) return DBNull.Value;

            var jsonProp = mapping.JsonProperty ?? mapping.ApiField;
            var apiField = mapping.ApiField ?? mapping.JsonProperty;
            if (string.IsNullOrWhiteSpace(jsonProp)) return DBNull.Value;

            // 1) Propiedades directas del modelo (documentNumber, title, revision, trackingid)
            if (string.Equals(jsonProp, "documentNumber", StringComparison.OrdinalIgnoreCase))
                return mdoc.DocumentNumber ?? (object)DBNull.Value;
            if (string.Equals(jsonProp, "title", StringComparison.OrdinalIgnoreCase))
                return mdoc.Title ?? (object)DBNull.Value;
            if (string.Equals(jsonProp, "revision", StringComparison.OrdinalIgnoreCase))
                return mdoc.Revision ?? (object)DBNull.Value;
            if (string.Equals(jsonProp, "trackingid", StringComparison.OrdinalIgnoreCase))
                return mdoc.TrackingId;

            // 2) projectFields (campos custom del proyecto)
            if (mdoc.ProjectFields != null)
            {
                var pf = mdoc.ProjectFields.FirstOrDefault(p =>
                    string.Equals(p.Name, jsonProp, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.Name, apiField, StringComparison.OrdinalIgnoreCase));
                if (pf != null && pf.Value != null)
                    return pf.Value;
            }

            // 3) ExtensionData (resto de campos que vienen en el JSON)
            var fromExt = GetValueFromExtensionData(mdoc, jsonProp) ?? GetValueFromExtensionData(mdoc, apiField);
            return fromExt ?? (object)DBNull.Value;
        }

        private static object GetValueFromExtensionData(Searchresult mdoc, string key)
        {
            if (mdoc.ExtensionData == null || string.IsNullOrEmpty(key)) return null;
            JToken token;
            if (mdoc.ExtensionData.TryGetValue(key, out token) && token != null)
            {
                if (token is JValue jv) return jv.Value != null ? jv.Value.ToString() : (object)null;
                return token.ToString();
            }
            return null;
        }
    }
}
