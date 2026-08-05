using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SigmabotSync.Domain.Entities;

namespace SigmabotSync.Domain.Config
{
    /// <summary>
    /// Configuración para el caso de uso de extracción de archivos
    /// </summary>
    public class FileExtractionConfig
    {
        /// <summary>
        /// Base URL de la API Aconex (ej. https://us1.aconex.com). Se usa para construir las URLs de búsqueda y descarga.
        /// </summary>
        public string AconexBaseUrl { get; set; }

        /// <summary>
        /// ID del proyecto de Aconex
        /// </summary>
        public string ProjectId { get; set; }

        /// <summary>
        /// Nombre del proyecto (opcional) para usar como carpeta raíz de salida.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// ID de la organización en Aconex
        /// </summary>
        public string OrgId { get; set; }

        /// <summary>
        /// ID del usuario en Aconex
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Header de autorización (Basic base64)
        /// </summary>
        public string AuthorizationHeader { get; set; }

        /// <summary>
        /// Integration ID de Aconex (para X-Application-Key)
        /// </summary>
        public string IntegrationId { get; set; }

        /// <summary>
        /// Tamaño de página para paginado (default: 300)
        /// </summary>
        public int ResultSize { get; set; } = 300;

        /// <summary>
        /// Campos a retornar en la búsqueda
        /// </summary>
        public List<string> ReturnFields { get; set; }

        /// <summary>
        /// Ruta base donde se guardarán los archivos descargados
        /// </summary>
        public string BasePath { get; set; } = @"C:\Users\Usuario\AppData\Local\Temp\SigmaBotFileExtractionSalfa\";

        /// <summary>
        /// Constructor por defecto con campos estándar
        /// </summary>
        public FileExtractionConfig()
        {
            ReturnFields = new List<string>
            {
                "approved", "asBuiltRequired", "attribute1", "attribute2", "attribute3", "attribute4",
                "author", "authorisedBy", "category", "check1", "check2", "comments", "comments2",
                "confidential", "contractDeliverable", "contractnumber", "contractordocumentnumber",
                "contractorrev", "current", "date1", "date2", "discipline", "docno", "doctype", "filename",
                "fileSize", "fileType", "forreview", "markupLastModifiedDate", "milestonedate",
                "numberOfMarkups", "packagenumber", "percentComplete", "plannedsubmissiondate",
                "printSize", "projectField1", "projectField2", "projectField3", "received", "reference",
                "registered", "reviewed", "reviewSource", "reviewstatus", "revision", "revisiondate",
                "selectlist1", "selectlist2", "selectlist3", "selectlist4", "selectlist5", "selectlist6",
                "selectlist7", "selectlist8", "selectlist9", "selectlist10", "scale", "statusid",
                "tagNumber", "title", "toclient", "trackingid", "versionnumber", "vdrcode",
                "vendordocumentnumber", "vendorrev", "versionnumber"
            };
        }

        /// <summary>
        /// Crea la configuración desde un registro Credencial (Tipo = "Aconex"). ProjectId viene de settings; OrgId y UserId de la credencial.
        /// </summary>
        public static FileExtractionConfig FromCredencial(Credencial aconex, string projectId, string basePath = null)
        {
            if (aconex == null)
                throw new ArgumentNullException(nameof(aconex));
            if (string.IsNullOrWhiteSpace(aconex.Aconex_Usuario) || string.IsNullOrWhiteSpace(aconex.Aconex_Clave) || string.IsNullOrWhiteSpace(aconex.Aconex_IntegrationId))
                throw new ArgumentException("La credencial Aconex debe tener Aconex_Usuario, Aconex_Clave y Aconex_IntegrationId");
            if (string.IsNullOrWhiteSpace(projectId))
                throw new ArgumentException("ProjectId es requerido");

            string authHeader = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{aconex.Aconex_Usuario}:{aconex.Aconex_Clave}")
            );

            return new FileExtractionConfig
            {
                AconexBaseUrl = aconex.GetAconexBaseUrl(),
                ProjectId = projectId,
                OrgId = aconex.Aconex_OrganizationId ?? "",
                UserId = aconex.Aconex_UserId ?? "",
                AuthorizationHeader = authHeader,
                IntegrationId = aconex.Aconex_IntegrationId,
                BasePath = string.IsNullOrWhiteSpace(basePath) ? @"C:\Users\Usuario\AppData\Local\Temp\SigmaBotFileExtractionSalfa\" : basePath
            };
        }
    }
}
