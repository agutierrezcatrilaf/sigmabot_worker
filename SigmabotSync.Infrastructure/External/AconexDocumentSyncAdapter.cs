using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SigmabotSync.Domain.Models;
using SigmabotSync.Domain.Ports;

namespace SigmabotSync.Infrastructure.External
{
    /// <summary>Adaptador de salida: sincronización por integridad y metadata del registro (Aconex).</summary>
    public sealed class AconexDocumentSyncAdapter : IDocumentSyncReadPort
    {
        private readonly AconexDocumentClient _client;

        public AconexDocumentSyncAdapter(AconexDocumentClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public Task<List<DocumentIntegrityInfo>> GetChangedDocumentsAsync(
            string projectId,
            DateTime since,
            CancellationToken cancellationToken = default)
        {
            return _client.GetChangedDocumentsAsync(projectId, since);
        }

        public Task<DocumentMetadata> GetDocumentMetadataAsync(
            string projectId,
            string documentId,
            CancellationToken cancellationToken = default)
        {
            return _client.GetDocumentMetadataAsync(projectId, documentId);
        }
    }
}
