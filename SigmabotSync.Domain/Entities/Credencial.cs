using System;

namespace SigmabotSync.Domain.Entities
{
    /// <summary>
    /// Registro de la tabla Credenciales. Tipo "Aconex" = credenciales Aconex; Tipo "BD" = credenciales para BD de metadata de documentos.
    /// </summary>
    public class Credencial
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Tipo { get; set; }

        // Campos Aconex (usados cuando Tipo = "Aconex")
        public string Aconex_Instancia { get; set; }
        public string Aconex_Usuario { get; set; }
        public string Aconex_Clave { get; set; }
        public string Aconex_IntegrationId { get; set; }
        public string Aconex_OrganizationId { get; set; }
        public string Aconex_UserId { get; set; }

        // Campos BD (usados cuando Tipo = "BD")
        public string BD_Servidor { get; set; }
        public string BD_TipoConexion { get; set; }
        public string BD_Usuario { get; set; }
        public string BD_Clave { get; set; }
        public string BD_BaseDatos { get; set; }

        /// <summary>
        /// Base URL para API Aconex (ej. "https://us1.aconex.com"). Si Aconex_Instancia no tiene protocolo, se antepone "https://".
        /// </summary>
        public string GetAconexBaseUrl()
        {
            if (string.IsNullOrWhiteSpace(Aconex_Instancia))
                return "https://us1.aconex.com";
            var inst = Aconex_Instancia.Trim();
            if (inst.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || inst.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return inst;
            return "https://" + inst;
        }

        /// <summary>
        /// Construye la cadena de conexión SQL para esta credencial BD (válido cuando Tipo = "BD").
        /// </summary>
        public string GetConnectionString()
        {
            if (string.IsNullOrWhiteSpace(BD_Servidor) || string.IsNullOrWhiteSpace(BD_BaseDatos))
                return string.Empty;
            var user = string.IsNullOrWhiteSpace(BD_Usuario) ? "" : $";User Id={BD_Usuario.Trim()}";
            var pass = string.IsNullOrWhiteSpace(BD_Clave) ? "" : $";Password={BD_Clave.Trim()}";
            return $"Server={BD_Servidor.Trim()};Database={BD_BaseDatos.Trim()}{user}{pass};TrustServerCertificate=True;";
        }
    }
}
