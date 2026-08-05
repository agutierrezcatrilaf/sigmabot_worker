using System;
using System.IO;
using System.Text;
using SigmabotSync.Application.Common;

namespace SigmabotSync.Console
{
    /// <summary>
    /// Escribe en consola y además en un archivo de log por día (Logs/SigmabotSync_yyyy-MM-dd.log).
    /// Inicializar al arranque con Inicializar() para que todo Console.WriteLine vaya también al archivo.
    /// </summary>
    public sealed class DailyLogWriter : TextWriter
    {
        private readonly TextWriter _console;
        private readonly object _lock = new object();
        private readonly string _logDirectory;

        public override Encoding Encoding => _console.Encoding;

        public DailyLogWriter(TextWriter consoleOut)
        {
            _console = consoleOut ?? throw new ArgumentNullException(nameof(consoleOut));
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
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

        private string GetLogPath()
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
            AppendToFile(Environment.NewLine);
        }

        public override void WriteLine(string value)
        {
            _console.WriteLine(value);
            var line = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + (value ?? "") + Environment.NewLine;
            AppendToFile(line);
        }

        public override void WriteLine(object value)
        {
            WriteLine(value?.ToString() ?? "");
        }

        private void AppendToFile(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            var path = GetLogPath();
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(path, text);
                }
                catch (Exception ex)
                {
                    try { _console.WriteLine($"[Aviso] No se pudo escribir en el archivo de log: {ex.Message}"); } catch { }
                }
            }
        }
    }

    /// <summary>
    /// Inicializa el log diario: redirige la salida de consola para que también se guarde en Logs/SigmabotSync_yyyy-MM-dd.log.
    /// </summary>
    public static class DailyLog
    {
        private static bool _inicializado;

        /// <summary>
        /// Ruta del archivo de log del día actual (mismo criterio que DailyLogWriter).
        /// Útil para mostrarla al usuario al inicio.
        /// </summary>
        public static string GetRutaLogActual()
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            return Path.Combine(dir, "SigmabotSync_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");
        }

        /// <summary>
        /// Redirige Console.Out para que todo lo que se escribe en consola se guarde también en un archivo por día.
        /// Llamar una sola vez al inicio de Main.
        /// </summary>
        public static void Inicializar()
        {
            if (_inicializado) return;
            try
            {
                var original = System.Console.Out;
                System.Console.SetOut(new DailyLogWriter(original));
                _inicializado = true;
            }
            catch (Exception ex)
            {
                Utilities.Wlog($"[Aviso] No se pudo inicializar el log diario (la consola sigue funcionando): {ex.Message}", 0);
            }
        }
    }
}
