using SigmabotSync.Domain.Models.Extraction;
using SigmabotSync.Domain.Ports;
using SigmabotSync.Application.Common;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SigmabotSync.Application.Extraction
{
    public class WorkflowExtractionWorker
    {
        private const string LegacyWorkflowApplicationKey = "a7f7bf46-a848-4b7a-ae8c-ed55b3952010";

        public Dictionary<string, MemProject> MemProjects { get; private set; }
        public Dictionary<string, bool> MemUsers { get; private set; }

        public Dictionary<string, MemProyFlujo> MemWflows { get; private set; }
        public Dictionary<string, MemProyPaso> MemWflowsSteps { get; private set; }
        public List<string> MemUserByProject { get; private set; }
        public StreamWriter Mfile1 { get; private set; }
        public DataTable Archivostmp { get; private set; }
        public DataTable Asignadostmp { get; private set; }
        public DataTable FlujosdeTrabajotmp { get; private set; }
        public DataTable PasosFlujosdeTrabajotmp { get; private set; }
        public DataTable Usuariostmp { get; private set; }
        public DataTable UsuariosNew { get; private set; }
        public DataTable UsuarioPorProyectotmp { get; private set; }
        public Dictionary<string, string> MGroups { get; private set; }

        private Dictionary<string, string> _config;
        private readonly SqlConnection _dbConWorkflow;
        private readonly IAconexHttpGetPort _httpGet;

        public WorkflowExtractionWorker(Dictionary<string, string> config, string connectionString, IAconexHttpGetPort httpGet)
        {
            _config = config;
            _dbConWorkflow = new SqlConnection(connectionString);
            _httpGet = httpGet ?? throw new ArgumentNullException(nameof(httpGet));
        }

        public void FlujosdeTrabajo(string proyectID)
        {
            try
            {
                InitVariables1();
                dbchecktmpTables();
                Dbcleartmptables();
                LoadUsersToMemory1();
                NewACXUsersToMemory(proyectID);
                LoadProjectWorkflowsToMemory(proyectID);
                LoadProjectWorkflowsStepsToMemory(proyectID);
                DatosActuales(proyectID);
                GetACXWorkflows(proyectID);
                DbUpdateProjectData(proyectID);
            }
            catch (Exception ex)
            {
                // Log o manejo de error
                Utilities.Wlog($"Error en FlujosdeTrabajo: {ex.Message}", 0);
            }
        }


        public void InitVariables1()
        {
            MemProjects = new Dictionary<string, MemProject>();
            MemUsers = new Dictionary<string, bool>();
            getgroups();
        }

        public void getgroups()
        {
            string carpeta = Environment.CurrentDirectory;
            string archivo = Path.Combine(carpeta, "grupos.txt");

            var groups = new Dictionary<string, string> { { "x", "x" } };

            if (File.Exists(archivo))
            {
                using (var reader = new StreamReader(archivo))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var parts = line.Split('=');
                            if (parts.Length == 2)
                            {
                                var val1 = parts[0];
                                var val2 = parts[1];
                                if (!groups.ContainsKey(val1))
                                    groups.Add(val1, val2);
                            }
                        }
                    }
                }
            }
            MGroups = groups;
        }

        public void dbchecktmpTables()
        {
            // Limpiar y liberar referencias previas si existen
            if (Archivostmp != null)
            {
                Archivostmp.Clear();
                Archivostmp = null;
            }
            if (Asignadostmp != null)
            {
                Asignadostmp.Clear();
                Asignadostmp = null;
            }
            if (FlujosdeTrabajotmp != null)
            {
                FlujosdeTrabajotmp.Clear();
                FlujosdeTrabajotmp = null;
            }
            if (PasosFlujosdeTrabajotmp != null)
            {
                PasosFlujosdeTrabajotmp.Clear();
                PasosFlujosdeTrabajotmp = null;
            }
            if (UsuarioPorProyectotmp != null)
            {
                UsuarioPorProyectotmp.Clear();
                UsuarioPorProyectotmp = null;
            }

            // Crear DataTable archivostmp
            Archivostmp = new DataTable("Archivos_tmp");
            Archivostmp.Columns.Add(new DataColumn("ACXProjectId", typeof(string)));
            Archivostmp.Columns.Add(new DataColumn("IdFlujodeTrabajo", typeof(string)));
            Archivostmp.Columns.Add(new DataColumn("NumeroFlujodeTrabajo", typeof(string)));
            Archivostmp.Columns.Add(new DataColumn("Numero", typeof(string)));
            Archivostmp.Columns.Add(new DataColumn("Titulo", typeof(string)));
            Archivostmp.Columns.Add(new DataColumn("Revision", typeof(string)));
            Archivostmp.Columns.Add(new DataColumn("ACXTrackingId", typeof(string)));
            Archivostmp.Columns.Add(new DataColumn("Version", typeof(string)));
            Archivostmp.Columns.Add(new DataColumn("Archivo", typeof(string)));
            Archivostmp.Columns.Add(new DataColumn("Tamaño", typeof(long)));

            // Crear DataTable Asignadostmp
            Asignadostmp = new DataTable("Asignados_tmp");
            Asignadostmp.Columns.Add(new DataColumn("ACXProjectId", typeof(string)));
            Asignadostmp.Columns.Add(new DataColumn("IdFlujodeTrabajo", typeof(string)));
            Asignadostmp.Columns.Add(new DataColumn("NumeroFlujodeTrabajo", typeof(string)));
            Asignadostmp.Columns.Add(new DataColumn("Organizacion", typeof(string)));
            Asignadostmp.Columns.Add(new DataColumn("Nombre", typeof(string)));
            Asignadostmp.Columns.Add(new DataColumn("ACXIdAsignado", typeof(string)));

            // Crear DataTable FlujosdeTrabajotmp
            FlujosdeTrabajotmp = new DataTable("FlujosdeTrabajo_tmp");
            FlujosdeTrabajotmp.Columns.Add(new DataColumn("ACXProjectId", typeof(string)));
            FlujosdeTrabajotmp.Columns.Add(new DataColumn("Numero", typeof(string)));
            FlujosdeTrabajotmp.Columns.Add(new DataColumn("Nombre", typeof(string)));
            FlujosdeTrabajotmp.Columns.Add(new DataColumn("Estado", typeof(string)));
            FlujosdeTrabajotmp.Columns.Add(new DataColumn("Plantilla", typeof(string)));
            FlujosdeTrabajotmp.Columns.Add(new DataColumn("NombreEmisor", typeof(string)));
            FlujosdeTrabajotmp.Columns.Add(new DataColumn("ACXIdEmisor", typeof(string)));
            FlujosdeTrabajotmp.Columns.Add(new DataColumn("OrganizacionEmisora", typeof(string)));
            FlujosdeTrabajotmp.Columns.Add(new DataColumn("MotivodeEmision", typeof(string)));

            // Crear DataTable PasosFlujosdeTrabajotmp
            PasosFlujosdeTrabajotmp = new DataTable("PasosFlujosdeTrabajo_tmp");
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("ACXProjectId", typeof(string)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("IdFlujodeTrabajo", typeof(string)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("NumeroFlujodeTrabajo", typeof(string)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("Nombre", typeof(string)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("Estado", typeof(string)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("Resultado", typeof(string)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("Comentarios", typeof(string)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("FechaFinalizacion", typeof(DateTime)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("FechaLimite", typeof(DateTime)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("FechaInicio", typeof(DateTime)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("Duracion", typeof(int)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("FechaLimiteOriginal", typeof(DateTime)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("DiasAtraso", typeof(int)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("RevisorOrganizacion", typeof(string)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("RevisorNombre", typeof(string)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("RevisorACXId", typeof(string)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("AtrasoFC", typeof(long)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("AtrasoFCHoy", typeof(long)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("EstadoFC", typeof(string)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("TrackingId", typeof(string)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("DocVersion", typeof(string)));
            PasosFlujosdeTrabajotmp.Columns.Add(new DataColumn("Grupo", typeof(string)));

            // Crear DataTable UsuarioPorProyectotmp
            UsuarioPorProyectotmp = new DataTable("UsuarioPorProyecto_tmp");
            UsuarioPorProyectotmp.Columns.Add(new DataColumn("ACXProjectId", typeof(string)));
            UsuarioPorProyectotmp.Columns.Add(new DataColumn("ACXUserId", typeof(string)));

            dbUpdate("modulo1");  // Asumo que este m�todo tambi�n est� migrado y disponible
        }

        public void dbUpdate(string modulo)
        {
            string carpeta = Environment.CurrentDirectory;
            string archivo = Path.Combine(carpeta, modulo + ".dbu");

            if (File.Exists(archivo))
            {
                string sql = string.Empty;
                using (var reader = new StreamReader(archivo))
                {
                    sql = reader.ReadToEnd();
                }

                if (!string.IsNullOrEmpty(sql))
                {
                    if (_dbConWorkflow.State == ConnectionState.Closed)
                        _dbConWorkflow.Open();

                    try
                    {
                        using (var sc = new SqlCommand(sql, _dbConWorkflow))
                        {
                            sc.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        Utilities.Wlog($"Documentos: ERROR Actualizando Base de datos: {sql}\n{ex.Message}", 0);
                    }

                    try
                    {
                        File.Delete(archivo);
                    }
                    catch (Exception ex)
                    {
                        Utilities.Wlog($"Flujos: no se pudo eliminar archivo '{archivo}': {ex.Message}", 1);
                    }
                }
            }
        }

        public void Dbcleartmptables()
        {
            if (Archivostmp != null)
            {
                Archivostmp.Clear();
                // archivostmp = null; // opcional, si quieres liberar referencia
            }
            if (Asignadostmp != null)
            {
                Asignadostmp.Clear();
                // Asignadostmp = null;
            }
            if (FlujosdeTrabajotmp != null)
            {
                FlujosdeTrabajotmp.Clear();
                // FlujosdeTrabajotmp = null;
            }
            if (PasosFlujosdeTrabajotmp != null)
            {
                PasosFlujosdeTrabajotmp.Clear();
                // PasosFlujosdeTrabajotmp = null;
            }
            if (UsuarioPorProyectotmp != null)
            {
                UsuarioPorProyectotmp.Clear();
                // UsuarioPorProyectotmp = null;
            }
        }

        public void LoadUsersToMemory1()
        {
            // Wlog("PROCESO {LoadUsersToMemory}: Carga usuarios en memoria", 1);

            Usuariostmp = new DataTable();
            UsuariosNew = new DataTable();

            try
            {
                MemUsers.Clear();

                using (var da = new SqlDataAdapter("SELECT [Nombre], [Cargo], ISNULL(ACXUserId, '') AS ACXUserId, [Organizacion] FROM [Usuarios]", _dbConWorkflow))
                {
                    da.Fill(Usuariostmp);
                }

                UsuariosNew = Usuariostmp.Clone();
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Flujos: ERROR {{LoadUsersToMemory}}: {ex.Message}", 0);
            }
        }

        public void NewACXUsersToMemory(string projid)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string authcode = Utilities.EncodeTexto($"{_config["ACXUser"]}:{_config["ACXPass"]}");

            List<string> memUserByProject = new List<string>();

            try
            {
                string responseFromServer = _httpGet.GetStringAsync(new AconexHttpGetRequest
                {
                    Url = $"https://us1.aconex.com/api/projects/{projid}/directory?",
                    AuthorizationHeaderBase64 = authcode,
                    ExtraHeaders = new[] { ("X-Application-Key", LegacyWorkflowApplicationKey) }
                }).GetAwaiter().GetResult();

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(responseFromServer);

                foreach (XmlElement user in doc.GetElementsByTagName("Directory"))
                {
                    if (user["SearchResultType"]?.InnerText == "USER_TYPE")
                    {
                        string userid = user["UserId"]?.InnerText ?? "";
                        memUserByProject.Add(userid);

                        if (!dbUserExists(userid))
                        {
                            dbUserToDbTmp(user); // M�todo existente que debe aceptar XmlElement
                        }
                    }
                }

                dbAddProjectUsersTmp(memUserByProject, projid);
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Flujos: ERROR {{NewACXUsersToMemory}}: Proyecto: {_config["NombrePrj"]} ({projid}) Mensaje: {ex.Message}", 0);
            }
        }

        public void dbUserToDbTmp(XmlElement user)
        {
            try
            {
                string title = "";
                if (user["JobTitle"] != null)
                {
                    title = user["JobTitle"].InnerText;
                }

                DataRow row = UsuariosNew.NewRow();
                row["Nombre"] = user["UserName"]?.InnerText ?? "";
                row["Cargo"] = title;
                row["ACXUserId"] = user["UserId"]?.InnerText ?? "";
                row["Organizacion"] = user["OrganizationName"]?.InnerText ?? "";

                UsuariosNew.Rows.Add(row);
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Flujos: ERROR {{dbUserToDbTmp}}: {user["UserName"]?.InnerText} ({user["UserId"]?.InnerText}) : {ex.Message}", 0);
            }
        }

        public bool dbUserExists(string userid)
        {
            try
            {
                DataRow[] dr = Usuariostmp.Select($"ACXUserId = '{userid}'");
                if (dr.Length == 0)
                {
                    dr = UsuariosNew.Select($"ACXUserId = '{userid}'");
                    return dr.Length > 0;
                }
                return true;
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Flujos: ERROR {{dbUserExists}} userid={userid}: {ex.Message}", 0);
                return false;
            }
        }

        public void dbAddProjectUsersTmp(List<string> memUserByProject, string projectId)
        {
            // Wlog("PROCESO {dbAddProjectUsersTmp}: Agrega usuarios de proyecto:" + projectId, 1);

            try
            {
                foreach (string user in memUserByProject)
                {
                    DataRow row = UsuarioPorProyectotmp.NewRow();
                    row["ACXProjectId"] = projectId;
                    row["ACXUserId"] = user;
                    UsuarioPorProyectotmp.Rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Flujos: ERROR {{dbAddProjectUsersTmp}}: {projectId}: {ex.Message}", 0);
            }
        }

        public void LoadProjectWorkflowsToMemory(string projid)
        {
            // Wlog("PROCESO {loadProjectWorkflowstoMemory}: Carga flujos de trabajo actuales a la memoria", 2);
            MemWflows = new Dictionary<string, MemProyFlujo>();
            MemWflows.Clear();

            return; // Esta l�nea detiene la ejecuci�n del m�todo, igual que Exit Sub en VB.NET

            DataTable actualDB = new DataTable();

            try
            {
                using (var da = new SqlDataAdapter($"SELECT * FROM [FlujosdeTrabajo] WHERE [ACXProjectId] = '{projid}'", _dbConWorkflow))
                {
                    da.Fill(actualDB);
                }

                foreach (DataRow row in actualDB.Rows)
                {
                    var flujo = new MemProyFlujo
                    {
                        Numero = row["Numero"].ToString(),
                        Nombre = row["Nombre"].ToString(),
                        Estado = row["Estado"].ToString(),
                        Plantilla = row["Plantilla"].ToString(),
                        NombreEmisor = row["NombreEmisor"].ToString(),
                        ACXIdEmisor = row["ACXIdEmisor"].ToString(),
                        OrganizacionEmisora = row["OrganizacionEmisora"].ToString(),
                        MotivodeEmision = row["MotivodeEmision"].ToString()
                    };

                    MemWflows.Add(row["Numero"].ToString(), flujo);
                }
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Flujos: ERROR {{loadProjectWorkflowstoMemory}}: {ex.Message}", 0);
            }
        }



        public void LoadProjectWorkflowsStepsToMemory(string projid)
        {
            // Wlog($"PROCESO {{loadProjectWorkflowsStepstoMemory}}: Carga pasos de flujos de trabajo actuales a la memoria del proyecto: {_config["NombrePrj"]} ({projid})", 2);
            MemWflowsSteps = new Dictionary<string, MemProyPaso>();
            MemWflowsSteps.Clear();

            return; // L�nea equivalente a Exit Sub

            try
            {
                var actualDB = new DataTable();

                using (var da = new SqlDataAdapter(
                    $"SELECT * FROM [PasosFlujosdeTrabajo] WHERE [ACXProjectId] = '{projid}'", _dbConWorkflow))
                {
                    da.Fill(actualDB);
                }

                foreach (DataRow row in actualDB.Rows)
                {
                    string idFlujo = row["IdFlujodeTrabajo"].ToString();

                    if (!MemWflowsSteps.ContainsKey(idFlujo))
                    {
                        var paso = new MemProyPaso
                        {
                            IdFlujodeTrabajo = idFlujo,
                            NumeroFlujodeTrabajo = row["NumeroFlujodeTrabajo"].ToString(),
                            Nombre = row["Nombre"].ToString(),
                            Estado = row["Estado"].ToString(),
                            Resultado = row["Resultado"].ToString(),
                            Comentarios = row["Comentarios"].ToString(),
                            Duracion = Convert.ToInt32(row["Duracion"]),
                            DiasAtraso = Convert.ToInt32(row["DiasAtraso"]),
                            RevisorOrganizacion = row["RevisorOrganizacion"].ToString(),
                            RevisorNombre = row["RevisorNombre"].ToString(),
                            RevisorACXId = row["RevisorACXId"].ToString()
                        };

                        MemWflowsSteps.Add(idFlujo, paso);
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Flujos: ERROR {{loadProjectWorkflowsStepstoMemory}}: {_config["NombrePrj"]} ({projid}): {ex.Message}", 0);
            }
        }

        public void DatosActuales(string projid)
        {
            try
            {
                DataTable actualDB = new DataTable();

                if (_dbConWorkflow.State == ConnectionState.Closed)
                {
                    _dbConWorkflow.Open();
                }

                using (var da = new SqlDataAdapter(
                    $"SELECT COUNT(*) AS total FROM [PasosFlujosdeTrabajo] WHERE [ACXProjectId] = '{projid}'",
                    _dbConWorkflow))
                {
                    da.Fill(actualDB);
                }

                string totalPasos = actualDB.Rows[0]["total"].ToString();
                actualDB.Clear();

                using (var da = new SqlDataAdapter(
                    $"SELECT COUNT(*) AS total FROM [FlujosdeTrabajo] WHERE [ACXProjectId] = '{projid}'",
                    _dbConWorkflow))
                {
                    da.Fill(actualDB);
                }

                string totalFlujos = actualDB.Rows[0]["total"].ToString();

                Utilities.Wlog($"Flujos: Total de Flujos antes del proceso: {totalFlujos}", 1);
                Utilities.Wlog($"Flujos: Total de Pasos antes del proceso: {totalPasos}", 1);
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Flujos: ERROR {{datos actuales}}: {ex.Message}", 0);
            }
        }

        public void GetACXWorkflows(string projid)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            var stopwatch = new System.Diagnostics.Stopwatch();

            try
            {
                Utilities.Wlog($"[Workflow] {DateTime.Now} Inicio GetACXWorkflows para {projid}", 1);
                stopwatch.Restart();
                if (_dbConWorkflow.State == ConnectionState.Closed)
                    _dbConWorkflow.Open();

                long pagina = 1;
                long maxpages = 1;
                string authcode = Utilities.EncodeTexto(_config["ACXUser"] + ":" + _config["ACXPass"]);

                maxpages = GetMaxPages(projid, authcode);

                if (maxpages > 0)
                {
                    var paginas = new bool[maxpages + 1]; // indexa desde 1

                    Utilities.Wlog("Flujos: Total de Paginas " + maxpages, 2);

                    for (pagina = 1; pagina <= maxpages; pagina++)
                    {
                        paginas[pagina] = GetWorkflowByPage((int)pagina, projid, authcode);

                    }

                    long tmalas = 0;
                    for (int i = 1; i <= maxpages; i++)
                    {
                        if (!paginas[i]) tmalas++;
                    }

                    if (tmalas > 0)
                    {
                        Utilities.Wlog("Flujos: Total de paginas para re-procesar: " + tmalas, 1);

                        for (pagina = maxpages; pagina >= 1; pagina--)
                        {
                            if (!paginas[pagina])
                            {
                                paginas[pagina] = GetWorkflowByPage((int)pagina, projid, authcode);
                            }
                        }

                        tmalas = 0;
                        for (int i = 1; i <= maxpages; i++)
                        {
                            if (!paginas[i]) tmalas++;
                        }

                        if (tmalas > 0)
                        {
                            Utilities.Wlog("Flujos: Total paginas con errores: " + tmalas, 1);
                        }
                        else
                        {
                            Utilities.Wlog("Flujos: Todas las paginas re-procesadas sin problemas", 1);
                        }
                    }
                    else
                    {
                        Utilities.Wlog("Flujos: Todas las paginas procesadas sin problemas", 1);
                    }

                    AppState.totFlujosAconex = MemWflows.Count;
                }

                stopwatch.Stop();
                Utilities.Wlog($"[Workflow] {DateTime.Now} Finaliz� GetACXWorkflows para {projid} en {stopwatch.Elapsed.Minutes:D2}:{stopwatch.Elapsed.Seconds:D2} (mm:ss)", 1);
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Flujos: ERROR {{GetACXWorkflows}}: {_config["NombrePrj"]} ({projid}) Mensaje: {ex.Message}", 0);
            }
            finally
            {
                if (_dbConWorkflow.State == ConnectionState.Open)
                    _dbConWorkflow.Close(); // ? Cerrar conexi�n cuando termina todo
            }
        }


        public long GetMaxPages(string projid, string authcode)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            string uri = $"https://us1.aconex.com/api/projects/{projid}/workflows?page_size=300&page_number=1";

            try
            {
                string responseFromServer = _httpGet.GetStringAsync(new AconexHttpGetRequest
                {
                    Url = uri,
                    AuthorizationHeaderBase64 = authcode,
                    Accept = "application/vnd.aconex.workflow.v1+xml",
                    ExtraHeaders = new[] { ("X-Application-Key", LegacyWorkflowApplicationKey) }
                }).GetAwaiter().GetResult();

                var doc = new XmlDocument();
                doc.LoadXml(responseFromServer);

                long tresult = Convert.ToInt64(doc.SelectSingleNode("WorkflowSearch").Attributes["TotalResults"].InnerText);
                Utilities.Wlog("Flujos: Pasos de Flujo de Trabajo a procesar:" + tresult, 1);

                AppState.totPasosFlujosAconex = tresult;

                if (tresult > 0)
                    return Convert.ToInt64(doc.SelectSingleNode("WorkflowSearch").Attributes["TotalPages"].InnerText);
                return 0;
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Flujos: ERROR {{GetMaxPages}}: {_config["NombrePrj"]} ({projid}) Mensaje: {ex.Message}", 0);
                return 0;
            }
        }


        public bool GetWorkflowByPage(long pagina, string projid, string authcode)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            bool resultado = true;

            string uri = $"https://us1.aconex.com/api/projects/{projid}/workflows?page_size=300&page_number={pagina}";
            Console.WriteLine("Pagina: " + pagina);

            try
            {
                string responseFromServer = _httpGet.GetStringAsync(new AconexHttpGetRequest
                {
                    Url = uri,
                    AuthorizationHeaderBase64 = authcode,
                    Accept = "application/vnd.aconex.workflow.v1+xml",
                    ExtraHeaders = new[] { ("X-Application-Key", LegacyWorkflowApplicationKey) }
                }).GetAwaiter().GetResult();

                var doc = new XmlDocument();
                doc.LoadXml(responseFromServer);

                XmlNodeList workflows = doc.SelectSingleNode("WorkflowSearch")
                                           .SelectSingleNode("SearchResults")
                                           .SelectNodes("Workflow");

                foreach (XmlElement wfs in workflows)
                {
                    string wfNumber = wfs.SelectSingleNode("WorkflowNumber").InnerText;
                    string wfId = wfs.GetAttribute("WorkflowId");

                    AgregaPasoFlujo(wfs, projid);
                }
            }
            catch (Exception ex)
            {
                Utilities.Wlog("Flujos: ERROR {getWorkflowbyPage}: " + ex.Message, 0);
                resultado = false;
            }

            return resultado;
        }

        public void AgregaPasoFlujo(XmlElement wfs, string projectId)
        {
            try
            {
                // ---------------------
                // 1. Diccionario de nodos
                // ---------------------
                var nodes = wfs.ChildNodes.Cast<XmlNode>()
                    .Where(n => n.NodeType == XmlNodeType.Element)
                    .ToDictionary(n => n.Name, n => n.InnerText);

                string Get(string key) => nodes.TryGetValue(key, out var val) ? val : "";

                // ---------------------
                // 2. Extracci�n de datos
                // ---------------------
                string wfId = wfs.GetAttribute("WorkflowId");
                string wfNumber = Get("WorkflowNumber");
                string wfName = Get("WorkflowName");
                string wfStatus = Get("WorkflowStatus");
                string wfTemplate = Get("WorkflowTemplate");
                string reasonForIssue = Get("ReasonForIssue");
                string comments = Get("Comments");

                string dateCompleted = Get("DateCompleted");
                string dateDue = Get("DateDue");
                string originalDueDate = Get("OriginalDueDate");
                string dateIn = Get("DateIn");
                string duration = Get("Duration");
                string daysLate = Get("DaysLate");

                string stepName = Get("StepName");
                string stepOutcome = Get("StepOutcome");
                string stepStatus = Get("StepStatus");

                string docNumber = Get("DocumentNumber");
                string docTitle = Get("DocumentTitle");
                string docRevision = Get("DocumentRevision");
                string docTrackingId = Get("DocumentTrackingId");
                string docVersion = Get("DocumentVersion");
                string fileName = GetSingleSelect(wfs, "FileName");
                string fileSize = Get("FileSize");

                // ---------------------
                // 3. Reviewer e Initiator
                // ---------------------
                string revisorOrgName = "", revisorName = "", revisorUserId = "";
                string initiatorOrgName = "", initiatorName = "", initiatorUserId = "";

                XmlNode reviewer = wfs.SelectSingleNode("Reviewer");
                if (reviewer != null)
                {
                    revisorOrgName = reviewer.SelectSingleNode("OrganizationName")?.InnerText ?? "";
                    revisorName = reviewer.SelectSingleNode("Name")?.InnerText ?? "";
                    revisorUserId = reviewer.SelectSingleNode("UserId")?.InnerText ?? "";
                }

                XmlNode initiator = wfs.SelectSingleNode("Initiator");
                if (initiator != null)
                {
                    initiatorOrgName = initiator.SelectSingleNode("OrganizationName")?.InnerText ?? "";
                    initiatorName = initiator.SelectSingleNode("Name")?.InnerText ?? "";
                    initiatorUserId = initiator.SelectSingleNode("UserId")?.InnerText ?? "";
                }

                // ---------------------
                // 4. Flujo de trabajo (�nico por n�mero)
                // ---------------------
                if (!MemWflows.ContainsKey(wfNumber))
                {
                    MemWflows.Add(wfNumber, new MemProyFlujo
                    {
                        Numero = wfNumber,
                        Nombre = wfName,
                        Estado = wfStatus,
                        Plantilla = wfTemplate,
                        NombreEmisor = initiatorName,
                        ACXIdEmisor = initiatorUserId,
                        OrganizacionEmisora = initiatorOrgName,
                        MotivodeEmision = reasonForIssue
                    });

                    var flujo = FlujosdeTrabajotmp.NewRow();
                    flujo["ACXProjectId"] = projectId;
                    flujo["Numero"] = wfNumber;
                    flujo["Nombre"] = wfName;
                    flujo["Estado"] = wfStatus;
                    flujo["Plantilla"] = wfTemplate;
                    flujo["NombreEmisor"] = initiatorName;
                    flujo["ACXIdEmisor"] = initiatorUserId;
                    flujo["OrganizacionEmisora"] = initiatorOrgName;
                    flujo["MotivodeEmision"] = reasonForIssue;
                    FlujosdeTrabajotmp.Rows.Add(flujo);
                }

                // ---------------------
                // 5. Asignados
                // ---------------------
                foreach (XmlElement assg in wfs.SelectSingleNode("Assignees").SelectNodes("Assignee"))
                {
                    var asignado = Asignadostmp.NewRow();
                    asignado["ACXProjectId"] = projectId;
                    asignado["IdFlujodeTrabajo"] = wfId;
                    asignado["NumeroFlujodeTrabajo"] = wfNumber;
                    asignado["Organizacion"] = assg.SelectSingleNode("OrganizationName")?.InnerText ?? "";
                    asignado["Nombre"] = assg.SelectSingleNode("Name")?.InnerText ?? "";
                    asignado["ACXIdAsignado"] = assg.SelectSingleNode("UserId")?.InnerText ?? "";
                    Asignadostmp.Rows.Add(asignado);
                }

                string grupo = MGroups.ContainsKey(wfTemplate) ? MGroups[wfTemplate] : "No asignado";

                // ---------------------
                // 6. Paso del flujo
                // ---------------------
                long atrasoFC = 0, atrasoFCHoy = 0;
                DateTime? odd = mCDate(originalDueDate);
                DateTime? dd = mCDate(dateDue);

                if (dd.HasValue && odd.HasValue)
                {
                    atrasoFC = (long)(dd.Value - odd.Value).TotalDays;
                }

                if (odd.HasValue)
                {
                    if (string.IsNullOrEmpty(dateCompleted))
                    {
                        atrasoFCHoy = (long)(DateTime.Now - odd.Value).TotalDays;
                    }
                    else
                    {
                        var dc = mCDate(dateCompleted);
                        if (dc.HasValue && odd.Value < dc.Value)
                            atrasoFCHoy = (long)(dc.Value - odd.Value).TotalDays;
                    }
                }

                var paso = PasosFlujosdeTrabajotmp.NewRow();
                paso["ACXProjectId"] = projectId;
                paso["IdFlujodeTrabajo"] = wfId;
                paso["NumeroFlujodeTrabajo"] = wfNumber;
                paso["Nombre"] = stepName;
                paso["Estado"] = stepStatus;
                paso["Resultado"] = stepOutcome;
                paso["Comentarios"] = comments;
                paso["FechaFinalizacion"] = string.IsNullOrEmpty(dateCompleted) ? (object)DBNull.Value : mCDate(dateCompleted);
                paso["FechaLimite"] = dd ?? (object)DBNull.Value;
                paso["FechaInicio"] = string.IsNullOrEmpty(dateIn) ? (object)DBNull.Value : mCDate(dateIn);
                paso["Duracion"] = string.IsNullOrEmpty(duration) ? 0 : (int)double.Parse(duration, CultureInfo.InvariantCulture);
                paso["FechaLimiteOriginal"] = string.IsNullOrEmpty(originalDueDate) ? (object)DBNull.Value : mCDate(originalDueDate);
                paso["AtrasoFC"] = atrasoFC;
                paso["AtrasoFCHoy"] = atrasoFCHoy;
                paso["EstadoFC"] = atrasoFCHoy >= 0 ? "Vigente" : "Vencido";
                paso["RevisorOrganizacion"] = revisorOrgName;
                paso["DiasAtraso"] = revisorOrgName == "Conexi�n Kimal Lo Aguirre S.A." ? (object)atrasoFCHoy : (object)daysLate;
                paso["RevisorNombre"] = revisorName;
                paso["RevisorACXId"] = revisorUserId;
                paso["TrackingId"] = docTrackingId;
                paso["DocVersion"] = docVersion;
                paso["Grupo"] = grupo;
                PasosFlujosdeTrabajotmp.Rows.Add(paso);

                // ---------------------
                // 7. Archivo
                // ---------------------
                var archivo = Archivostmp.NewRow();
                archivo["ACXProjectId"] = projectId;
                archivo["IdFlujodeTrabajo"] = wfId;
                archivo["NumeroFlujodeTrabajo"] = wfNumber;
                archivo["Numero"] = docNumber;
                archivo["Titulo"] = docTitle;
                archivo["Revision"] = docRevision;
                archivo["ACXTrackingId"] = docTrackingId;
                archivo["Version"] = docVersion;
                archivo["Archivo"] = fileName;
                archivo["Tamaño"] = fileSize;
                Archivostmp.Rows.Add(archivo);
            }
            catch (Exception ex)
            {
                Utilities.Wlog("Flujos: ERROR {AgregaPasoFlujo}:" + projectId + ":" + wfs.GetAttribute("WorkflowId") + ":" + ex.Message, 0);
            }
        }



        DateTime? mCDate(string sdate)
        {
            if (string.IsNullOrWhiteSpace(sdate))
                return null;

            try
            {
                int mYear = int.Parse(sdate.Substring(0, 4));
                int mMonth = int.Parse(sdate.Substring(5, 2));
                int mDay = int.Parse(sdate.Substring(8, 2));
                int mHour = int.Parse(sdate.Substring(11, 2));
                int mMin = int.Parse(sdate.Substring(14, 2));
                int mSec = int.Parse(sdate.Substring(17, 2));

                // Construye fecha UTC y la convierte a hora local
                var utcDate = new DateTime(mYear, mMonth, mDay, mHour, mMin, mSec, DateTimeKind.Utc);
                var localDate = utcDate.ToLocalTime();

                // Trunca la hora (como DateSerial en VB)
                return new DateTime(localDate.Year, localDate.Month, localDate.Day);
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Flujos: ERROR al parsear fecha (DateSerial): sdate={sdate}, {ex.Message}", 0);
                return null;
            }
        }

        public string GetSingleSelect(XmlElement wfs, string nodo)
        {
            try
            {
                var node = wfs.SelectSingleNode(nodo);
                if (node != null)
                {
                    return node.InnerText;
                }
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Flujos: ERROR GetSingleSelect nodo={nodo}: {ex.Message}", 0);
            }
            return string.Empty;
        }

        public void DbUpdateProjectData(string projid)
        {
            if (_dbConWorkflow.State == ConnectionState.Closed)
                _dbConWorkflow.Open();

            try
            {
                if (UsuariosNew.Rows.Count > 0)
                {
                    using (var bulk = new SqlBulkCopy(_dbConWorkflow))
                    {
                        bulk.DestinationTableName = "Usuarios_tmp";
                        bulk.WriteToServer(UsuariosNew);
                    }

                    using (var cmd = new SqlCommand(
                        "INSERT INTO [Usuarios] ([Nombre],[Cargo],[ACXUserId],[Organizacion]) " +
                        "SELECT [Nombre],[Cargo],[ACXUserId],[Organizacion] FROM Usuarios_tmp", _dbConWorkflow))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Flujos: ERROR {{dbUpdateProjectData}}: proyecto: {_config["NombrePrj"]}:{ex.Message}", 0);
            }

            SqlTransaction transaction;

            try
            {
                if (PasosFlujosdeTrabajotmp.Rows.Count > 0)
                {
                    AppState.totPasosFlujosDescar = PasosFlujosdeTrabajotmp.Rows.Count;
                    Utilities.Wlog($"Flujos: Total de Pasos de flujo rescatados {AppState.totPasosFlujosDescar}", 1);

                    using (var bulk = new SqlBulkCopy(_dbConWorkflow))
                    {
                        bulk.DestinationTableName = "PasosFlujosdeTrabajo_tmp";
                        bulk.WriteToServer(PasosFlujosdeTrabajotmp);
                    }

                    using (var cmd = new SqlCommand("TRUNCATE TABLE PasosFlujosdeTrabajo", _dbConWorkflow))
                    {
                        transaction = _dbConWorkflow.BeginTransaction("BorraPasosFlujosdeTrabajo");
                        cmd.Transaction = transaction;
                        cmd.ExecuteNonQuery();
                        transaction.Commit();
                    }

                    using (var cmd = new SqlCommand(
                        "INSERT INTO [PasosFlujosdeTrabajo] " +
                        "([ACXProjectId],[IdFlujodeTrabajo],[NumeroFlujodeTrabajo],[Nombre],[Estado],[Resultado],[Comentarios],[FechaFinalizacion],[FechaLimite],[FechaInicio]," +
                        "[Duracion],[FechaLimiteOriginal],[DiasAtraso],[RevisorOrganizacion],[RevisorNombre],[RevisorACXId],AtrasoFC,AtrasoFCHoy,EstadoFC,TrackingId,DocVersion,Grupo) " +
                        "SELECT [ACXProjectId],[IdFlujodeTrabajo],[NumeroFlujodeTrabajo],[Nombre],[Estado],[Resultado],[Comentarios],[FechaFinalizacion],[FechaLimite]," +
                        "[FechaInicio],[Duracion],[FechaLimiteOriginal],[DiasAtraso],[RevisorOrganizacion],[RevisorNombre],[RevisorACXId],AtrasoFC,AtrasoFCHoy,EstadoFC,TrackingId,DocVersion,Grupo " +
                        "FROM PasosFlujosdeTrabajo_tmp", _dbConWorkflow))
                    {
                        transaction = _dbConWorkflow.BeginTransaction("CopiaPasosFlujosdeTrabajo");
                        cmd.Transaction = transaction;
                        cmd.ExecuteNonQuery();
                        transaction.Commit();
                    }
                }

                void BulkInsertAndReplace(DataTable table, string tempTable, string mainTable, string insertQuery, string context)
                {
                    if (table.Rows.Count == 0) return;

                    using (var bulk = new SqlBulkCopy(_dbConWorkflow))
                    {
                        bulk.DestinationTableName = tempTable;
                        bulk.WriteToServer(table);
                    }

                    using (var cmdTruncate = new SqlCommand($"TRUNCATE TABLE {mainTable}", _dbConWorkflow))
                    {
                        transaction = _dbConWorkflow.BeginTransaction("Borra" + context);
                        cmdTruncate.Transaction = transaction;
                        cmdTruncate.ExecuteNonQuery();
                        transaction.Commit();
                    }

                    using (var cmdInsert = new SqlCommand(insertQuery, _dbConWorkflow))
                    {
                        transaction = _dbConWorkflow.BeginTransaction("Copia" + context);
                        cmdInsert.Transaction = transaction;
                        cmdInsert.ExecuteNonQuery();
                        transaction.Commit();
                    }
                }

                BulkInsertAndReplace(UsuarioPorProyectotmp, "UsuarioPorProyecto_tmp", "UsuarioPorProyecto",
                    "INSERT INTO UsuarioPorProyecto ([ACXIdProyecto],[ACXUserId]) SELECT [ACXIdProyecto],[ACXUserId] FROM UsuarioPorProyecto_tmp", "UsuariosPorProyecto");

                if (FlujosdeTrabajotmp.Rows.Count > 0)
                {
                    AppState.totFlujosDescar = FlujosdeTrabajotmp.Rows.Count;
                    Utilities.Wlog($"Flujos: Total de Flujos rescatados {AppState.totFlujosDescar}", 1);
                }

                BulkInsertAndReplace(FlujosdeTrabajotmp, "FlujosdeTrabajo_tmp", "FlujosdeTrabajo",
                    "INSERT INTO FlujosdeTrabajo ([ACXProjectId],[Numero],[Nombre],[Estado],[Plantilla],[NombreEmisor],[ACXIdEmisor],[OrganizacionEmisora],[MotivodeEmision]) " +
                    "SELECT [ACXProjectId],[Numero],[Nombre],[Estado],[Plantilla],[NombreEmisor],[ACXIdEmisor],[OrganizacionEmisora],[MotivodeEmision] FROM FlujosdeTrabajo_tmp", "FlujosdeTrabajo");

                BulkInsertAndReplace(Archivostmp, "Archivos_tmp", "Archivos",
                    "INSERT INTO [Archivos] ([ACXProjectId],[IdFlujodeTrabajo],[NumeroFlujodeTrabajo],[Numero],[Titulo],[Revision],[ACXTrackingId],[Version],[Archivo],[Tamaño]) " +
                    "SELECT [ACXProjectId],[IdFlujodeTrabajo],[NumeroFlujodeTrabajo],[Numero],[Titulo],[Revision],[ACXTrackingId],[Version],[Archivo],[Tamaño] FROM Archivos_tmp", "Archivos");

                BulkInsertAndReplace(Asignadostmp, "Asignados_tmp", "Asignados",
                    "INSERT INTO [Asignados] ([ACXProjectId],[IdFlujodeTrabajo],[NumeroFlujodeTrabajo],[Organizacion],[Nombre],[ACXIdAsignado]) " +
                    "SELECT [ACXProjectId],[IdFlujodeTrabajo],[NumeroFlujodeTrabajo],[Organizacion],[Nombre],[ACXIdAsignado] FROM Asignados_tmp", "Asignados");

                // Limpieza de tablas temporales
                string[] tempTables = {
            "Usuarios_tmp",
            "PasosFlujosdeTrabajo_tmp",
            "UsuarioPorProyecto_tmp",
            "FlujosdeTrabajo_tmp",
            "Archivos_tmp",
            "Asignados_tmp"
        };

                foreach (string tempTable in tempTables)
                {
                    using (var cmd = new SqlCommand($"TRUNCATE TABLE {tempTable}", _dbConWorkflow))
                    {
                        transaction = _dbConWorkflow.BeginTransaction("borra" + tempTable);
                        cmd.Transaction = transaction;
                        cmd.ExecuteNonQuery();
                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Flujos: ERROR {{dbUpdateProjectData}}: proyecto: {_config["NombrePrj"]}:{ex.Message}", 0);
            }

            _dbConWorkflow.Close();
        }
    }
}
