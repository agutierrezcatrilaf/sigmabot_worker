using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SigmabotSync.Domain.Models.Extraction
{
    public class Area
    {
        public string id { get; set; }
        public string name { get; set; }
        public List<Area> children { get; set; }
    }

    public class Areas
    {
        public List<Area> areas { get; set; }
    }
}
