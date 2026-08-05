using SigmabotSync.Domain.Models.Extraction;
using SigmabotSync.Domain.Ports;
using SigmabotSync.Application.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace SigmabotSync.Application.Extraction
{
    public class MailExtractionWorker
    {
        /// <summary>Misma clave que el código histórico para GET con adjuntos y páginas (no usada en GetMaxPages).</summary>
        private const string LegacyMailApplicationKey = "a7f7bf46-a848-4b7a-ae8c-ed55b3952010";

        private Dictionary<string, string> _config;
        private readonly SqlConnection _dbConMails;
        private readonly IAconexHttpGetPort _httpGet;

        private DataTable DestinatariosTmp = new DataTable();
        private DataTable CorreosRecibidosTmp = new DataTable();
        private DataTable CorreosEnviadosTmp = new DataTable();
        private DataTable DocumentosEnviadosTmp = new DataTable();
        private DataTable DocumentosRecibidosTmp = new DataTable();

        private readonly ConcurrentBag<CorreoDto> _bagCorreosRecibidos = new ConcurrentBag<CorreoDto>();
        private readonly ConcurrentBag<CorreoDto> _bagCorreosEnviados = new ConcurrentBag<CorreoDto>();
        private readonly ConcurrentBag<DestinatarioDto> _bagDestinatarios = new ConcurrentBag<DestinatarioDto>();
        private readonly ConcurrentBag<DocumentoDto> _bagDocumentosRecibidos = new ConcurrentBag<DocumentoDto>();
        private readonly ConcurrentBag<DocumentoDto> _bagDocumentosEnviados = new ConcurrentBag<DocumentoDto>();


        private List<ControlProcesoCorreos> controles = new List<ControlProcesoCorreos>();


        private HashSet<string> _mailIdsInbox;
        private HashSet<string> _mailIdsSent;

        private string _fechaInicio;
        private string _fechaFin;

        public MailExtractionWorker(Dictionary<string, string> config, string connectionString, IAconexHttpGetPort httpGet)
        {
            _config = config;
            _dbConMails = new SqlConnection(connectionString);
            _httpGet = httpGet ?? throw new ArgumentNullException(nameof(httpGet));
        }

        public void Correos(string proyectID)
        {
            ResetCorreosAppState();
            DbCheckTmpTables();
            DatosActuales(proyectID);

            _mailIdsInbox = CargarMailIdsExistentesDeDB(proyectID, "inbox");
            _mailIdsSent = CargarMailIdsExistentesDeDB(proyectID, "sentbox");

            GetTransmittalsOptimizedWithRetry(proyectID);

            // 2. Mapear los ConcurrentBag a DataTables
            MapBagsToDataTables();

            // 3. Insertar en las tablas finales
            DbUpdateProjectData(proyectID);

            Utilities.Wlog($"INBOX: Obtenidos={AppState.totalCorreosRecibidosAconex}, Excluidos={AppState.totalCorreosRecibidosDescartados}, Procesados={AppState.totalCorreosRecibidosProcesados}, Documentos={DocumentosRecibidosTmp.Rows.Count}", 1);
            Utilities.Wlog($"SENTBOX: Obtenidos={AppState.totalCorreosEnviadosAconex}, Excluidos={AppState.totalCorreosEnviadosDescartados}, Procesados={AppState.totalCorreosEnviadosProcesados}, Documentos={DocumentosEnviadosTmp.Rows.Count}", 1);
        }

        private static void ResetCorreosAppState()
        {
            AppState.totalCorreosRecibidosProcesados = 0;
            AppState.totalCorreosEnviadosProcesados = 0;
            AppState.totalCorreosRecibidosDescartados = 0;
            AppState.totalCorreosEnviadosDescartados = 0;
            AppState.totalCorreosRecibidosAconex = 0;
            AppState.totalCorreosEnviadosAconex = 0;
        }

        private void DbCheckTmpTables()
        {
            if (CorreosRecibidosTmp != null)
            {
                CorreosRecibidosTmp.Clear();
                CorreosRecibidosTmp = null;
            }

            CorreosRecibidosTmp = new DataTable("CorreosRecibidos_tmp");
            CorreosRecibidosTmp.Columns.Add(new DataColumn("ACXProjectId", typeof(string)));
            CorreosRecibidosTmp.Columns.Add(new DataColumn("MailId", typeof(string)));
            CorreosRecibidosTmp.Columns.Add(new DataColumn("MailNo", typeof(string)));
            CorreosRecibidosTmp.Columns.Add(new DataColumn("FromOrganizationName", typeof(string)));
            CorreosRecibidosTmp.Columns.Add(new DataColumn("FromName", typeof(string)));
            CorreosRecibidosTmp.Columns.Add(new DataColumn("ReasonForIssue", typeof(string)));
            CorreosRecibidosTmp.Columns.Add(new DataColumn("ReferenceNumber", typeof(string)));
            CorreosRecibidosTmp.Columns.Add(new DataColumn("SentDate", typeof(string)));
            CorreosRecibidosTmp.Columns.Add(new DataColumn("Subject", typeof(string)));
            CorreosRecibidosTmp.Columns.Add(new DataColumn("CorrespondenceType", typeof(string)));

            if (CorreosEnviadosTmp != null)
            {
                CorreosEnviadosTmp.Clear();
                CorreosEnviadosTmp = null;
            }

            CorreosEnviadosTmp = new DataTable("CorreosEnviados_tmp");
            CorreosEnviadosTmp.Columns.Add(new DataColumn("ACXProjectId", typeof(string)));
            CorreosEnviadosTmp.Columns.Add(new DataColumn("MailId", typeof(string)));
            CorreosEnviadosTmp.Columns.Add(new DataColumn("MailNo", typeof(string)));
            CorreosEnviadosTmp.Columns.Add(new DataColumn("FromOrganizationName", typeof(string)));
            CorreosEnviadosTmp.Columns.Add(new DataColumn("FromName", typeof(string)));
            CorreosEnviadosTmp.Columns.Add(new DataColumn("ReasonForIssue", typeof(string)));
            CorreosEnviadosTmp.Columns.Add(new DataColumn("ReferenceNumber", typeof(string)));
            CorreosEnviadosTmp.Columns.Add(new DataColumn("SentDate", typeof(string)));
            CorreosEnviadosTmp.Columns.Add(new DataColumn("Subject", typeof(string)));
            CorreosEnviadosTmp.Columns.Add(new DataColumn("CorrespondenceType", typeof(string)));

            if (DocumentosRecibidosTmp != null)
            {
                DocumentosRecibidosTmp.Clear();
                DocumentosRecibidosTmp = null;
            }

            DocumentosRecibidosTmp = new DataTable("DocumentosRecibidos_Tmp");
            DocumentosRecibidosTmp.Columns.Add(new DataColumn("ACXProjectId", typeof(string)));
            DocumentosRecibidosTmp.Columns.Add(new DataColumn("MailId", typeof(string)));
            DocumentosRecibidosTmp.Columns.Add(new DataColumn("DocumentNo", typeof(string)));
            DocumentosRecibidosTmp.Columns.Add(new DataColumn("FileName", typeof(string)));
            DocumentosRecibidosTmp.Columns.Add(new DataColumn("FileSize", typeof(string)));
            DocumentosRecibidosTmp.Columns.Add(new DataColumn("Revision", typeof(string)));
            DocumentosRecibidosTmp.Columns.Add(new DataColumn("RevisionDate", typeof(string)));
            DocumentosRecibidosTmp.Columns.Add(new DataColumn("Title", typeof(string)));

            if (DocumentosEnviadosTmp != null)
            {
                DocumentosEnviadosTmp.Clear();
                DocumentosEnviadosTmp = null;
            }

            DocumentosEnviadosTmp = new DataTable("DocumentosEnviados_Tmp");
            DocumentosEnviadosTmp.Columns.Add(new DataColumn("ACXProjectId", typeof(string)));
            DocumentosEnviadosTmp.Columns.Add(new DataColumn("MailId", typeof(string)));
            DocumentosEnviadosTmp.Columns.Add(new DataColumn("DocumentNo", typeof(string)));
            DocumentosEnviadosTmp.Columns.Add(new DataColumn("FileName", typeof(string)));
            DocumentosEnviadosTmp.Columns.Add(new DataColumn("FileSize", typeof(string)));
            DocumentosEnviadosTmp.Columns.Add(new DataColumn("Revision", typeof(string)));
            DocumentosEnviadosTmp.Columns.Add(new DataColumn("RevisionDate", typeof(string)));
            DocumentosEnviadosTmp.Columns.Add(new DataColumn("Title", typeof(string)));

            if (DestinatariosTmp != null)
            {
                DestinatariosTmp.Clear();
                DestinatariosTmp = null;
            }

            DestinatariosTmp = new DataTable("Destinatarios_Tmp");
            DestinatariosTmp.Columns.Add(new DataColumn("ACXProjectId", typeof(string)));
            DestinatariosTmp.Columns.Add(new DataColumn("MailId", typeof(string)));
            DestinatariosTmp.Columns.Add(new DataColumn("MailNo", typeof(string)));
            DestinatariosTmp.Columns.Add(new DataColumn("ACXUserId", typeof(string)));
            DestinatariosTmp.Columns.Add(new DataColumn("UserName", typeof(string)));
            DestinatariosTmp.Columns.Add(new DataColumn("Organization", typeof(string)));
        }

        private void DatosActuales(string projid)
        {
            try
            {
                if (_dbConMails.State == ConnectionState.Closed)
                {
                    _dbConMails.Open();
                }

                // Recibidos
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM CorreosRecibidos WHERE [ACXProjectId] = @projid", _dbConMails))
                {
                    cmd.Parameters.AddWithValue("@projid", projid);
                    var total = Convert.ToInt32(cmd.ExecuteScalar());
                    Utilities.Wlog("Correos: Transmisiones Recibidas antes del proceso " + total, 1);
                }

                // Enviados
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM CorreosEnviados WHERE [ACXProjectId] = @projid", _dbConMails))
                {
                    cmd.Parameters.AddWithValue("@projid", projid);
                    var total = Convert.ToInt32(cmd.ExecuteScalar());
                    Utilities.Wlog("Correos: Transmisiones Enviadas antes del proceso " + total, 1);
                }
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Correos: ERROR {{DatosActuales}}: {_config["NombrePrj"]} ({projid}): {ex.Message}", 0);
            }
        }

        public long GetMaxPages(string projid, string authcode, string mailbox)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string uri = $"https://us1.aconex.com/api/projects/{projid}/mail?mail_box={mailbox}&page_size=300&search_type=PAGED&search_query=corrtypeid:23 AND sentdate:[{_fechaInicio} TO {_fechaFin}]";

            try
            {
                string responseFromServer = _httpGet.GetStringAsync(new AconexHttpGetRequest
                {
                    Url = uri,
                    AuthorizationHeaderBase64 = authcode,
                    Accept = "application/xml",
                    ContentType = "application/vnd.aconex.mail.v3+xml",
                    ExtraHeaders = null
                }).GetAwaiter().GetResult();

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(responseFromServer);

                long tresult = long.Parse(doc.SelectSingleNode("MailSearch")?.Attributes?["TotalResults"]?.InnerText ?? "0");

                if (mailbox == "inbox")
                    AppState.totalCorreosRecibidosAconex = tresult;
                else
                    AppState.totalCorreosEnviadosAconex = tresult;

                if (tresult > 0)
                {
                    long totalPages = long.Parse(doc.SelectSingleNode("MailSearch")?.Attributes?["TotalPages"]?.InnerText ?? "0");
                    return totalPages;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Correos: ERROR al obtener total de páginas (GetMaxPages): {ex.Message}", 0);
                return 0;
            }
        }

        public void DbUpdateProjectData(string projId)
        {
            if (_dbConMails.State == ConnectionState.Closed)
                _dbConMails.Open();

            try
            {
                // --- Correos Recibidos ---
                SyncTempToFinal(
                    projId,
                    CorreosRecibidosTmp,
                    "CorreosRecibidos_tmp",
                    "CorreosRecibidos",
                    "([ACXProjectId],[MailId],[MailNo],[FromOrganizationName],[FromName],[ReasonForIssue],[ReferenceNumber],[SentDate],[Subject],CorrespondenceType)",
                    "f.ACXProjectId = t.ACXProjectId AND f.MailId = t.MailId");

                // --- Correos Enviados ---
                SyncTempToFinal(
                    projId,
                    CorreosEnviadosTmp,
                    "CorreosEnviados_tmp",
                    "CorreosEnviados",
                    "([ACXProjectId],[MailId],[MailNo],[FromOrganizationName],[FromName],[ReasonForIssue],[ReferenceNumber],[SentDate],[Subject],CorrespondenceType)",
                    "f.ACXProjectId = t.ACXProjectId AND f.MailId = t.MailId");

                // --- Documentos Recibidos ---
                SyncTempToFinal(
                    projId,
                    DocumentosRecibidosTmp,
                    "DocumentosRecibidos_tmp",
                    "DocumentosRecibidos",
                    "([ACXProjectId],MailId,DocumentNo,[FileName],FileSize,Revision,RevisionDate,Title)",
                    "f.ACXProjectId = t.ACXProjectId AND f.MailId = t.MailId AND f.DocumentNo = t.DocumentNo AND f.FileName = t.FileName");

                // --- Documentos Enviados ---
                SyncTempToFinal(
                    projId,
                    DocumentosEnviadosTmp,
                    "DocumentosEnviados_tmp",
                    "DocumentosEnviados",
                    "([ACXProjectId],MailId,DocumentNo,[FileName],FileSize,Revision,RevisionDate,Title)",
                    "f.ACXProjectId = t.ACXProjectId AND f.MailId = t.MailId AND f.DocumentNo = t.DocumentNo AND f.FileName = t.FileName");

                // --- Destinatarios ---
                SyncTempToFinal(
                    projId,
                    DestinatariosTmp,
                    "Destinatarios_tmp",
                    "Destinatarios",
                    "([ACXProjectId],[MailId],[MailNo],[ACXUserId],[UserName],[Organization])",
                    "f.ACXProjectId = t.ACXProjectId AND f.MailId = t.MailId AND f.ACXUserId = t.ACXUserId");

                AppState.totalCorreosRecibidosProcesados = CorreosRecibidosTmp.Rows.Count;
                AppState.totalCorreosEnviadosProcesados = CorreosEnviadosTmp.Rows.Count;

                foreach (var control in controles)
                {
                    GuardarControlCorreos(control);
                }
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Correos: ERROR {{dbUpdateProjectData}}: proyecto: {_config["NombrePrj"]}:{ex.Message}", 0);
            }
            finally
            {
                _dbConMails.Close();
            }
        }

        private void SyncTempToFinal(
            string projId,
            DataTable sourceTable,
            string tempTableName,
            string finalTableName,
            string columnsWithParens,
            string joinCondition)
        {
            if (sourceTable.Rows.Count == 0)
                return;

            // 1) Limpiar tabla temporal
            using (var tran = _dbConMails.BeginTransaction())
            using (var cmd = new SqlCommand($"TRUNCATE TABLE {tempTableName}", _dbConMails, tran))
            {
                cmd.ExecuteNonQuery();
                tran.Commit();
            }

            // 2) Cargar datos a la temporal
            using (var bulk = new SqlBulkCopy(_dbConMails))
            {
                bulk.DestinationTableName = tempTableName;
                bulk.WriteToServer(sourceTable);
            }

            // --- construir lista de columnas con alias t. ---
            var colList = string.Join(",",
                columnsWithParens
                    .Replace("(", "")
                    .Replace(")", "")
                    .Split(',')
                    .Select(c =>
                    {
                        // quita [] para poder agregar alias, luego los vuelve a poner
                        var clean = c.Trim().Trim('[', ']');
                        return $"t.[{clean}]";
                    }));
            // 3) Insertar solo los que no existen en final
            var columnsNoParens = columnsWithParens.Replace("(", "").Replace(")", "").Trim();
            using (var tran = _dbConMails.BeginTransaction())
            using (var cmd = new SqlCommand($@"
                INSERT INTO {finalTableName} {columnsWithParens}
                SELECT {colList}
                FROM {tempTableName} t
                LEFT JOIN {finalTableName} f
                    ON {joinCondition}
                WHERE f.ACXProjectId IS NULL", _dbConMails, tran))
            {
                cmd.ExecuteNonQuery();
                tran.Commit();
            }

            // 4) Opcional limpiar tabla temporal
            using (var tran = _dbConMails.BeginTransaction())
            using (var cmd = new SqlCommand($"TRUNCATE TABLE {tempTableName}", _dbConMails, tran))
            {
                cmd.ExecuteNonQuery();
                tran.Commit();
            }
        }

        // M�todo principal optimizado con reintento
        void GetTransmittalsOptimizedWithRetry(string projid)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string authcode = Utilities.EncodeTexto(_config["ACXUser"] + ":" + _config["ACXPass"]);

            foreach (var mailbox in new[] { "inbox", "sentbox" })
            {
                GetOrCreateControlCorreos(projid, mailbox);

                Utilities.Wlog($"[{mailbox.ToUpper()}] Inicio de GetTransmittals para {projid}", 1);
                var stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();

                long maxpages = GetMaxPages(projid, authcode, mailbox);
                if (maxpages <= 0) continue;

                // Lista de p�ginas fallidas para reintento
                List<long> paginasFallidas = new List<long>();

                for (long pagina = 1; pagina <= maxpages; pagina++)
                {
                    bool exito = ProcesarPagina(projid, authcode, mailbox, pagina);
                    if (!exito) paginasFallidas.Add(pagina);
                }

                // Reintento de p�ginas fallidas
                foreach (long pagina in paginasFallidas)
                {
                    Utilities.Wlog($"[{mailbox.ToUpper()}] Reintentando p�gina {pagina} para {projid}", 1);
                    ProcesarPagina(projid, authcode, mailbox, pagina);
                }

                stopwatch.Stop();
                Utilities.Wlog($"[{mailbox.ToUpper()}] Finaliz� GetTransmittals para {projid} en {stopwatch.Elapsed.Minutes:D2}:{stopwatch.Elapsed.Seconds:D2}", 1);

            }
        }

        // Procesa una p�gina completa (correos + adjuntos) en paralelo
        bool ProcesarPagina(string projid, string authcode, string mailbox, long pagina)
        {
            try
            {
                string pageXml = GetPageXml(projid, authcode, mailbox, pagina, _fechaInicio, _fechaFin);
                if (string.IsNullOrWhiteSpace(pageXml)) return false;

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(Utilities.CleanXml(pageXml));

                var mails = doc.SelectSingleNode("MailSearch")?
                               .SelectSingleNode("SearchResults")?
                               .SelectNodes("Mail");
                if (mails == null) return true; // No hay mails, pero no es error

                long totalResults = long.Parse(doc.SelectSingleNode("MailSearch")?.Attributes?["TotalResults"]?.InnerText ?? "1");

                long cuanta = 0;
                object progressLock = new object();

                HashSet<string> _mailIdsExistentes = mailbox == "inbox" ? _mailIdsInbox : _mailIdsSent;


                // Paso 1: procesar correos y destinatarios en paralelo
                Parallel.ForEach(mails.Cast<XmlElement>(),
                    new ParallelOptions { MaxDegreeOfParallelism = 8 },
                    mdoc =>
                    {
                        try
                        {
                            string mailId = mdoc.GetAttribute("MailId");

                            // progress bar
                            long tmails;
                            double progreso;
                            lock (progressLock)
                            {
                                cuanta++;
                                tmails = (pagina - 1) * 300 + cuanta;
                                progreso = totalResults > 0 ? (double)tmails / totalResults * 100 : 0;
                            }

                            // filtro duplicados antes de procesar
                            bool yaProcesado;
                            lock (_mailIdsExistentes)
                            {
                                yaProcesado = _mailIdsExistentes.Contains(mailId);
                                if (!yaProcesado)
                                    _mailIdsExistentes.Add(mailId);
                                else
                                {
                                    // ? Contador de excluidos
                                    if (mailbox == "inbox")
                                        Interlocked.Increment(ref AppState.totalCorreosRecibidosDescartados);
                                    else
                                        Interlocked.Increment(ref AppState.totalCorreosEnviadosDescartados);
                                    return;
                                }
                            }

                            ProcesarCorreoSinAdjuntos(mdoc, projid, mailbox);
                            ObtenerAdjuntos(mailId, projid, mailbox, authcode);

                        }
                        catch (Exception ex)
                        {
                            Utilities.Wlog($"Correos: ERROR ParallelMail:{projid} - {ex.Message}", 0);
                        }
                    });

                return true;
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Correos: ERROR ProcesarPagina {mailbox} p�gina {pagina} - {projid} - {ex.Message}", 0);
                return false;
            }
        }

        // Procesar correo y destinatarios sin adjuntos
        void ProcesarCorreoSinAdjuntos(XmlElement mdoc, string projectId, string mailbox)
        {
            try
            {
                var correo = new CorreoDto
                {
                    ACXProjectId = projectId,
                    MailId = mdoc.GetAttribute("MailId"),
                    MailNo = mdoc.SelectSingleNode("MailNo")?.InnerText ?? "",
                    FromOrganizationName = mdoc.SelectSingleNode("FromUserDetails/OrganizationName")?.InnerText ?? "",
                    FromName = mdoc.SelectSingleNode("FromUserDetails/Name")?.InnerText ?? "",
                    ReasonForIssue = mdoc.SelectSingleNode("ReasonForIssue")?.InnerText ?? "",
                    ReferenceNumber = mdoc.SelectSingleNode("ReferenceNumber")?.InnerText ?? "",
                    SentDate = mdoc.SelectSingleNode("SentDate")?.InnerText ?? "",
                    Subject = mdoc.SelectSingleNode("Subject")?.InnerText ?? "",
                    CorrespondenceType = mdoc.SelectSingleNode("CorrespondenceType")?.InnerText ?? "",
                };

                if (mailbox == "inbox")
                    _bagCorreosRecibidos.Add(correo);
                else
                    _bagCorreosEnviados.Add(correo);

                XmlNodeList destinatarios = mdoc.SelectSingleNode("ToUsers")?.SelectNodes("Recipient");
                if (destinatarios != null)
                {
                    foreach (XmlElement touser in destinatarios)
                    {
                        var dest = new DestinatarioDto
                        {
                            ACXProjectId = projectId,
                            MailId = correo.MailId,
                            MailNo = correo.MailNo,
                            ACXUserId = touser.SelectSingleNode("UserId")?.InnerText ?? "",
                            UserName = touser.SelectSingleNode("Name")?.InnerText ?? "",
                            Organization = touser.SelectSingleNode("OrganizationName")?.InnerText ?? ""
                        };
                        _bagDestinatarios.Add(dest);
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Correos: ERROR ProcesarCorreoSinAdjuntos {projectId} - MailId: {mdoc.GetAttribute("MailId")} - {ex.Message}", 0);
            }
        }

        // Descargar adjuntos para un MailId
        void ObtenerAdjuntos(string mailId, string projectId, string mailbox, string authcode)
        {
            string uri = $"https://us1.aconex.com/api/projects/{projectId}/mail/{mailId}";

            Utilities.EjecutarConReintentos(() =>
            {
                string responseXml = _httpGet.GetStringAsync(new AconexHttpGetRequest
                {
                    Url = uri,
                    AuthorizationHeaderBase64 = authcode,
                    Accept = "application/xml",
                    ContentType = "application/vnd.aconex.mail.v3+xml",
                    ExtraHeaders = new[] { ("X-Application-Key", LegacyMailApplicationKey) }
                }).GetAwaiter().GetResult();

                XmlDocument docAdj = new XmlDocument();
                docAdj.LoadXml(Utilities.CleanXml(responseXml));

                XmlNodeList attachments = docAdj.SelectSingleNode("Mail")?
                                                .SelectSingleNode("Attachments")?
                                                .SelectNodes("RegisteredDocumentAttachment");
                if (attachments != null)
                {
                    foreach (XmlElement adoc in attachments)
                    {
                        var adjunto = new DocumentoDto
                        {
                            ACXProjectId = projectId,
                            MailId = mailId,
                            DocumentNo = adoc.SelectSingleNode("DocumentNo")?.InnerText ?? "",
                            FileName = adoc.SelectSingleNode("FileName")?.InnerText ?? "",
                            FileSize = adoc.SelectSingleNode("FileSize")?.InnerText ?? "0",
                            Revision = adoc.SelectSingleNode("Revision")?.InnerText ?? "",
                            RevisionDate = adoc.SelectSingleNode("RevisionDate")?.InnerText ?? "",
                            Title = adoc.SelectSingleNode("Title")?.InnerText ?? ""
                        };

                        if (mailbox == "inbox")
                            _bagDocumentosRecibidos.Add(adjunto);
                        else
                            _bagDocumentosEnviados.Add(adjunto);
                    }
                }

                return true;
            }, $"ObtenerAdjuntos {projectId}/{mailId}");
        }

        //// Descarga XML de p�gina
        string GetPageXml(string projid, string authcode, string mailbox, long page, string fechaInicio, string fechaFin)
        {
            string uri = $"https://us1.aconex.com/api/projects/{projid}/mail?mail_box={mailbox}" +
                         $"&return_fields=docno,sentdate,closedoutdetails,fromUserDetails,mailRecipients," +
                         $"reasonforissueid,subject,inreftomailno,corrtypeid&page_size=300&search_type=PAGED" +
                         $"&search_query=corrtypeid:23 AND sentdate:[{fechaInicio} TO {fechaFin}]&page_number={page}";

            return Utilities.EjecutarConReintentos(() =>
            {
                return _httpGet.GetStringAsync(new AconexHttpGetRequest
                {
                    Url = uri,
                    AuthorizationHeaderBase64 = authcode,
                    Accept = "application/xml",
                    ContentType = "application/vnd.aconex.mail.v3+xml",
                    ExtraHeaders = new[] { ("X-Application-Key", LegacyMailApplicationKey) }
                }).GetAwaiter().GetResult();
            }, $"GetPageXml {projid}/{mailbox}/p�g {page}");
        }

        HashSet<string> CargarMailIdsExistentesDeDB(string projId, string mailbox)
        {
            var hs = new HashSet<string>();
            string tabla = mailbox == "inbox" ? "CorreosRecibidos" : "CorreosEnviados";

            using (var cmd = new SqlCommand($"SELECT MailId FROM {tabla} WHERE ACXProjectId = @projid", _dbConMails))
            {
                cmd.Parameters.AddWithValue("@projid", projId);

                if (_dbConMails.State != ConnectionState.Open)
                    _dbConMails.Open();

                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        // verificamos si la columna 0 es DBNull
                        string valor = rdr.IsDBNull(0) ? string.Empty : rdr.GetString(0);

                        // y solo agregamos si no es vac�o
                        if (!string.IsNullOrEmpty(valor))
                            hs.Add(valor);
                    }
                }
            }

            return hs;
        }



        private ControlProcesoCorreos GetOrCreateControlCorreos(string projId, string mailbox)
        {
            ControlProcesoCorreos control = null;

            bool cerrarConexion = _dbConMails.State != ConnectionState.Open;
            if (cerrarConexion) _dbConMails.Open();

            using (var cmd = new SqlCommand(@"
            SELECT UltimaFecha, RangoDias
            FROM ProcesoCorreosControl
            WHERE ProjId = @ProjId AND Mailbox = @Mailbox", _dbConMails))
            {
                cmd.Parameters.AddWithValue("@ProjId", projId);
                cmd.Parameters.AddWithValue("@Mailbox", mailbox);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        control = new ControlProcesoCorreos
                        {
                            ProjId = projId,
                            Mailbox = mailbox,
                            UltimaFechaProcesada = reader.GetDateTime(0),
                            RangoDias = reader.GetInt32(1)
                        };
                    }
                }
            }

            if (control == null)
            {
                // Valores por defecto si no hay registro
                control = new ControlProcesoCorreos
                {
                    ProjId = projId,
                    Mailbox = mailbox,
                    UltimaFechaProcesada = DateTime.UtcNow.AddDays(-30),
                    RangoDias = 30
                };
            }

            if (cerrarConexion) _dbConMails.Close();

            // Calcular fechas para el endpoint
            _fechaInicio = control.UltimaFechaProcesada.ToString("yyyyMMdd");
            DateTime fechaFinDate = control.UltimaFechaProcesada.AddDays(control.RangoDias);
            if (fechaFinDate > DateTime.UtcNow)
                fechaFinDate = DateTime.UtcNow;

            _fechaFin = fechaFinDate.ToString("yyyyMMdd");
            control.UltimaFechaProcesada = fechaFinDate.Date;

            // Guardar en memoria
            controles.Add(control);

            return control;
        }





        private void GuardarControlCorreos(ControlProcesoCorreos control)
        {
            bool cerrarConexion = _dbConMails.State != ConnectionState.Open;
            if (cerrarConexion) _dbConMails.Open();

            using (var cmd = new SqlCommand(@"
        IF EXISTS (SELECT 1 FROM ProcesoCorreosControl WHERE ProjId = @ProjId AND Mailbox = @Mailbox)
        BEGIN
            UPDATE ProcesoCorreosControl
            SET UltimaFecha = @UltimaFecha,
                RangoDias = @RangoDias
            WHERE ProjId = @ProjId AND Mailbox = @Mailbox
        END
        ELSE
        BEGIN
            INSERT INTO ProcesoCorreosControl (ProjId, Mailbox, UltimaFecha, RangoDias)
            VALUES (@ProjId, @Mailbox, @UltimaFecha, @RangoDias)
        END", _dbConMails))
            {
                cmd.Parameters.AddWithValue("@ProjId", control.ProjId);
                cmd.Parameters.AddWithValue("@Mailbox", control.Mailbox);
                cmd.Parameters.AddWithValue("@UltimaFecha", control.UltimaFechaProcesada);
                cmd.Parameters.AddWithValue("@RangoDias", control.RangoDias);

                cmd.ExecuteNonQuery();
            }

            if (cerrarConexion) _dbConMails.Close();
        }



        void MapBagsToDataTables()
        {
            // Correos Recibidos
            CorreosRecibidosTmp.Clear();
            foreach (var c in _bagCorreosRecibidos)
            {
                var row = CorreosRecibidosTmp.NewRow();
                row["ACXProjectId"] = c.ACXProjectId;
                row["MailId"] = c.MailId;
                row["MailNo"] = c.MailNo;
                row["FromOrganizationName"] = c.FromOrganizationName;
                row["FromName"] = c.FromName;
                row["ReasonForIssue"] = c.ReasonForIssue;
                row["ReferenceNumber"] = c.ReferenceNumber;
                row["SentDate"] = c.SentDate;
                row["Subject"] = c.Subject;
                row["CorrespondenceType"] = c.CorrespondenceType;
                CorreosRecibidosTmp.Rows.Add(row);
            }

            // Correos Enviados
            CorreosEnviadosTmp.Clear();
            foreach (var c in _bagCorreosEnviados)
            {
                var row = CorreosEnviadosTmp.NewRow();
                row["ACXProjectId"] = c.ACXProjectId;
                row["MailId"] = c.MailId;
                row["MailNo"] = c.MailNo;
                row["FromOrganizationName"] = c.FromOrganizationName;
                row["FromName"] = c.FromName;
                row["ReasonForIssue"] = c.ReasonForIssue;
                row["ReferenceNumber"] = c.ReferenceNumber;
                row["SentDate"] = c.SentDate;
                row["Subject"] = c.Subject;
                row["CorrespondenceType"] = c.CorrespondenceType;
                CorreosEnviadosTmp.Rows.Add(row);
            }

            // Destinatarios
            DestinatariosTmp.Clear();
            foreach (var d in _bagDestinatarios)
            {
                var row = DestinatariosTmp.NewRow();
                row["ACXProjectId"] = d.ACXProjectId;
                row["MailId"] = d.MailId;
                row["MailNo"] = d.MailNo;
                row["ACXUserId"] = d.ACXUserId;
                row["UserName"] = d.UserName;
                row["Organization"] = d.Organization;
                DestinatariosTmp.Rows.Add(row);
            }

            // Documentos Recibidos
            DocumentosRecibidosTmp.Clear();
            foreach (var doc in _bagDocumentosRecibidos)
            {
                var row = DocumentosRecibidosTmp.NewRow();
                row["ACXProjectId"] = doc.ACXProjectId;
                row["MailId"] = doc.MailId;
                row["DocumentNo"] = doc.DocumentNo;
                row["FileName"] = doc.FileName;
                row["FileSize"] = doc.FileSize;
                row["Revision"] = doc.Revision;
                row["RevisionDate"] = doc.RevisionDate;
                row["Title"] = doc.Title;
                DocumentosRecibidosTmp.Rows.Add(row);
            }

            // Documentos Enviados
            DocumentosEnviadosTmp.Clear();
            foreach (var doc in _bagDocumentosEnviados)
            {
                var row = DocumentosEnviadosTmp.NewRow();
                row["ACXProjectId"] = doc.ACXProjectId;
                row["MailId"] = doc.MailId;
                row["DocumentNo"] = doc.DocumentNo;
                row["FileName"] = doc.FileName;
                row["FileSize"] = doc.FileSize;
                row["Revision"] = doc.Revision;
                row["RevisionDate"] = doc.RevisionDate;
                row["Title"] = doc.Title;
                DocumentosEnviadosTmp.Rows.Add(row);
            }
        }


    }
}
