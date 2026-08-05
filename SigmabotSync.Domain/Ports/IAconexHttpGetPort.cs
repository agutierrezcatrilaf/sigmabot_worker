using System.Threading;
using System.Threading.Tasks;

namespace SigmabotSync.Domain.Ports
{
    /// <summary>GET HTTP genérico para APIs Aconex (reemplazo de WebRequest en workers de extracción).</summary>
    public interface IAconexHttpGetPort
    {
        Task<string> GetStringAsync(AconexHttpGetRequest request, CancellationToken cancellationToken = default);
    }
}
