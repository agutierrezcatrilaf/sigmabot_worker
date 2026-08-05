using SigmabotSync.Domain.Models.Extraction;
using SigmabotSync.Domain.Ports;
using SigmabotSync.Application.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SigmabotSync.Application.Extraction
{
    public class IncidentExtractionWorker
    {
        private readonly Dictionary<string, string> _config;
        private readonly SqlConnection _dbConField;
        private readonly IAconexHttpGetPort _httpGet;

        public static DataTable Areastmp;
        public static DataTable Incidentestmp;
        private Dictionary<string, string> mCustomFields;


        public IncidentExtractionWorker(Dictionary<string, string> config, string connectionString, IAconexHttpGetPort httpGet)
        {
            _config = config;
            _dbConField = new SqlConnection(connectionString);
            _httpGet = httpGet ?? throw new ArgumentNullException(nameof(httpGet));
        }
        public void ProcessIncidents(string proyectID)
        {
            try
            {
                DbClearTmpTables();
                DbCheckTmpTables();


                DatosActualesField();
                GetFields(proyectID);
                DbUpdateProjectData(proyectID);
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Incidentes: ERROR {{ProcessFields}}: {ex.Message}", 0);
            }
        }

        public void DbClearTmpTables()
        {
            Areastmp?.Clear();
            Incidentestmp?.Clear();
        }

        public void DbCheckTmpTables()
        {
            Areastmp?.Clear();
            Areastmp = new DataTable("Areas_tmp");
            Areastmp.Columns.Add("ACXProjectId", typeof(string));
            Areastmp.Columns.Add("AreaId", typeof(string));
            Areastmp.Columns.Add("Name", typeof(string));
            Areastmp.Columns.Add("ParentId", typeof(string));
            Areastmp.Columns.Add("IsRoot", typeof(string));

            Incidentestmp?.Clear();
            Incidentestmp = new DataTable("Incidentestmp_tmp");

            string[] cols = new string[]
            {
            "tipo", "descripcion", "estatus", "area", "numero", "detalleubicacion",
            "asignadoa", "fechalimite", "cerradopor", "fechaCierre", "AccionesCorrectivas",
            "AccionesInmediatas", "ClasificacinDelAccidente", "ClasificaciondeIncidenteAmbiental",
            "queocurrio", "ConTiempoPerdido", "Costo", "Criticidad", "CuandoOcurri",
            "DondeOcurri", "EmpresaContratista", "EstndarDeCalidadNoCumplido", "Hora",
            "Impacto", "ImpactoAmbiental", "ImpactoDeLaNoConfirmidad", "OtraClasificacionDeAccidente",
            "ProcesoAfectado", "ResponsableExterno", "ResponsableInterno", "TipoDeAccidente",
            "TipoDeCuasiAccidente", "TipoDeNoConformidadDeCalidad", "TipoDeNoConformidadSgi",
            "FechadeCreacion"
            };

            foreach (var col in cols)
                Incidentestmp.Columns.Add(col, typeof(string));
        }

        public void DatosActualesField()
        {
            try
            {
                if (_dbConField.State == ConnectionState.Closed)
                    _dbConField.Open();

                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM incidentes", _dbConField))
                {
                    int totalDocs = (int)cmd.ExecuteScalar();
                    Utilities.Wlog("Incidentes: Total de Incidentes antes del proceso " + totalDocs, 1);
                }
            }
            catch (Exception ex)
            {
                Utilities.Wlog("Incidentes: ERROR {datosactuales}: " + ex.Message, 0);
            }
        }

        public void GetFields(string projId)
        {
            // Descarga campos personalizados
            GetCustomFields(projId);

            // Descarga �reas y llena Areastmp
            GetAreas(projId);

            string ARoot = GetRoot(projId);

            AppState.totIncAconex = 0;

            if (Areastmp.Rows.Count > 0)
            {
                foreach (DataRow areaRow in Areastmp.Rows)
                {
                    GetIssues(projId, areaRow["AreaId"].ToString());
                }
            }
        }

        public void GetCustomFields(string projId)
        {
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                string authCode = Utilities.EncodeTexto(_config["ACXUser"] + ":" + _config["ACXPass"]);
                string uri = $"https://us1.aconex.com/field-management/api/projects/{projId}/customfields";

                string responseFromServer = _httpGet.GetStringAsync(new AconexHttpGetRequest
                {
                    Url = uri,
                    AuthorizationHeaderBase64 = authCode,
                    Accept = "application/json",
                    ExtraHeaders = new[] { ("X-Application", _config["FieldIntegrationId"]) }
                }).GetAwaiter().GetResult();

                mCustomFields = new Dictionary<string, string>();

                var rst = JsonConvert.DeserializeObject<SystemCustomFields>(responseFromServer);

                foreach (var cfield in rst.custom_fields)
                {
                    mCustomFields.Add(cfield.id, cfield.label);
                }

                // Campos adicionales manuales
                mCustomFields.Add("1353331688026413746", "Empresa Contratista");
                mCustomFields.Add("1353331688026413782", "Proceso Afectado");
            }
            catch (Exception ex)
            {
                Utilities.Wlog("Incidentes: ERROR {GetCustomFields}: " + ex.Message, 0);
            }
        }

        public void GetAreas(string projId)
        {
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                string authCode = Utilities.EncodeTexto(_config["ACXUser"] + ":" + _config["ACXPass"]);
                string uri = $"https://us1.aconex.com/field-management/api/projects/{projId}/areas";

                string responseFromServer = _httpGet.GetStringAsync(new AconexHttpGetRequest
                {
                    Url = uri,
                    AuthorizationHeaderBase64 = authCode,
                    Accept = "application/json",
                    ExtraHeaders = new[] { ("X-Application", _config["FieldIntegrationId"]) }
                }).GetAwaiter().GetResult();

                var rst = JsonConvert.DeserializeObject<Areas>(responseFromServer);

                Area root = rst.areas[0];
                DataRow row = Areastmp.NewRow();
                row["ACXProjectId"] = projId;
                row["AreaId"] = root.id;
                row["Name"] = root.name;
                row["IsRoot"] = true;
                Areastmp.Rows.Add(row);

                foreach (Area area in root.children)
                {
                    AddArea(projId, area, root.id);
                }
            }
            catch (Exception ex)
            {
                Utilities.Wlog("Incidentes: ERROR {GetAreas}: " + ex.Message, 0);
            }
        }

        public void AddArea(string projId, Area mArea, string parentId)
        {
            try
            {
                if (mArea.children != null && mArea.children.Count > 0)
                {
                    foreach (Area ar in mArea.children)
                    {
                        AddArea(projId, ar, mArea.id);
                    }
                }

                DataRow row = Areastmp.NewRow();
                row["ACXProjectId"] = projId;
                row["AreaId"] = mArea.id;
                row["Name"] = mArea.name;
                row["ParentId"] = parentId;
                row["IsRoot"] = false;
                Areastmp.Rows.Add(row);
            }
            catch (Exception ex)
            {
                Utilities.Wlog("Incidentes: ERROR {AddArea}: " + ex.Message, 0);
            }
        }
        public string GetRoot(string projId)
        {
            return Areastmp.Rows[0]["AreaId"].ToString();
        }

        public void GetIssues(string projId, string rootId)
        {
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                string authCode = Utilities.EncodeTexto(_config["ACXUser"] + ":" + _config["ACXPass"]);
                string uri = $"https://us1.aconex.com/field-management/api/projects/{projId}/areas/{rootId}/issues?include_shared=true&page_number=1&page_size=500";

                string responseFromServer = _httpGet.GetStringAsync(new AconexHttpGetRequest
                {
                    Url = uri,
                    AuthorizationHeaderBase64 = authCode,
                    Accept = "application/vnd.aconex.issues.v2+json",
                    ExtraHeaders = new[] { ("X-Application", _config["FieldIntegrationId"]) }
                }).GetAwaiter().GetResult();

                var rst = JsonConvert.DeserializeObject<Issues>(responseFromServer);

                int totChks = 0;
                AppState.totalobs = 0; // Asumimos que totalobs es un campo de clase

                foreach (var iss in rst.issues)
                {
                    totChks++;
                    AddIssue(projId, iss);
                }

                AppState.totIncAconex += totChks;
            }
            catch (Exception ex)
            {
                Utilities.Wlog("Incidentes: ERROR {GetIssues}: " + ex.Message, 0);
            }
        }

        public void AddIssue(string prjid, Issue missue)
        {
            try
            {
                string descripcion = missue.description;
                string estatus = missue.status;
                string area = missue.area?.name; // dynamic
                string numero = missue.issue_number;
                string detalleubicacion = missue.location_detail;
                string asignadoa = "";
                string tipo = missue.issue_type?.name; // dynamic

                // assigned_to
                if (missue.assigned_to != null)
                {
                    try
                    {
                        dynamic assigned = missue.assigned_to;
                        string nombre = assigned.user?.first_name ?? "";
                        string apellido = assigned.user?.last_name ?? "";
                        string org = assigned.organization?.name ?? "";
                        asignadoa = $"{nombre} {apellido} ({org})";
                    }
                    catch
                    {
                        asignadoa = "";
                    }
                }

                // closed_by
                string fechalimite = missue.due_date;
                string cerradopor = "";
                string fechaCierre = "";
                if (missue.closed_by != null)
                {
                    try
                    {
                        dynamic closed = missue.closed_by;
                        string nombre = closed.user?.first_name ?? "";
                        string apellido = closed.user?.last_name ?? "";
                        string org = closed.organization?.name ?? "";
                        cerradopor = $"{nombre} {apellido} ({org})";
                        fechaCierre = missue.closed_at;
                    }
                    catch
                    {
                        cerradopor = "";
                    }
                }

                string FechadeCreacion = missue.meta_data?.created_at;

                // Inicializar los campos custom
                string AccionesCorrectivas = "", AccionesInmediatas = "", ClasificacinDelAccidente = "",
                       ClasificaciondeIncidenteAmbiental = "", queocurrio = "", ConTiempoPerdido = "",
                       Costo = "", Criticidad = "", CuandoOcurri = "", DondeOcurri = "", EmpresaContratista = "",
                       EstndarDeCalidadNoCumplido = "", Hora = "", Impacto = "", ImpactoAmbiental = "",
                       ImpactoDeLaNoConfirmidad = "", OtraClasificacionDeAccidente = "", ProcesoAfectado = "",
                       ResponsableExterno = "", ResponsableInterno = "", TipoDeAccidente = "",
                       TipoDeCuasiAccidente = "", TipoDeNoConformidadDeCalidad = "", TipoDeNoConformidadSgi = "";

                // Procesar custom fields
                foreach (var prf in missue.custom_fields)
                {
                    if (!string.IsNullOrEmpty(prf.value))
                    {
                        switch (mCustomFields[prf.id])
                        {
                            case "Acciones Correctivas": AccionesCorrectivas = prf.value; break;
                            case "Acciones Inmediatas": AccionesInmediatas = prf.value; break;
                            case "Impacto Ambiental": ImpactoAmbiental = prf.value; break;
                            case "Clasificaci�n del Accidente": ClasificacinDelAccidente = prf.value; break;
                            case "Con tiempo perdido": ConTiempoPerdido = prf.value; break;
                            case "Costo": Costo = prf.value; break;
                            case "Criticidad": Criticidad = prf.value; break;
                            case "Cuando Ocurri�": CuandoOcurri = prf.value; break;
                            case "Empresa Contratista": EmpresaContratista = prf.value; break;
                            case "Estandar de Calidad no Cumplido": EstndarDeCalidadNoCumplido = prf.value; break;
                            case "Hora": Hora = prf.value; break;
                            case "Impacto": Impacto = prf.value; break;
                            case "Impacto de la No Conformidad": ImpactoDeLaNoConfirmidad = prf.value; break;
                            case "Clasificaci�n de Incidente Ambiental": ClasificaciondeIncidenteAmbiental = prf.value; break;
                            case "Otra Clasificacion de Accidente": OtraClasificacionDeAccidente = prf.value; break;
                            case "Proceso Afectado": ProcesoAfectado = prf.value; break;
                            case "Que Ocurri�": queocurrio = prf.value; break;
                            case "Responsable Externo": ResponsableExterno = prf.value; break;
                            case "Responsable Interno": ResponsableInterno = prf.value; break;
                            case "Tipo de Accidente": TipoDeAccidente = prf.value; break;
                            case "Tipo de Cuasi Accidente": TipoDeCuasiAccidente = prf.value; break;
                            case "Tipo de No Conformidad SGI": TipoDeNoConformidadSgi = prf.value; break;
                            case "Tipo de No Conformidad de Calidad": TipoDeNoConformidadDeCalidad = prf.value; break;
                        }
                    }
                }

                // Agregar fila al DataTable
                DataRow row1 = Incidentestmp.NewRow();
                row1["tipo"] = tipo;
                row1["descripcion"] = descripcion;
                row1["estatus"] = estatus;
                row1["area"] = area;
                row1["numero"] = numero;
                row1["detalleubicacion"] = detalleubicacion;
                row1["asignadoa"] = asignadoa;
                row1["fechalimite"] = fechalimite;
                row1["cerradopor"] = cerradopor;
                row1["fechaCierre"] = fechaCierre;
                row1["AccionesCorrectivas"] = AccionesCorrectivas;
                row1["AccionesInmediatas"] = AccionesInmediatas;
                row1["ClasificacinDelAccidente"] = ClasificacinDelAccidente;
                row1["ClasificaciondeIncidenteAmbiental"] = ClasificaciondeIncidenteAmbiental;
                row1["queocurrio"] = queocurrio;
                row1["ConTiempoPerdido"] = ConTiempoPerdido;
                row1["Costo"] = Costo;
                row1["Criticidad"] = Criticidad;
                row1["CuandoOcurri"] = CuandoOcurri;
                row1["DondeOcurri"] = DondeOcurri;
                row1["EmpresaContratista"] = EmpresaContratista;
                row1["EstndarDeCalidadNoCumplido"] = EstndarDeCalidadNoCumplido;
                row1["Hora"] = Hora;
                row1["Impacto"] = Impacto;
                row1["ImpactoAmbiental"] = ImpactoAmbiental;
                row1["ImpactoDeLaNoConfirmidad"] = ImpactoDeLaNoConfirmidad;
                row1["OtraClasificacionDeAccidente"] = OtraClasificacionDeAccidente;
                row1["ProcesoAfectado"] = ProcesoAfectado;
                row1["ResponsableExterno"] = ResponsableExterno;
                row1["ResponsableInterno"] = ResponsableInterno;
                row1["TipoDeAccidente"] = TipoDeAccidente;
                row1["TipoDeCuasiAccidente"] = TipoDeCuasiAccidente;
                row1["TipoDeNoConformidadDeCalidad"] = TipoDeNoConformidadDeCalidad;
                row1["TipoDeNoConformidadSgi"] = TipoDeNoConformidadSgi;
                row1["FechadeCreacion"] = FechadeCreacion;

                Incidentestmp.Rows.Add(row1);
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"Incidentes: ERROR al agregar fila a Incidentestmp: {ex.Message}", 0);
            }
        }

        public void DbUpdateProjectData(string projid)
        {
            if (_dbConField.State == ConnectionState.Closed)
                _dbConField.Open();

            using (var transaction = _dbConField.BeginTransaction())
            {
                try
                {
                    // --- �reas ---
                    if (Areastmp.Rows.Count > 0)
                    {
                        using (var s = new SqlBulkCopy(_dbConField, SqlBulkCopyOptions.Default, transaction))
                        {
                            s.DestinationTableName = "Areas_tmp";
                            s.WriteToServer(Areastmp);
                        }

                        using (var sc = new SqlCommand("DELETE FROM Areas WHERE ACXProjectId=@projid", _dbConField, transaction))
                        {
                            sc.Parameters.AddWithValue("@projid", projid);
                            sc.ExecuteNonQuery();
                        }

                        using (var sc = new SqlCommand(@"
                    INSERT INTO [dbo].[Areas] ([ACXProjectId],[AreaId],[Name],[ParentId],[IsRoot])
                    SELECT [ACXProjectId],[AreaId],[Name],[ParentId],[IsRoot] FROM [Areas_tmp]", _dbConField, transaction))
                        {
                            sc.ExecuteNonQuery();
                        }

                        using (var sc = new SqlCommand("TRUNCATE TABLE Areas_tmp", _dbConField, transaction))
                        {
                            sc.ExecuteNonQuery();
                        }
                    }

                    // --- Incidentes ---
                    if (Incidentestmp.Rows.Count > 0)
                    {
                        AppState.totIncDescar = Incidentestmp.Rows.Count;

                        using (var s = new SqlBulkCopy(_dbConField, SqlBulkCopyOptions.Default, transaction))
                        {
                            s.DestinationTableName = "Incidentes_tmp";
                            s.WriteToServer(Incidentestmp);
                        }

                        using (var sc = new SqlCommand("DELETE FROM incidentes", _dbConField, transaction))
                        {
                            sc.ExecuteNonQuery();
                        }

                        using (var sc = new SqlCommand(@"
                    INSERT INTO [dbo].[incidentes] (
                        [tipo],[descripcion],[estatus],[area],[numero],[detalleubicacion],[asignadoa],
                        [fechalimite],[cerradopor],[fechaCierre],[AccionesCorrectivas],[AccionesInmediatas],
                        [ClasificacinDelAccidente],[ClasificaciondeIncidenteAmbiental],[queocurrio],
                        [ConTiempoPerdido],[Costo],[Criticidad],[CuandoOcurri],[DondeOcurri],[EmpresaContratista],
                        [EstndarDeCalidadNoCumplido],[Hora],[Impacto],[ImpactoAmbiental],[ImpactoDeLaNoConfirmidad],
                        [OtraClasificacionDeAccidente],[ProcesoAfectado],[ResponsableExterno],[ResponsableInterno],
                        [TipoDeAccidente],[TipoDeCuasiAccidente],[TipoDeNoConformidadDeCalidad],[TipoDeNoConformidadSgi],
                        [FechadeCreacion])
                    SELECT * FROM [Incidentes_tmp]", _dbConField, transaction))
                        {
                            sc.ExecuteNonQuery();
                        }

                        using (var sc = new SqlCommand("TRUNCATE TABLE Incidentes_tmp", _dbConField, transaction))
                        {
                            sc.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Utilities.Wlog("Error en DbUpdateProjectDataOptimized: " + ex.Message, 0);
                }
                finally
                {
                    _dbConField.Close();
                }
            }
        }

    }

}
