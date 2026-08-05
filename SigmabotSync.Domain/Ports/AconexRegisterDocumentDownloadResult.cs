namespace SigmabotSync.Domain.Ports
{
    public enum AconexRegisterDocumentDownloadStatus
    {
        Saved,
        OmittedEmptyDocument,
        Error
    }

    /// <summary>Resultado de GET .../register/{documentId} (contenido del archivo).</summary>
    public sealed class AconexRegisterDocumentDownloadResult
    {
        public AconexRegisterDocumentDownloadStatus Status { get; init; }
        public string Message { get; init; }
    }
}
