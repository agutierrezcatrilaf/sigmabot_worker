using System;
using System.Threading;

namespace SigmabotSync.Application.Common
{
    public class AppState
    {
        public static string LogFile { get; set; }

        public static long TotDoctosAconex { get; set; }
        public static long totDoctosDescar { get; set; }
        public static long totFlujosAconex { get; set; }
        public static long totPasosFlujosDescar { get; set; }
        public static long totFlujosDescar { get; set; }
        public static long totPasosFlujosAconex { get; set; }
        public static long totIncAconex { get; set; }
        public static long totalobs { get; set; }
        public static long totIncDescar { get; set; }

        public static long totalCorreosRecibidosProcesados;
        public static long totalCorreosEnviadosProcesados;
        public static long totalCorreosRecibidosDescartados;
        public static long totalCorreosEnviadosDescartados;
        public static long totalCorreosRecibidosAconex;
        public static long totalCorreosEnviadosAconex;

        /// <summary>Errores por ítem/etapa absorbidos (no detienen FullExtraction).</summary>
        public static int erroresDocumentos;
        public static int erroresCorreos;
        public static int erroresFlujos;

        public static void IncErroresDocumentos() => Interlocked.Increment(ref erroresDocumentos);
        public static void IncErroresCorreos() => Interlocked.Increment(ref erroresCorreos);
        public static void IncErroresFlujos() => Interlocked.Increment(ref erroresFlujos);

        /// <summary>Pone a cero contadores de extracción (FullExtraction / DocumentExtraction embebido).</summary>
        public static void ResetExtractionCounters()
        {
            TotDoctosAconex = 0;
            totDoctosDescar = 0;
            totFlujosAconex = 0;
            totPasosFlujosDescar = 0;
            totFlujosDescar = 0;
            totPasosFlujosAconex = 0;
            totIncAconex = 0;
            totalobs = 0;
            totIncDescar = 0;
            totalCorreosRecibidosProcesados = 0;
            totalCorreosEnviadosProcesados = 0;
            totalCorreosRecibidosDescartados = 0;
            totalCorreosEnviadosDescartados = 0;
            totalCorreosRecibidosAconex = 0;
            totalCorreosEnviadosAconex = 0;
            erroresDocumentos = 0;
            erroresCorreos = 0;
            erroresFlujos = 0;
        }
    }
}
