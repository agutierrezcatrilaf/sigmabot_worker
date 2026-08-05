using System;
using System.Text.RegularExpressions;

namespace SigmabotSync.Domain.Configuration
{
    /// <summary>
    /// Ida Codelco → SALFA: deriva el nombre de tipo Aconex (DocumentTypeId) desde TipoDeDocumento_singleSelect.
    /// Prefijo numérico → Plano Externo; prefijo con letras → Documento Externo.
    /// </summary>
    public static class ProjectSyncDocumentTypeResolver
    {
        public const string DocumentoExterno = "Documento Externo";
        public const string PlanoExterno = "Plano Externo";

        private static readonly Regex CodePrefixRegex = new Regex(
            @"^([A-Za-z0-9]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>Código al inicio del texto Aconex (ej. 502CA, ESPEL, 201OH).</summary>
        public static string ExtractTipoDocumentoCodePrefix(string tipoDeDocumentoText)
        {
            if (string.IsNullOrWhiteSpace(tipoDeDocumentoText))
                return null;

            Match match = CodePrefixRegex.Match(tipoDeDocumentoText.Trim());
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>Nombre en TiposDocumentos SALFA según el prefijo del código Codelco.</summary>
        public static string ResolveSalfaDocumentTypeName(string tipoDeDocumentoText)
        {
            string code = ExtractTipoDocumentoCodePrefix(tipoDeDocumentoText);
            if (string.IsNullOrEmpty(code))
                return null;

            return char.IsDigit(code[0]) ? PlanoExterno : DocumentoExterno;
        }
    }
}
