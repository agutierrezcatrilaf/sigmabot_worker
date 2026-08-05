using System.Threading;
using System.Threading.Tasks;
using SigmabotSync.Domain.Models;

namespace SigmabotSync.Domain.Ports
{
    /// <summary>GET .../register/{documentId}/metadata del proyecto origen.</summary>
    public interface IAconexRegisterMetadataPort
    {
        Task<DocumentMetadata> GetRegisterMetadataAsync(
            string baseUrl,
            string projectId,
            string documentId,
            string authorizationHeaderBase64,
            CancellationToken cancellationToken = default);
    }
}
