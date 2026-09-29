using System;
using System.Text;

namespace SigmabotSync.Application.Common
{
    /// <summary>
    /// Líneas Info del log descargable: motivo en una frase y, si aporta, el detalle sin XML.
    /// </summary>
    public static class ReadableLog
    {
        public static void Error(string motivo, string detalle = null)
        {
            Utilities.Wlog("ERROR: " + motivo, 0);
            WriteDetalle(detalle, motivo);
        }

        public static void Documento(string docNo, string documentId, string motivo, string detalle = null)
        {
            string doc = string.IsNullOrWhiteSpace(docNo) ? "?" : docNo.Trim();
            string id = string.IsNullOrWhiteSpace(documentId) ? "?" : documentId.Trim();
            Utilities.Wlog($"ERROR DocNo={doc} Id={id}: {motivo}", 0);
            WriteDetalle(detalle, motivo);
        }

        public static void Correo(string mailNo, string mailId, string motivo, string detalle = null)
        {
            string no = string.IsNullOrWhiteSpace(mailNo) ? "?" : mailNo.Trim();
            string id = string.IsNullOrWhiteSpace(mailId) ? "?" : mailId.Trim();
            Utilities.Wlog($"ERROR Mail={no} Id={id}: {motivo}", 0);
            WriteDetalle(detalle, motivo);
        }

        public static void Flujo(string numero, string workflowId, string motivo, string detalle = null)
        {
            string nro = string.IsNullOrWhiteSpace(numero) ? "?" : numero.Trim();
            string id = string.IsNullOrWhiteSpace(workflowId) ? "?" : workflowId.Trim();
            Utilities.Wlog($"ERROR Flujo={nro} Id={id}: {motivo}", 0);
            WriteDetalle(detalle, motivo);
        }

        private static void WriteDetalle(string detalle, string motivo)
        {
            string extra = ExtraerDetalle(detalle, motivo);
            if (!string.IsNullOrWhiteSpace(extra))
                Utilities.Wlog("  Detalle: " + extra, 0);
        }

        private static string ExtraerDetalle(string message, string motivo)
        {
            if (string.IsNullOrWhiteSpace(message))
                return null;
            string detalle = Limpiar(message);
            if (string.IsNullOrWhiteSpace(detalle))
                return null;
            if (!string.IsNullOrWhiteSpace(motivo)
                && (string.Equals(detalle, motivo, StringComparison.OrdinalIgnoreCase)
                    || detalle.StartsWith(motivo, StringComparison.OrdinalIgnoreCase)
                    || motivo.StartsWith(detalle, StringComparison.OrdinalIgnoreCase)))
                return null;
            if (detalle.Length <= 300)
                return detalle;
            return detalle.Substring(0, 300) + "...";
        }

        private static string Limpiar(string text)
        {
            var sb = new StringBuilder(text.Length);
            bool inTag = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '<')
                {
                    inTag = true;
                    continue;
                }
                if (c == '>')
                {
                    inTag = false;
                    sb.Append(' ');
                    continue;
                }
                if (!inTag)
                    sb.Append(c);
            }

            string limpio = sb.ToString().Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
            while (limpio.IndexOf("  ", StringComparison.Ordinal) >= 0)
                limpio = limpio.Replace("  ", " ");
            return limpio.Trim();
        }
    }
}
