using SigmabotSync.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SigmabotSync.Infrastructure.External
{
    public class AconexUserClient : AconexClientBase
    {
        public AconexUserClient(string u, string p, string id) : base(u, p, id) { }

        public async Task<UserInfo> GetCurrentUserAsync()
        {
            using (var client = CreateClient())
            {
                var response = await client.GetAsync("https://us1.aconex.com/api/user");

                response.EnsureSuccessStatusCode();
                var xml = await response.Content.ReadAsStringAsync();
                return ParseUser(xml);
            }
        }

        public async Task<bool> ValidateConnectionAsync()
        {
            using (var client = CreateClient())
            {
                var response = await client.GetAsync("https://us1.aconex.com/api/user");
                return response.IsSuccessStatusCode;  // 200 = credenciales válidas
            }
        }


        private UserInfo ParseUser(string xml)
        {
            var doc = XDocument.Parse(xml);
            var root = doc.Root;

            return new UserInfo
            {
                UserId = (string)root.Element("userId"),
                FirstName = (string)root.Element("firstName"),
                LastName = (string)root.Element("lastName"),
                OrganizationName = (string)root.Element("organizationName"),
                OrganizationId = (string)root.Element("organizationId")
            };
        }


    }

}
