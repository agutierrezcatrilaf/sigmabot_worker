using System;
using System.IO;
using System.Text;
using SigmabotSync.Application.Common;

namespace SigmabotSync.Console
{
    /// <summary>
    /// Escribe en consola y además en un archivo de log por día (SigmabotSync_yyyy-MM-dd.log).
    /// Opcionalmente duplica en un archivo por ejecución (job-{trabajo}-ejec-{id}.log).
    /// </summary>
    public sealed class DailyLogWriter : TextWriter
    {
        private readonly TextWriter _console;
        private readonly object _lock = new object();
        private readonly string _logDirectory;

        public override Encoding Encoding => _console.Encoding;

        public DailyLogWriter(TextWriter consoleOut, string logDirectory)
        {
            _console = consoleOut ?? throw new ArgumentNullException(nameof(consoleOut));
            _logDirectory = logDirectory ?? throw new ArgumentNullException(nameof(logDirectory));
            try
            {
                if (!Directory.Exists(_logDirectory))
                    Directory.CreateDirectory(_logDirectory);
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"[Aviso] No se pudo crear el directorio de log '{_logDirectory}': {ex.Message}", 0);
            }
        }

        private string GetDailyLogPath()
        {
            return Path.Combine(_logDirectory, "SigmabotSync_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");
        }

        public override void Write(char value)
        {
            _console.Write(value);
        }

        public override void Write(string value)
        {
            _console.Write(value);
        }

        public override void WriteLine()
        {
            _console.WriteLine();
            AppendToFiles(Environment.NewLine);
        }

        public override void WriteLine(string value)
        {
            _console.WriteLine(value);
            var line = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + (value ?? "") + Environment.NewLine;
            AppendToFiles(line);
        }

        public override void WriteLine(object value)
        {
            WriteLine(value?.ToString() ?? "");
        }

        private void AppendToFiles(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            lock (_lock)
            {
                AppendToFile(GetDailyLogPath(), text);
                var ejecucion = DailyLog.GetRutaLogEjecucionInterna();
                if (!string.IsNullOrEmpty(ejecucion))
                    AppendToFile(ejecucion, text);
            }
        }

        private void AppendToFile(string path, string text)
        {
            try
            {
                File.AppendAllText(path, text);
            }
            catch (Exception ex)
            {
                try { _console.WriteLine($"[Aviso] No se pudo escribir en el archivo de log '{path}': {ex.Message}"); } catch { }
            }
        }
    }

    /// <summary>
    /// Inicializa el log diario y opcionalmente un log por ejecución de trabajo.
    /// </summary>
    public static class DailyLog
    {
        private static bool _inicializado;
        private static string _logDirectory;
        private static string _executionLogPath;

        /// <summary>Ruta del archivo de log del día actual.</summary>
        public static string GetRutaLogActual()
        {
            var dir = ResolverDirectorioLog(_logDirectory);
            return Path.Combine(dir, "SigmabotSync_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");
        }

        /// <summary>Ruta del log de ejecución activo, o null si no hay uno.</summary>
        public static string GetRutaLogEjecucionActual() => _executionLogPath;

        internal static string GetRutaLogEjecucionInterna() => _executionLogPath;

        /// <summary>Archivo dedicado: job-{idTrabajo}-ejec-{idEjecucion}.log en el directorio de logs.</summary>
        public static string IniciarLogEjecucion(int idTrabajo, int idEjecucion)
        {
            var dir = ResolverDirectorioLog(_logDirectory);
            try
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"[Aviso] No se pudo crear directorio de log de ejecución: {ex.Message}", 0);
            }

            _executionLogPath = Path.Combine(dir, $"job-{idTrabajo}-ejec-{idEjecucion}.log");
            try
            {
                File.AppendAllText(
                    _executionLogPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] === Inicio ejecución IdTrabajo={idTrabajo}, IdEjecucion={idEjecucion} ===" + Environment.NewLine);
            }
            catch
            {
                // ignorar
            }

            return _executionLogPath;
        }

        public static void FinalizarLogEjecucion()
        {
            _executionLogPath = null;
        }

        public static void Inicializar(string configuredDirectory = null)
        {
            if (_inicializado) return;
            try
            {
                _logDirectory = configuredDirectory;
                var original = System.Console.Out;
                System.Console.SetOut(new DailyLogWriter(original, ResolverDirectorioLog(configuredDirectory)));
                _inicializado = true;
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"[Aviso] No se pudo inicializar el log diario (la consola sigue funcionando): {ex.Message}", 0);
            }
        }

        internal static string ResolverDirectorioLog(string configuredDirectory)
        {
            if (!string.IsNullOrWhiteSpace(configuredDirectory))
            {
                var path = configuredDirectory.Trim();
                if (!Path.IsPathRooted(path))
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                return path;
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        }
    }
}
