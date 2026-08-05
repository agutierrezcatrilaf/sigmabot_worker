using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SigmabotSync.Domain.Models;

namespace SigmabotSync.Domain.Ports
{
    /// <summary>
    /// Lectura de integridad y metadata del registro de documentos en Aconex (caso Synchronization).
    /// </summary>
    public interface IDocumentSyncReadPort
    {
        Task<List<DocumentIntegrityInfo>> GetChangedDocumentsAsync(
            string projectId,
            DateTime since,
            CancellationToken cancellationToken = default);

        Task<DocumentMetadata> GetDocumentMetadataAsync(
            string projectId,
            string documentId,
            CancellationToken cancellationToken = default);
    }
}
