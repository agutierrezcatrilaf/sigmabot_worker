using System.Threading;
using System.Threading.Tasks;
using SigmabotSync.Domain.Models.Synchronization;

namespace SigmabotSync.Domain.Ports
{
    /// <summary>Lee TiposDocumentos, EstatusDocumentos y equivalencias ProjectSync desde CredencialBD.</summary>
    public interface IAconexDocumentCatalogPort
    {
        Task<AconexDocumentCatalog> LoadCatalogAsync(CancellationToken cancellationToken = default);

        /// <summary>Incluye equivalencias del par origen→destino para el IdTrabajo.</summary>
        Task<AconexDocumentCatalog> LoadCatalogAsync(
            int idTrabajo,
            string acxProjectIdOrigen,
            string acxProjectIdDestino,
            CancellationToken cancellationToken = default);
    }
}
