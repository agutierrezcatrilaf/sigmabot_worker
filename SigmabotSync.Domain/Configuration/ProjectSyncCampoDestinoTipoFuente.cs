namespace SigmabotSync.Domain.Configuration
{
    /// <summary>Tipo de origen del valor en la matriz <c>TransmittalSyncCampoDestino</c>.</summary>
    public static class ProjectSyncCampoDestinoTipoFuente
    {
        public const string CampoOrigen = "CampoOrigen";
        public const string ReglaDocumentTypeFromTipo = "ReglaDocumentTypeFromTipo";
        /// <summary>Legacy: statusid desde parámetro. Preferir Adjunto/Status + allowlist IdEstatusDocumentoDestino.</summary>
        public const string ParametroIdEstatusDestino = "ParametroIdEstatusDestino";
        /// <summary>Valor del adjunto (Revision, MailNo, Status, …).</summary>
        public const string Adjunto = "Adjunto";
        public const string Constante = "Constante";
        public const string SoloPreservar = "SoloPreservar";
    }
}
