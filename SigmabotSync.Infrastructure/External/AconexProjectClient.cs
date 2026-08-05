using SigmabotSync.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SigmabotSync.Infrastructure.External
{
    public class AconexProjectClient : AconexClientBase
    {
        public AconexProjectClient(string u, string p, string id) : base(u, p, id) { }

        public async Task<List<Project>> GetUserProjectsAsync()
        {
            using (var client = CreateClient())
            {
                var response = await client.GetAsync("https://us1.aconex.com/api/projects");
                response.EnsureSuccessStatusCode();

                var xml = await response.Content.ReadAsStringAsync();

                return ParseProjects(xml);
            }
        }

        private List<Project> ParseProjects(string xml)
        {
            var result = new List<Project>();
            var doc = XDocument.Parse(xml);

            var items = doc
                .Root                       // ProjectResults
                ?.Element("SearchResults")  // SearchResults
                ?.Elements("Project");      // Cada <Project>...</Project>

            if (items == null)
                return result;

            foreach (var prj in items)
            {
                result.Add(new Project
                {
                    Id = (string)prj.Element("ProjectId"),
                    Description = (string)prj.Element("ProjectShortName")
                });
            }

            return result;
        }    //}
    }

}
