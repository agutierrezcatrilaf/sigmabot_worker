using System.Threading;
using System.Threading.Tasks;

namespace SigmabotSync.Domain.Ports
{
    /// <summary>
    /// Register Document: GET schema y POST multipart (Basic + X-Application-Key opcional).
    /// </summary>
    public interface IAconexRegisterWritePort
    {
        /// <summary>GET .../register/schema. Lanza si la respuesta HTTP no es exitosa.</summary>
        Task<string> GetRegisterSchemaXmlAsync(
            string baseUrl,
            string projectId,
            string authorizationHeaderBase64,
            string integrationIdOrNull,
            CancellationToken cancellationToken = default);

        /// <summary>POST .../register (multipart/mixed).</summary>
        Task<AconexRawHttpResponse> PostRegisterDocumentAsync(
            string baseUrl,
            string projectId,
            string authorizationHeaderBase64,
            string integrationIdOrNull,
            string multipartBody,
            string boundary,
            CancellationToken cancellationToken = default);

        /// <summary>POST .../register/{documentId}/supersede (multipart/mixed; misma forma que Register).</summary>
        Task<AconexRawHttpResponse> PostSupersedeDocumentAsync(
            string baseUrl,
            string projectId,
            string documentId,
            string authorizationHeaderBase64,
            string integrationIdOrNull,
            string multipartBody,
            string boundary,
            CancellationToken cancellationToken = default);
    }
}
