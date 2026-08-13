namespace SigmabotSync.Domain.Config
{
    /// <summary>Configuración de logging en settings.json.</summary>
    public sealed class LoggingSettings
    {
        /// <summary>
        /// Directorio para archivos SigmabotSync_yyyy-MM-dd.log.
        /// Si es relativo, se resuelve contra BaseDirectory de la consola.
        /// Vacío o null → Logs bajo BaseDirectory.
        /// </summary>
        public string Directory { get; set; }

        /// <summary>Info (solo mensajes nivel 0) o Debug (nivel 0–2).</summary>
        public string Level { get; set; }
    }
}
