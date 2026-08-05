using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SigmabotSync.Domain.Models
{
    public class DocumentIntegrityInfo
    {
        public string Id { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public DateTime LastEventDate { get; set; }
    }
}
