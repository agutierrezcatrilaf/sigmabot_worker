using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SigmabotSync.Domain.Entities;

namespace SigmabotSync.Domain.Ports
{
    /// <summary>Lee campos por proyecto y enlaces; resuelve mapeo origen→destino para Register.</summary>
    public interface ITransmittalSyncFieldMapPort
    {
        Task<IReadOnlyList<TransmittalSyncCampoMapeoItem>> GetMappingsAsync(
            int idTrabajo,
            string acxProjectIdOrigen,
            string acxProjectIdDestino,
            CancellationToken cancellationToken = default);
    }
}
