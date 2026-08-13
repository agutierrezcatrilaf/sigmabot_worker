namespace SigmabotSync.Application.Common
{
    /// <summary>Nivel máximo de verbosidad para <see cref="Utilities.Wlog"/>.</summary>
    public static class LogLevelConfig
    {
        /// <summary>0 = solo mensajes con nivel 0 (Info). 2 = incluye nivel 1 y 2 (Debug).</summary>
        public static int MaxNivelVerbose { get; private set; } = 0;

        public static void Configure(string level)
        {
            if (string.Equals(level?.Trim(), "Debug", System.StringComparison.OrdinalIgnoreCase))
                MaxNivelVerbose = 2;
            else
                MaxNivelVerbose = 0;
        }
    }
}
