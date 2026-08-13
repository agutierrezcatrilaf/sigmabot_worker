using System;

using System.Collections.Generic;

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



        public event Action<string, int> OnStatus;



        public TransmittalSyncWorker(TransmittalSyncService service)

        {

            _service = service ?? throw new ArgumentNullException(nameof(service));

        }



        public async Task<IReadOnlyList<TransmittalSyncProjectResult>> RunAsync(

            TransmittalSyncRunRequest request,

            CancellationToken cancellationToken = default)

        {

            var resultados = new List<TransmittalSyncProjectResult>();



            if (request == null)

                throw new ArgumentNullException(nameof(request));



            var proyectos = request.Proyectos?.Where(p => p != null && !string.IsNullOrWhiteSpace(p.ProjectId)).ToList();

            if (proyectos == null || proyectos.Count == 0)

            {

                OnStatus?.Invoke("No hay proyectos configurados para sincronizar.", 0);

                return resultados;

            }



            if (proyectos.Count < 2)

            {

                OnStatus?.Invoke("ProjectSync requiere dos proyectos (lado 1 y lado 2) para sincronización cruzada.", 0);

                return resultados;

            }



            OnStatus?.Invoke($"ProjectSync cross-project: {proyectos.Count} proyecto(s), lookback {request.DiasLookback} días.", 0);



            for (int i = 0; i < proyectos.Count; i++)

            {

                for (int j = 0; j < proyectos.Count; j++)

                {

                    if (i == j)

                        continue;



                    var source = proyectos[i];

                    var target = proyectos[j];



                    cancellationToken.ThrowIfCancellationRequested();

                    OnStatus?.Invoke($"--- Origen: {source.Label} ({source.ProjectId}) → Destino: {target.Label} ({target.ProjectId}) ---", 0);



                    var result = await _service.ProcessCrossProjectAsync(

                        request,

                        source,

                        target,

                        (msg, nivel) => OnStatus?.Invoke(msg, nivel),

                        cancellationToken).ConfigureAwait(false);



                    result.SourceLabel = source.Label;

                    result.TargetLabel = target.Label;

                    resultados.Add(result);



                    OnStatus?.Invoke(

                        $"Resumen {source.Label} → {target.Label}: procesados={result.ProcessedMails}, " +

                        $"ya_procesados={result.SkippedAlreadyProcessed}, archivos={result.FilesApplied}, errores={result.Errors}",

                        0);

                }

            }



            OnStatus?.Invoke("Sincronización cross-project finalizada.", 0);

            return resultados;

        }

    }

}


