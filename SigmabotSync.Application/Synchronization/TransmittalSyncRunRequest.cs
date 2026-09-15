using System.Collections.Generic;
using SigmabotSync.Domain.Entities;

namespace SigmabotSync.Application.Synchronization
{
    public sealed class TransmittalSyncRunRequest
    {
        public int IdTrabajo { get; set; }
        public string BaseUrl { get; set; }
        public string AuthorizationHeaderBase64 { get; set; }
        public string IntegrationId { get; set; }
        public string OrgId { get; set; }
        public string UserId { get; set; }
        public int DiasLookback { get; set; } = 30;
        public IReadOnlyList<ProyectoSyncItem> Proyectos { get; set; }

        /// <summary>
        /// Allowlist de status en vuelta SALFA→Codelco (CSV de idEstatus, ej. 1207959768).
        /// Vacío = sin filtro. El status escrito en Codelco sale del Status del adjunto.
        /// </summary>
        public string IdEstatusDocumentoDestino { get; set; }

        /// <summary>Proyecto Aconex usado al resolver idEstatus de allowlist/parámetro (default: lado 1 / IdProyecto).</summary>
        public string IdProyectoEstatusFijo { get; set; }

        /// <summary>Vuelta SALFA→Codelco: Subject debe contener este texto (vacío = sin filtro).</summary>
        public string SubjectFiltroTransmittalVuelta { get; set; }

        /// <summary>Id proyecto lado 2 (SALFA). Para elegir returnFields destino en supersede ida.</summary>
        public string IdProyecto2 { get; set; }

        /// <summary>returnFields extra register/search destino Codelco (supersede vuelta).</summary>
        public IReadOnlyList<string> CamposConsultaRegistroDestino { get; set; }

        /// <summary>returnFields extra register/search destino SALFA (supersede ida).</summary>
        public IReadOnlyList<string> CamposConsultaRegistroDestinoSalfa { get; set; }
    }
}
