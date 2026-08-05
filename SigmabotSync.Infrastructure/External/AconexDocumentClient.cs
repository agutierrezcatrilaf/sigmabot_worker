using Newtonsoft.Json;
using SigmabotSync.Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SigmabotSync.Infrastructure.External
{
    public class AconexDocumentClient : AconexClientBase
    {
        public AconexDocumentClient(string u, string p, string id) : base(u, p, id) { }

        public async Task<List<DocumentIntegrityInfo>> GetChangedDocumentsAsync(string projectId, DateTime since)
        {
            string url =
                $"https://us1.aconex.com/api/projects/{projectId}/register/integrity" +
                $"?everythingsince={since:yyyy-MM-ddTHH:mm:ss.fffZ}&show_document_history=true";

            using (var client = CreateClient())
            {
                client.DefaultRequestHeaders.Accept.ParseAdd("application/xml");

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string xml = await response.Content.ReadAsStringAsync();
                return ParseIntegrityResponse(xml);
            }
        }

        public async Task<DocumentMetadata> GetDocumentMetadataAsync(string projectId, string documentId)
        {
            string url = $"https://us1.aconex.com/api/projects/{projectId}/register/{documentId}/metadata";

            using (var client = CreateClient())
            {
                client.DefaultRequestHeaders.Accept.ParseAdd("application/xml");

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string xml = await response.Content.ReadAsStringAsync();

                return ParseMetadataXml(xml);
            }
        }

        private List<DocumentIntegrityInfo> ParseIntegrityResponse(string xml)
        {
            var result = new List<DocumentIntegrityInfo>();
            var doc = XDocument.Parse(xml);

            var items = doc.Root?.Elements("Document");
            if (items == null) return result;

            foreach (var x in items)
            {
                result.Add(new DocumentIntegrityInfo
                {
                    Id = (string)x.Attribute("id"),
                    LastModifiedDate = DateTime.Parse((string)x.Attribute("lastModifiedDate")),
                    LastEventDate = DateTime.Parse((string)x.Attribute("lastEventDate"))
                });
            }

            return result;
        }

        private DocumentMetadata ParseMetadataXml(string xml)
        {
            var serializer = new XmlSerializer(typeof(DocumentMetadata));

            using (var reader = new StringReader(xml))
            {
                try
                {
                    return (DocumentMetadata)serializer.Deserialize(reader);
                }
                catch (Exception)
                {
                    Console.WriteLine("Error al procesar metadata:");
                    Console.WriteLine(xml);
                    throw;
                }
            }
        }


    }

}
