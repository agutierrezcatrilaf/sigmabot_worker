using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SigmabotSync.Domain.Ports;

namespace SigmabotSync.Infrastructure.External
{
    public sealed class AconexRegisterWriteAdapter : IAconexRegisterWritePort, IDisposable
    {
        private readonly HttpClient _httpClient;

        public AconexRegisterWriteAdapter()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10)
            };
        }

        public async Task<string> GetRegisterSchemaXmlAsync(
            string baseUrl,
            string projectId,
            string authorizationHeaderBase64,
            string integrationIdOrNull,
            CancellationToken cancellationToken = default)
        {
            string root = string.IsNullOrWhiteSpace(baseUrl) ? "https://us1.aconex.com" : baseUrl.TrimEnd('/');
            string schemaUrl = $"{root}/api/projects/{projectId}/register/schema";

            using (var request = new HttpRequestMessage(HttpMethod.Get, schemaUrl))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authorizationHeaderBase64);
                if (!string.IsNullOrEmpty(integrationIdOrNull))
                    request.Headers.TryAddWithoutValidation("X-Application-Key", integrationIdOrNull);

                using (var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    string responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                        throw new InvalidOperationException($"Aconex register/schema falló: {response.StatusCode}. {responseText}");
                    return responseText;
                }
            }
        }

        public Task<AconexRawHttpResponse> PostRegisterDocumentAsync(
            string baseUrl,
            string projectId,
            string authorizationHeaderBase64,
            string integrationIdOrNull,
            string multipartBody,
            string boundary,
            CancellationToken cancellationToken = default)
        {
            string root = string.IsNullOrWhiteSpace(baseUrl) ? "https://us1.aconex.com" : baseUrl.TrimEnd('/');
            string url = $"{root}/api/projects/{projectId}/register";
            return PostMultipartMixedAsync(url, authorizationHeaderBase64, integrationIdOrNull, multipartBody, boundary, cancellationToken);
        }

        public Task<AconexRawHttpResponse> PostSupersedeDocumentAsync(
            string baseUrl,
            string projectId,
            string documentId,
            string authorizationHeaderBase64,
            string integrationIdOrNull,
            string multipartBody,
            string boundary,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(documentId))
                throw new ArgumentException("documentId requerido.", nameof(documentId));

            string root = string.IsNullOrWhiteSpace(baseUrl) ? "https://us1.aconex.com" : baseUrl.TrimEnd('/');
            string url = $"{root}/api/projects/{projectId}/register/{documentId}/supersede";
            return PostMultipartMixedAsync(url, authorizationHeaderBase64, integrationIdOrNull, multipartBody, boundary, cancellationToken);
        }

        private async Task<AconexRawHttpResponse> PostMultipartMixedAsync(
            string requestUrl,
            string authorizationHeaderBase64,
            string integrationIdOrNull,
            string multipartBody,
            string boundary,
            CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, requestUrl))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authorizationHeaderBase64);
                if (!string.IsNullOrEmpty(integrationIdOrNull))
                    request.Headers.TryAddWithoutValidation("X-Application-Key", integrationIdOrNull);

                var content = new StringContent(multipartBody, Encoding.UTF8);
                content.Headers.ContentType = new MediaTypeHeaderValue("multipart/mixed");
                content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("boundary", "\"" + boundary + "\""));
                request.Content = content;

                using (var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return new AconexRawHttpResponse
                    {
                        StatusCode = (int)response.StatusCode,
                        Body = body
                    };
                }
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
