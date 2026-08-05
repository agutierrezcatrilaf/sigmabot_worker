using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SigmabotSync.Application.Synchronization
{
    /// <summary>
    /// Sincronización cross-project: lee transmitals del origen y crea/actualiza documentos en el registro del destino.
    /// </summary>
    public sealed class TransmittalSyncWorker
    {
        private readonly TransmittalSyncService _service;

        public event Action<string> OnStatus;

        public TransmittalSyncWorker(TransmittalSyncService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public async Task RunAsync(TransmittalSyncRunRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var proyectos = request.Proyectos?.Where(p => p != null && !string.IsNullOrWhiteSpace(p.ProjectId)).ToList();
            if (proyectos == null || proyectos.Count == 0)
            {
                OnStatus?.Invoke("No hay proyectos configurados para sincronizar.");
                return;
            }

            if (proyectos.Count < 2)
            {
                OnStatus?.Invoke("ProjectSync requiere dos proyectos (lado 1 y lado 2) para sincronización cruzada.");
                return;
            }

            OnStatus?.Invoke($"ProjectSync cross-project: {proyectos.Count} proyecto(s), lookback {request.DiasLookback} días.");

            for (int i = 0; i < proyectos.Count; i++)
            {
                for (int j = 0; j < proyectos.Count; j++)
                {
                    if (i == j)
                        continue;

                    var source = proyectos[i];
                    var target = proyectos[j];

                    cancellationToken.ThrowIfCancellationRequested();
                    OnStatus?.Invoke($"--- Origen: {source.Label} ({source.ProjectId}) → Destino: {target.Label} ({target.ProjectId}) ---");

                    var result = await _service.ProcessCrossProjectAsync(
                        request,
                        source,
                        target,
                        msg => OnStatus?.Invoke(msg),
                        cancellationToken).ConfigureAwait(false);

                    OnStatus?.Invoke(
                        $"Resumen {source.Label} → {target.Label}: mails={result.TotalMails}, procesados={result.ProcessedMails}, " +
                        $"omitidos={result.SkippedAlreadyProcessed}, marcadores={result.PlaceholdersCreated}, " +
                        $"archivos={result.FilesApplied}, errores={result.Errors}");
                }
            }

            OnStatus?.Invoke("Sincronización cross-project finalizada.");
        }
    }
}
