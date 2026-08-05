using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SigmabotSync.Domain.Models
{
    public class UserInfo
    {
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string OrganizationName { get; set; }
        public string OrganizationId { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }

}
