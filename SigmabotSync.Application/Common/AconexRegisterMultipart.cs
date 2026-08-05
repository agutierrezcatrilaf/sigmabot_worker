using System;
using System.Collections.Generic;
using System.Text;

namespace SigmabotSync.Application.Common
{
    /// <summary>
    /// multipart/mixed para Register Document y Supersede (misma forma que la guía Aconex y ejemplos curl).
    /// El boundary es solo un nombre de separador: en el cuerpo aparece como <c>--{nombre}</c> entre partes y <c>--{nombre}--</c> al final
    /// (en el curl, si el nombre es <c>myboundary</c>, verás <c>--myboundary</c> y <c>--myboundary--</c>).
    /// </summary>
    public static class AconexRegisterMultipart
    {
        /// <summary>
        /// Mismo valor que en el curl de ejemplo: <c>boundary="myboundary"</c>. Es válido usar siempre este string.
        /// </summary>
        public const string ExampleBoundary = "myboundary";

        /// <summary>
        /// Opcional: otro nombre de boundary en cada petición (<c>sigmabot_</c> + GUID) para casi eliminar la posibilidad de que
        /// esa misma cadena aparezca dentro del XML o del PDF (colisión con el parser). Aconex no lo exige; <see cref="ExampleBoundary"/> suele bastar.
        /// </summary>
        public static string CreateBoundary()
        {
            return "sigmabot_" + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Construye el body multipart/mixed: parte 1 = XML Document, parte 2 = X-Filename + base64 del archivo.
        /// </summary>
        public static string BuildRegisterBody(string xmlDocument, string fileName, string fileBase64, string boundary)
        {
            if (string.IsNullOrEmpty(boundary))
                throw new ArgumentException("boundary requerido.", nameof(boundary));

            var sb = new StringBuilder();
            sb.Append("--").Append(boundary).Append("\r\n\r\n");
            sb.Append(xmlDocument ?? "").Append("\r\n");
            sb.Append("--").Append(boundary).Append("\r\n");
            sb.Append("X-Filename: ").Append(fileName ?? "document").Append("\r\n\r\n");
            sb.Append(fileBase64 ?? "").Append("\r\n\r\n");
            sb.Append("--").Append(boundary).Append("--");
            return sb.ToString();
        }

        /// <summary>Multipart solo con XML (p. ej. Register con <c>HasFile=false</c>, marcador de posición).</summary>
        public static string BuildRegisterBodyXmlOnly(string xmlDocument, string boundary)
        {
            if (string.IsNullOrEmpty(boundary))
                throw new ArgumentException("boundary requerido.", nameof(boundary));

            var sb = new StringBuilder();
            sb.Append("--").Append(boundary).Append("\r\n\r\n");
            sb.Append(xmlDocument ?? "").Append("\r\n");
            sb.Append("--").Append(boundary).Append("--");
            return sb.ToString();
        }

        /// <summary>
        /// curl equivalente para depuración (copiar a bash; el body va en heredoc).
        /// No incluye el secret: usa el mismo Base64 que ya va en Authorization.
        /// </summary>
        public static string FormatCurlEquivalent(
            string httpMethod,
            string requestUrl,
            string authorizationHeaderBase64,
            string integrationIdOrNull,
            string multipartBody,
            string boundary)
        {
            var sb = new StringBuilder();
            sb.Append("curl -X ").Append(httpMethod ?? "POST").Append(" '").Append(requestUrl ?? "").Append("' \\");
            sb.Append("\n  -H 'Authorization: Basic ").Append(authorizationHeaderBase64 ?? "").Append("' \\");
            if (!string.IsNullOrWhiteSpace(integrationIdOrNull))
                sb.Append("\n  -H 'X-Application-Key: ").Append(integrationIdOrNull.Trim()).Append("' \\");
            sb.Append("\n  -H 'Content-Type: multipart/mixed; boundary=\"").Append(boundary ?? "").Append("\"' \\");
            sb.Append("\n  --data-binary @- <<'SIGMABOT_MULTIPART_EOF'");
            sb.Append("\n").Append(multipartBody ?? "");
            sb.Append("\nSIGMABOT_MULTIPART_EOF");
            return sb.ToString();
        }

        /// <summary>
        /// Detecta UTF-8 interpretado como Latin-1 en NVARCHAR (ej. ó = U+00C3 U+00B3 en lugar de U+00F3).
        /// </summary>
        public static string DescribeMojibakeSequences(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            var issues = new List<string>();
            for (int i = 0; i < text.Length - 1; i++)
            {
                if (text[i] == '\u00C3' && text[i + 1] >= '\u0080' && text[i + 1] <= '\u00BF')
                {
                    issues.Add($"pos{i}: U+00C3+U+{((int)text[i + 1]).ToString("X4", System.Globalization.CultureInfo.InvariantCulture)}");
                    i++;
                }
            }

            return issues.Count == 0 ? null : string.Join(", ", issues);
        }
    }
}
