using System;

namespace SigmabotSync.Application.Common
{
    /// <summary>Registro de workers con nivel Info (0) o Debug (1).</summary>
    public static class SyncLog
    {
        public static void Info(Action<string, int> log, string message)
        {
            if (log != null)
                log(message, 0);
        }

        public static void Debug(Action<string, int> log, string message)
        {
            if (log != null)
                log(message, 1);
        }
    }
}
