using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SigmabotSync.Application.Synchronization;
using SigmabotSync.Domain.Execution;

namespace SigmabotSync.Application.Common
{
    public static class EjecucionResumenSerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        public static string ToJson(EjecucionResumen resumen)
        {
            if (resumen == null)
                return null;
            return JsonConvert.SerializeObject(resumen, Settings);
        }

        public static ProjectSyncDireccionResumen FromTransmittalResult(TransmittalSyncProjectResult result)
        {
            if (result == null)
                return null;

            return new ProjectSyncDireccionResumen
            {
                Origen = result.SourceLabel ?? result.SourceProjectId,
                Destino = result.TargetLabel ?? result.TargetProjectId,
                Mails = result.TotalMails,
                MailsListados = result.TotalMailsListados > 0 ? result.TotalMailsListados : result.TotalMails,
                Procesados = result.ProcessedMails,
                OmitidosYaProcesados = result.SkippedAlreadyProcessed,
                DescartadosSubject = result.SkippedSubjectFilter,
                Marcadores = result.PlaceholdersCreated,
                Archivos = result.FilesApplied,
                Errores = result.Errors
            };
        }
    }
}
