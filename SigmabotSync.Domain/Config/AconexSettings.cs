using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SigmabotSync.Domain.Config
{
    /// <summary>Mapeo de un campo de documento: nombre en API Aconex, nombre en JSON y columna en BD.</summary>
    public class DocumentFieldMapping
    {
        /// <summary>Nombre del campo tal como lo espera la API (returnFields).</summary>
        public string ApiField { get; set; }
        /// <summary>Nombre del campo en el JSON de respuesta (p. ej. documentNumber, title).</summary>
        public string JsonProperty { get; set; }
        /// <summary>Nombre de la columna en Documentos / Documentos_tmp.</summary>
        public string DbColumn { get; set; }
    }

    public class AconexSettings
    {
        /// <summary>Conexión a la base de datos donde está la tabla Credenciales. Las credenciales Aconex y BD se leen desde esa tabla.</summary>
        public string DatabaseConnectionString { get; set; }

        [Obsolete("Las credenciales Aconex se obtienen de la tabla Credenciales (Tipo=Aconex). Se mantiene por compatibilidad.")]
        public string UserAconex { get; set; }
        [Obsolete("Las credenciales Aconex se obtienen de la tabla Credenciales (Tipo=Aconex). Se mantiene por compatibilidad.")]
        public string PassAconex { get; set; }
        [Obsolete("Las credenciales Aconex se obtienen de la tabla Credenciales (Tipo=Aconex). Se mantiene por compatibilidad.")]
        public string IntegrationIdAconex { get; set; }

        /// <summary>Obsoleto: la configuración del trabajo (ProjectId, BasePath, campos, credenciales) se obtiene de TrabajosConfiguracion. Se mantiene por compatibilidad en tests.</summary>
        [Obsolete("Usar TrabajosConfiguracion. Se mantiene por compatibilidad en tests.")]
        public ExtractionFilesConfig ExtractionFiles { get; set; }
    }

    public class ExtractionFilesConfig
    {
        public string UserAconex { get; set; }
        public string PassAconex { get; set; }
        public string IntegrationIdAconex { get; set; }
        public string ProjectId { get; set; }
        public string OrgId { get; set; }
        public string UserId { get; set; }
        public string BasePath { get; set; }
        /// <summary>Cadena de conexión a la BD. Si está vacía, se construye con DbServer, DbDatabase, DbUser y DbPassword.</summary>
        public string ConnectionString { get; set; }
        /// <summary>Nombre del proyecto para logs (ej. "Proyecto Salfa").</summary>
        public string ProjectName { get; set; }
        /// <summary>Servidor SQL (ej. "localhost" o "servidor\\instancia"). Se usa para construir ConnectionString si esta está vacía.</summary>
        public string DbServer { get; set; }
        /// <summary>Nombre de la base de datos. Se usa para construir ConnectionString si esta está vacía.</summary>
        public string DbDatabase { get; set; }
        /// <summary>Usuario SQL. Se usa para construir ConnectionString si esta está vacía.</summary>
        public string DbUser { get; set; }
        /// <summary>Contraseña SQL. Se usa para construir ConnectionString si esta está vacía.</summary>
        public string DbPassword { get; set; }

        /// <summary>Mapeo de campos: nombre en API (ApiField), nombre en JSON (JsonProperty) y columna en BD (DbColumn). Id, ACXProjectId y TrackingId siempre se incluyen. Si está vacío se usan valores por defecto (docno, title, revision, versionnumber).</summary>
        public List<DocumentFieldMapping> DocumentFieldMappings { get; set; }

        /// <summary>
        /// Obtiene la cadena de conexión: usa ConnectionString si tiene valor; si no, la genera con DbServer, DbDatabase, DbUser y DbPassword.
        /// </summary>
        public string GetConnectionString()
        {
            if (!string.IsNullOrWhiteSpace(ConnectionString))
                return ConnectionString.Trim();
            if (string.IsNullOrWhiteSpace(DbServer) || string.IsNullOrWhiteSpace(DbDatabase))
                return string.Empty;
            var user = string.IsNullOrWhiteSpace(DbUser) ? "" : $";User Id={DbUser.Trim()}";
            var pass = string.IsNullOrWhiteSpace(DbPassword) ? "" : $";Password={DbPassword}";
            return $"Server={DbServer.Trim()};Database={DbDatabase.Trim()}{user}{pass};";
        }
    }
}
