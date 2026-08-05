using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SigmabotSync.Domain.Models;
using SigmabotSync.Domain.Ports;

namespace SigmabotSync.Application.Synchronization
{
    public class DocumentService
    {
        private readonly IDocumentSyncReadPort _syncRead;
        private readonly IAconexRegisterWritePort _registerWrite;

        public DocumentService(IDocumentSyncReadPort syncRead, IAconexRegisterWritePort registerWrite)
        {
            _syncRead = syncRead ?? throw new ArgumentNullException(nameof(syncRead));
            _registerWrite = registerWrite ?? throw new ArgumentNullException(nameof(registerWrite));
        }

        public Task<List<DocumentIntegrityInfo>> GetChangedDocumentsAsync(string projectId, DateTime since)
        {
            return _syncRead.GetChangedDocumentsAsync(projectId, since);
        }

        public Task<DocumentMetadata> GetDocumentMetadataAsync(string projectID, string id)
        {
            return _syncRead.GetDocumentMetadataAsync(projectID, id);
        }

        /// <summary>
        /// POST .../register/{documentId}/supersede (mismo cuerpo multipart/mixed que Register Document).
        /// Las cookies del curl de prueba no son necesarias si se usa Basic + X-Application-Key como el resto de la API.
        /// </summary>
        public Task<AconexRawHttpResponse> SupersedeDocumentAsync(
            string baseUrl,
            string projectId,
            string documentId,
            string authorizationHeaderBase64,
            string integrationIdOrNull,
            string multipartBody,
            string boundary,
            CancellationToken cancellationToken = default)
        {
            return _registerWrite.PostSupersedeDocumentAsync(
                baseUrl,
                projectId,
                documentId,
                authorizationHeaderBase64,
                integrationIdOrNull,
                multipartBody,
                boundary,
                cancellationToken);
        }
    }
}
