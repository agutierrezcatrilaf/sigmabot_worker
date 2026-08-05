using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SigmabotSync.Domain.Models.Extraction;

namespace SigmabotSync.Domain.Ports
{
    /// <summary>
    /// Búsqueda paginada en el registro de documentos (POST .../register/search).
    /// Solo Authorization Basic; sin X-Application-Key (mismo criterio que los workers de extracción).
    /// </summary>
    public interface IAconexRegisterSearchPort
    {
        /// <param name="throwIfNotSuccess">Si es true, lanza ante HTTP no exitoso. Si es false, devuelve error en <see cref="AconexRegisterSearchResult"/>.</param>
        /// <param name="searchQuery">Filtro Lucene (legado). Preferir <paramref name="filterDocumentNo"/>.</param>
        /// <param name="filterDocumentNo">Filtro directo en body JSON (<c>docno</c>), formato soportado por Aconex register/search.</param>
        /// <param name="filterRevision">Filtro directo en body JSON (<c>revision</c>); omitir si revisión comodín.</param>
        /// <param name="filterVersionNumber">Filtro directo en body JSON (<c>versionnumber</c>).</param>
        Task<AconexRegisterSearchResult> SearchRegisterPageAsync(
            string baseUrl,
            string projectId,
            string orgId,
            string userId,
            string authorizationHeaderBase64,
            IReadOnlyList<string> returnFields,
            int resultSize,
            int pageNumber,
            bool throwIfNotSuccess = true,
            CancellationToken cancellationToken = default,
            string searchQuery = null,
            string filterDocumentNo = null,
            string filterRevision = null,
            string filterVersionNumber = null);
    }
}
