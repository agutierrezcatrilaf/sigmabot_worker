using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SigmabotSync.Domain.Models.Extraction
{
    public class CorreoDto
    {
        public string ACXProjectId { get; set; }
        public string MailId { get; set; }
        public string MailNo { get; set; }
        public string FromOrganizationName { get; set; }
        public string FromName { get; set; }
        public string ReasonForIssue { get; set; }
        public string ReferenceNumber { get; set; }
        public string SentDate { get; set; }
        public string Subject { get; set; }
        public string CorrespondenceType { get; set; }
    }

    public class DocumentoDto
    {
        public string ACXProjectId { get; set; }
        public string MailId { get; set; }
        public string DocumentNo { get; set; }
        public string FileName { get; set; }
        public string FileSize { get; set; }
        public string Revision { get; set; }
        public string RevisionDate { get; set; }
        public string Title { get; set; }
    }

    public class DestinatarioDto
    {
        public string ACXProjectId { get; set; }
        public string MailId { get; set; }
        public string MailNo { get; set; }
        public string ACXUserId { get; set; }
        public string UserName { get; set; }
        public string Organization { get; set; }
    }

    public class ControlProcesoCorreos
    {
        public string ProjId { get; set; }
        public string Mailbox { get; set; }
        public DateTime UltimaFechaProcesada { get; set; }
        public int RangoDias { get; set; }
        public int TotalDescargados { get; set; }
        public int TotalProcesados { get; set; }
    }
}
