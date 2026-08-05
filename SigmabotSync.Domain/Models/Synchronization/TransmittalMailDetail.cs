using System.Collections.Generic;

namespace SigmabotSync.Domain.Models.Synchronization
{
    public sealed class TransmittalMailDetail
    {
        public string MailId { get; set; }
        public string MailNo { get; set; }
        public string Subject { get; set; }
        public string InRefToMailId { get; set; }
        public string ThreadId { get; set; }
        public IList<TransmittalDocumentAttachment> Attachments { get; set; } = new List<TransmittalDocumentAttachment>();
    }
}
