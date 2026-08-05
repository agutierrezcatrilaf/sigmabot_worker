using System.Collections.Generic;

namespace SigmabotSync.Domain.Ports
{
    /// <summary>Solicitud GET a la API Aconex (cabeceras equivalentes a HttpWebRequest/WebRequest).</summary>
    public sealed class AconexHttpGetRequest
    {
        public string Url { get; init; }
        public string AuthorizationHeaderBase64 { get; init; }
        public string Accept { get; init; }
        /// <summary>Opcional (p. ej. mail API usa Content-Type en GET).</summary>
        public string ContentType { get; init; }
        /// <summary>Cabeceras extra (X-Application-Key, X-Application, etc.).</summary>
        public IReadOnlyList<(string Name, string Value)> ExtraHeaders { get; init; }
    }
}
