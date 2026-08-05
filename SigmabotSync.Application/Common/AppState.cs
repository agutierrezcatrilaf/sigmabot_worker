using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
