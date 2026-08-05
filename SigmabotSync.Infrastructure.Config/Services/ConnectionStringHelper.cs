using System;

namespace SigmabotSync.Infrastructure.Services
{
    /// <summary>
    /// Normalización de cadenas de conexión SQL (misma regla que el consola).
    /// </summary>
    public static class ConnectionStringHelper
    {
        /// <summary>
        /// Añade TrustServerCertificate=True si no está presente (certificados no confiables en .NET 8).
        /// </summary>
        public static string AsegurarTrustServerCertificate(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return connectionString;
            const string key = "TrustServerCertificate=";
            if (connectionString.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                return connectionString.Trim();
            var separator = connectionString.TrimEnd().EndsWith(";", StringComparison.Ordinal) ? "" : ";";
            return connectionString.Trim() + separator + "TrustServerCertificate=True;";
        }
    }
}
