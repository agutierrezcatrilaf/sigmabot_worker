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



            OnStatus?.Invoke("Buscando los transmittals de los últimos " + request.DiasLookback + " días.", 0);



            for (int i = 0; i < proyectos.Count; i++)

            {

                for (int j = 0; j < proyectos.Count; j++)

                {

                    if (i == j)

                        continue;



                    var source = proyectos[i];

                    var target = proyectos[j];



                    cancellationToken.ThrowIfCancellationRequested();

                    OnStatus?.Invoke(source.Label + " → " + target.Label + ".", 0);



                    var result = await _service.ProcessCrossProjectAsync(

                        request,

                        source,

                        target,

                        (msg, nivel) => OnStatus?.Invoke(msg, nivel),

                        cancellationToken).ConfigureAwait(false);



                    result.SourceLabel = source.Label;

                    result.TargetLabel = target.Label;

                    resultados.Add(result);



                    EscribirResumenDireccion(result, (msg, nivel) => OnStatus?.Invoke(msg, nivel));

                }

            }



            int sincronizados = resultados.Sum(r => r.ProcessedMails);
            int errores = resultados.Sum(r => r.Errors);
            int omitidosEstado = resultados.Sum(r => r.SkippedByStatus);
            string estado = errores == 0
                ? "sincronización completada"
                : "sincronización completada con errores";
            string linea = "Resultado: " + estado + ". Sincronizados: " + sincronizados + ". Errores: " + errores + ".";
            if (omitidosEstado > 0)
                linea += " Omitidos por estado: " + omitidosEstado + ".";
            OnStatus?.Invoke(linea, 0);

            return resultados;

        }

        private static void EscribirResumenDireccion(TransmittalSyncProjectResult result, Action<string, int> log)
        {
            if (result == null)
                return;

            if (result.TotalMails == 0 && result.SkippedAlreadyProcessed == 0 && result.Errors == 0)
            {
                log("No había transmittals nuevos.", 0);
                return;
            }

            log("Transmittals: " + result.TotalMails
                + ". Sincronizados: " + result.ProcessedMails
                + ". Ya estaban: " + result.SkippedAlreadyProcessed + ".", 0);
            if (result.SkippedSubjectFilter > 0)
                log("Omitidos por asunto: " + result.SkippedSubjectFilter + ".", 0);
            if (result.SkippedByStatus > 0)
                log("Omitidos por estado: " + result.SkippedByStatus + ".", 0);
            log("Marcadores: " + result.PlaceholdersCreated + ". Archivos: " + result.FilesApplied + ".", 0);
            if (result.Errors > 0)
                log("Con error: " + result.Errors + ".", 0);
        }

    }

}


