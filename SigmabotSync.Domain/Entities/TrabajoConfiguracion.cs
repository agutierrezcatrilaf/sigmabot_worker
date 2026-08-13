using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SigmabotSync.Domain.Config;

namespace SigmabotSync.Domain.Entities
{
    /// <summary>
    /// Registro de la tabla TrabajosConfiguracion. Contiene IdProyecto y los mapeos de campos (CamposConsulta, CamposResponse, CamposBD)
    /// que reemplazan a DocumentFieldMappings del settings.
    /// </summary>
    public class TrabajoConfiguracion
    {
        /// <summary>Identificador del trabajo (por defecto 1).</summary>
        public int IdTrabajo { get; set; }

        /// <summary>Nombre del proyecto para logs (desde TrabajosConfiguracion Nombre=Proyecto, ej. SalfaDemo).</summary>
        public string Proyecto { get; set; }

        /// <summary>ID del proyecto Aconex (lado 1 del par de sincronización).</summary>
        public string IdProyecto { get; set; }

        /// <summary>ID del segundo proyecto Aconex del par (opcional hasta configurar el par completo).</summary>
        public string IdProyecto2 { get; set; }

        /// <summary>Nombre para logs del segundo proyecto (TrabajosConfiguracion Nombre=Proyecto2).</summary>
        public string Proyecto2 { get; set; }

        /// <summary>Días hacia atrás para buscar transmitals en inbox (TrabajosConfiguracion Nombre=DiasLookbackTransmittal). Default 30.</summary>
        public int? DiasLookbackTransmittal { get; set; }

        /// <summary>Días hacia atrás para buscar correos en FullExtraction (TrabajosConfiguracion Nombre=DiasLookbackCorreos). Default 30.</summary>
        public int? DiasLookbackCorreos { get; set; }

        /// <summary>Código proyecto SALFA para nomenclatura docno (TrabajosConfiguracion Nombre=CodigoProyectoSalfa). Ej. 10031671.</summary>
        public string CodigoProyectoSalfa { get; set; }

        /// <summary>Nombres de campos para la consulta API (returnFields). Comma-separated o JSON array. Orden = CamposResponse y CamposBD.</summary>
        public string CamposConsulta { get; set; }

        /// <summary>Nombres de propiedades en el JSON de respuesta. Comma-separated o JSON array. Orden = CamposConsulta y CamposBD.</summary>
        public string CamposResponse { get; set; }

        /// <summary>Nombres de columnas en BD. Comma-separated o JSON array. Orden = CamposConsulta y CamposResponse.</summary>
        public string CamposBD { get; set; }

        /// <summary>Ruta base para extracción de archivos (desde TrabajosConfiguracion Nombre=BasePath).</summary>
        public string BasePath { get; set; }

        /// <summary>Nombre de la tabla de metadata en la BD (desde TrabajosConfiguracion Nombre=TablaMetadata). Ej. <c>DocumentosMetadata</c>.</summary>
        public string TablaMetadata { get; set; }

        /// <summary>Tabla de rutas físicas enlazada por <c>DocumentoId</c> (desde TrabajosConfiguracion Nombre=TablaPaths). Ej. <c>DocumentosPath</c>.</summary>
        public string TablaPaths { get; set; }

        /// <summary>Id de la credencial Aconex en tabla Credenciales (desde TrabajosConfiguracion Nombre=CredencialAconex).</summary>
        public int? CredencialAconexId { get; set; }

        /// <summary>Id de la credencial BD en tabla Credenciales (desde TrabajosConfiguracion Nombre=CredencialBD).</summary>
        public int? CredencialBDId { get; set; }

        /// <summary>
        /// idEstatus fijo al registrar documentos en el proyecto destino (lado 1, ej. Codelco).
        /// Acepta idEstatus numérico o nombre en EstatusDocumentos. Solo ProjectSync.
        /// </summary>
        public string IdEstatusDocumentoDestino { get; set; }

        /// <summary>
        /// Vuelta SALFA→Codelco: solo transmitals cuyo Subject contenga este texto (ej. Final).
        /// Vacío = sin filtro por subject.
        /// </summary>
        public string SubjectFiltroTransmittalVuelta { get; set; }

        /// <summary>
        /// ProjectSync supersede: campos adicionales en register/search del destino (Codelco) para leer project fields.
        /// CSV o JSON array (ej. Proveedor_singleSelect, Especialidad_singleSelect).
        /// </summary>
        public string CamposConsultaRegistroDestino { get; set; }

        /// <summary>
        /// ProjectSync supersede ida: returnFields extra en register/search del destino SALFA.
        /// </summary>
        public string CamposConsultaRegistroDestinoSalfa { get; set; }

        /// <summary>Tipo de trabajo: FileExtraction, ProjectSync, FullExtraction. Viene del campo Tipo de la tabla Trabajos.</summary>
        public string TipoTrabajo { get; set; }

        /// <summary>
        /// Construye la lista de DocumentFieldMapping a partir de CamposConsulta, CamposResponse y CamposBD.
        /// Acepta listas separadas por comas o JSON arrays; mismo número de elementos (por índice: ApiField, JsonProperty, DbColumn).
        /// </summary>
        public List<DocumentFieldMapping> ToDocumentFieldMappings()
        {
            var consulta = ParseStringArray(CamposConsulta);
            var response = ParseStringArray(CamposResponse);
            var bd = ParseStringArray(CamposBD);
            if (consulta == null || consulta.Count == 0)
                return null;
            int n = consulta.Count;
            var list = new List<DocumentFieldMapping>(n);
            for (int i = 0; i < n; i++)
            {
                list.Add(new DocumentFieldMapping
                {
                    ApiField = consulta[i],
                    JsonProperty = (response != null && i < response.Count) ? response[i] : consulta[i],
                    DbColumn = (bd != null && i < bd.Count) ? bd[i] : consulta[i]
                });
            }
            return list;
        }

        /// <summary>Proyectos del par (lado 1 = IdProyecto, lado 2 = IdProyecto2). ProjectSync cruza sentbox origen → registro destino.</summary>
        public List<ProyectoSyncItem> GetProyectosSync()
        {
            var list = new List<ProyectoSyncItem>(2);
            if (!string.IsNullOrWhiteSpace(IdProyecto))
                list.Add(new ProyectoSyncItem(IdProyecto.Trim(), string.IsNullOrWhiteSpace(Proyecto) ? IdProyecto.Trim() : Proyecto.Trim()));
            if (!string.IsNullOrWhiteSpace(IdProyecto2))
                list.Add(new ProyectoSyncItem(IdProyecto2.Trim(), string.IsNullOrWhiteSpace(Proyecto2) ? IdProyecto2.Trim() : Proyecto2.Trim()));
            return list;
        }

        public int ResolverDiasLookbackTransmittal()
        {
            if (DiasLookbackTransmittal.HasValue && DiasLookbackTransmittal.Value > 0)
                return DiasLookbackTransmittal.Value;
            return 30;
        }

        public int ResolverDiasLookbackCorreos()
        {
            if (DiasLookbackCorreos.HasValue && DiasLookbackCorreos.Value > 0)
                return DiasLookbackCorreos.Value;
            return 30;
        }

        /// <summary>
        /// Devuelve la lista de nombres de campos para la consulta API (returnFields), desde CamposConsulta.
        /// </summary>
        public List<string> ToReturnFields()
        {
            var list = ParseStringArray(CamposConsulta);
            return list ?? new List<string>();
        }

        /// <summary>returnFields para register/search del destino Codelco (supersede vuelta).</summary>
        public List<string> ToReturnFieldsRegistroDestino()
        {
            var list = ParseStringArray(CamposConsultaRegistroDestino);
            return list ?? new List<string>();
        }

        /// <summary>returnFields para register/search del destino SALFA (supersede ida).</summary>
        public List<string> ToReturnFieldsRegistroDestinoSalfa()
        {
            var list = ParseStringArray(CamposConsultaRegistroDestinoSalfa);
            return list ?? new List<string>();
        }

        private static List<string> ParseStringArray(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            json = json.Trim();
            try
            {
                var arr = JsonConvert.DeserializeObject<List<string>>(json);
                if (arr != null)
                    return arr.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                // Intentar como array de tokens separados por comas
                if (json.StartsWith("[") && json.EndsWith("]"))
                    return null;
                return json.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            }
            catch
            {
                return json.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            }
        }
    }
}
