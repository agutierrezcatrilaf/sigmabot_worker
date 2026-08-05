namespace SigmabotSync.Domain.Models.Synchronization
{
    public sealed class TransmittalDocumentAttachment
    {
        public string AttachmentId { get; set; }
        public string DocumentId { get; set; }
        public string RegisteredAs { get; set; }
        public string DocumentNo { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string Revision { get; set; }
        public string VersionNumber { get; set; }
        public string RevisionDate { get; set; }
        public string Status { get; set; }
        public string Title { get; set; }

        public bool IsPlaceholder => FileSize <= 0;
    }
}
