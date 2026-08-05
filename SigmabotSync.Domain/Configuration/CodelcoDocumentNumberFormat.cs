using System;

namespace SigmabotSync.Domain.Configuration
{
    /// <summary>Formato docno Codelco: segmentos WBS, tipo especialidad y correlativo.</summary>
    public static class CodelcoDocumentNumberFormat
    {
        public static bool TryParse(
            string documentNumber,
            out string wbsSegment,
            out string tipoEspSegment,
            out string correlativo)
        {
            wbsSegment = null;
            tipoEspSegment = null;
            correlativo = null;
            if (string.IsNullOrWhiteSpace(documentNumber))
                return false;

            string[] parts = documentNumber.Trim().Split('-');
            if (parts.Length < 4)
                return false;

            wbsSegment = parts[1].Trim();
            tipoEspSegment = parts[2].Trim();
            correlativo = parts[parts.Length - 1].Trim();
            return !string.IsNullOrEmpty(wbsSegment)
                && !string.IsNullOrEmpty(tipoEspSegment)
                && !string.IsNullOrEmpty(correlativo);
        }
    }
}
