using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SigmabotSync.Domain.Models
{
    [XmlRoot("RegisterDocument")]
    public class DocumentMetadata
    {
        [XmlAttribute("DocumentId")]
        public string DocumentId { get; set; }

        public Attribute1 Attribute1 { get; set; }

        public string Author { get; set; }
        public string Category { get; set; }
        public string Comments { get; set; }
        public string Confidential { get; set; }
        public string ConfidentialUserAccessList { get; set; }
        public string ContractDeliverable { get; set; }
        public string ContractNumber { get; set; }
        public string ContractorDocumentNumber { get; set; }

        public string Date1 { get; set; }
        public string Date2 { get; set; }
        public string DateForReview { get; set; }
        public string DateModified { get; set; }

        public string Discipline { get; set; }
        public string DocumentNumber { get; set; }
        public string DocumentStatus { get; set; }
        public string DocumentType { get; set; }

        public string FileSize { get; set; }
        public string FileType { get; set; }
        public string Filename { get; set; }

        public string MilestoneDate { get; set; }
        public string PlannedSubmissionDate { get; set; }

        public string ProjectField1 { get; set; }
        public string ProjectField2 { get; set; }

        public string ReviewSource { get; set; }
        public string ReviewStatus { get; set; }

        public string Revision { get; set; }
        public string RevisionDate { get; set; }

        public string SelectList1 { get; set; }
        public string SelectList2 { get; set; }
        public string SelectList3 { get; set; }

        public string Title { get; set; }
    }

    public class Attribute1
    {
        public string AttributeTypeNames { get; set; }
        public string AttributeType { get; set; }
    }

}
