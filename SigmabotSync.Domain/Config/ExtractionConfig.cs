using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SigmabotSync.Domain.Entities;

namespace SigmabotSync.Domain.Config
{
    /// <summary>
    /// Configuración para los workers de extracción. Las credenciales pueden venir de la tabla Credenciales o de settings.
    /// </summary>
    public class ExtractionConfig
    {
        /// <summary>Base URL API Aconex (ej. https://us1.aconex.com).</summary>
        public string AconexBaseUrl { get; set; }

        // Credenciales Aconex
        public string ACXUser { get; set; }
        public string ACXPass { get; set; }
        public string IntegrationIdAconex { get; set; }
        public string FieldIntegrationId { get; set; }

        // Información del proyecto
        public string NombrePrj { get; set; }
        public string OrgId { get; set; }
        public string userid { get; set; }

        // Cadena de conexión a base de datos (donde se escribe la metadata de documentos)
        public string ConnectionString { get; set; }

        /// <summary>Mapeo de campos documento (ApiField, JsonProperty, DbColumn). Si es null/empty el worker usa valores por defecto.</summary>
        public List<DocumentFieldMapping> DocumentFieldMappings { get; set; }

        /// <summary>
        /// Convierte la configuración a Dictionary para compatibilidad con workers existentes
        /// </summary>
        public Dictionary<string, string> ToDictionary()
        {
            var d = new Dictionary<string, string>
            {
                { "AconexBaseUrl", AconexBaseUrl ?? "" },
                { "ACXUser", ACXUser ?? "" },
                { "ACXPass", ACXPass ?? "" },
                { "IntegrationIdAconex", IntegrationIdAconex ?? "" },
                { "FieldIntegrationId", FieldIntegrationId ?? "" },
                { "NombrePrj", NombrePrj ?? "" },
                { "OrgId", OrgId ?? "" },
                { "userid", userid ?? "" }
            };
            if (DocumentFieldMappings != null && DocumentFieldMappings.Count > 0)
                d["DocumentFieldMappings"] = JsonConvert.SerializeObject(DocumentFieldMappings);
            return d;
        }

        /// <summary>
        /// Crea una configuración desde la tabla Credenciales: credencial Aconex (Tipo=Aconex) y credencial BD (Tipo=BD).
        /// La conexión a la BD de documentos viene de la credencial BD; las credenciales Aconex de la credencial Aconex.
        /// </summary>
        public static ExtractionConfig FromCredenciales(Credencial aconex, Credencial bd, string projectName = "", List<DocumentFieldMapping> documentFieldMappings = null)
        {
            if (aconex == null)
                throw new ArgumentNullException(nameof(aconex));
            if (bd == null)
                throw new ArgumentNullException(nameof(bd));

            return new ExtractionConfig
            {
                AconexBaseUrl = aconex.GetAconexBaseUrl(),
                ACXUser = aconex.Aconex_Usuario,
                ACXPass = aconex.Aconex_Clave,
                IntegrationIdAconex = aconex.Aconex_IntegrationId ?? "",
                FieldIntegrationId = aconex.Aconex_IntegrationId ?? "",
                NombrePrj = projectName ?? "Proyecto",
                OrgId = aconex.Aconex_OrganizationId ?? "",
                userid = aconex.Aconex_UserId ?? "",
                ConnectionString = bd.GetConnectionString(),
                DocumentFieldMappings = documentFieldMappings
            };
        }
    }
}
