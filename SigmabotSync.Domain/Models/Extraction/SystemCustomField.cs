using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SigmabotSync.Domain.Models.Extraction
{
    public class SystemCustomField
    {
        public string id { get; set; }
        public string label { get; set; }
        public object type { get; set; }
        public string family_id { get; set; }
        public bool mandatory { get; set; }
        public bool is_enabled { get; set; }
    }

    public class SystemCustomFields
    {
        public List<SystemCustomField> custom_fields { get; set; }
    }
}
