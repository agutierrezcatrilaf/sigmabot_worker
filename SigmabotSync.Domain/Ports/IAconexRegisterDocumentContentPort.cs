using System.Threading;
using System.Threading.Tasks;

namespace SigmabotSync.Domain.Ports
{
    /// <summary>
    /// Descarga del binario de un documento del registro (GET .../register/{documentId}).
    /// Solo Authorization Basic; sin X-Application-Key.
    /// </summary>
    public interface IAconexRegisterDocumentContentPort
    {
        Task<AconexRegisterDocumentDownloadResult> DownloadToFileAsync(
            string baseUrl,
            string projectId,
            string documentId,
            string filePath,
            string authorizationHeaderBase64,
            CancellationToken cancellationToken = default);
    }
}
