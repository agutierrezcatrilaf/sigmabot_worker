using System.Threading;
using System.Threading.Tasks;

namespace SigmabotSync.Domain.Ports
{
    /// <summary>
    /// Estado de ProjectSync cross-project.
    /// Mails procesados: <paramref name="projectId"/> = proyecto origen (donde se leyó el transmittal).
    /// Mapeo documentos: <paramref name="projectId"/> = proyecto destino (donde está el DocumentId local en Aconex).
    /// </summary>
    public interface ITransmittalSyncStatePort
    {
        Task<bool> IsMailProcessedAsync(
            int idTrabajo,
            string sourceProjectId,
            string mailId,
            CancellationToken cancellationToken = default);

        Task MarkMailProcessedAsync(
            int idTrabajo,
            string sourceProjectId,
            string mailId,
            CancellationToken cancellationToken = default);

        Task<string> GetLocalDocumentIdAsync(
            int idTrabajo,
            string targetProjectId,
            string documentNo,
            string revision,
            CancellationToken cancellationToken = default);

        Task SaveLocalDocumentMappingAsync(
            int idTrabajo,
            string targetProjectId,
            string documentNo,
            string revision,
            string localDocumentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Opción A: docno+rev+versión en proyecto origen ya sincronizado.
        /// Si <paramref name="sourceVersionNumber"/> está vacío, usa <paramref name="sourceDocumentId"/>.
        /// </summary>
        Task<bool> IsSourceDocumentSyncedAsync(
            int idTrabajo,
            string sourceProjectId,
            string sourceDocumentNo,
            string sourceRevision,
            string sourceVersionNumber,
            string sourceDocumentId,
            CancellationToken cancellationToken = default);

        Task MarkSourceDocumentSyncedAsync(
            int idTrabajo,
            string sourceProjectId,
            string sourceDocumentNo,
            string sourceRevision,
            string sourceVersionNumber,
            string sourceDocumentId,
            string destProjectId,
            string destDocumentId,
            string destDocumentNo,
            string destRevision,
            string mailId,
            CancellationToken cancellationToken = default);
    }
}
