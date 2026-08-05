using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SigmabotSync.Domain.Models;
using SigmabotSync.Domain.Ports;

namespace SigmabotSync.Infrastructure.External
{
    public sealed class AconexRegisterMetadataAdapter : IAconexRegisterMetadataPort, IDisposable
    {
        private readonly HttpClient _httpClient;

        public AconexRegisterMetadataAdapter()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        }

        public async Task<DocumentMetadata> GetRegisterMetadataAsync(
            string baseUrl,
            string projectId,
            string documentId,
            string authorizationHeaderBase64,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(documentId))
                return null;

            string root = (baseUrl ?? "https://us1.aconex.com").TrimEnd('/');
            string uri = $"{root}/api/projects/{projectId}/register/{documentId.Trim()}/metadata";

            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authorizationHeaderBase64);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                using (var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                        return null;

                    string xml = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(xml))
                        return null;

                    return DeserializeMetadata(xml);
                }
            }
        }

        private static DocumentMetadata DeserializeMetadata(string xml)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(DocumentMetadata));
                using (var reader = new StringReader(xml.Replace("\u0003", "")))
                    return (DocumentMetadata)serializer.Deserialize(reader);
            }
            catch
            {
                return null;
            }
        }

        public void Dispose() => _httpClient?.Dispose();
    }
}
