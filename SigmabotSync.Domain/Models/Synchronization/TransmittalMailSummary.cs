using System;

namespace SigmabotSync.Domain.Models.Synchronization
{
    public sealed class TransmittalMailSummary
    {
        public string MailId { get; set; }
        public string MailNo { get; set; }
        public string Subject { get; set; }
        public string ReferenceNumber { get; set; }
        public DateTime? SentDate { get; set; }
        public string FromOrganizationName { get; set; }
        public string FromUserName { get; set; }
    }
}
