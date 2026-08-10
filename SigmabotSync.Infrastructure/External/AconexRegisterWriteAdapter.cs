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
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan LargeUploadTimeout = TimeSpan.FromMinutes(60);

        private readonly HttpClient _httpClient;

        public AconexRegisterWriteAdapter()
        {
            _httpClient = new HttpClient
            {
                Timeout = DefaultTimeout
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
            return PostMultipartStringAsync(url, authorizationHeaderBase64, integrationIdOrNull, multipartBody, boundary, DefaultTimeout, cancellationToken);
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
            return PostMultipartStringAsync(url, authorizationHeaderBase64, integrationIdOrNull, multipartBody, boundary, DefaultTimeout, cancellationToken);
        }

        public Task<AconexRawHttpResponse> PostRegisterDocumentWithFileAsync(
            string baseUrl,
            string projectId,
            string authorizationHeaderBase64,
            string integrationIdOrNull,
            string xmlDocument,
            string filePath,
            string fileName,
            string boundary,
            CancellationToken cancellationToken = default)
        {
            string root = string.IsNullOrWhiteSpace(baseUrl) ? "https://us1.aconex.com" : baseUrl.TrimEnd('/');
            string url = $"{root}/api/projects/{projectId}/register";
            return PostMultipartFileAsync(
                url, authorizationHeaderBase64, integrationIdOrNull, xmlDocument, filePath, fileName, boundary, cancellationToken);
        }

        public Task<AconexRawHttpResponse> PostSupersedeDocumentWithFileAsync(
            string baseUrl,
            string projectId,
            string documentId,
            string authorizationHeaderBase64,
            string integrationIdOrNull,
            string xmlDocument,
            string filePath,
            string fileName,
            string boundary,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(documentId))
                throw new ArgumentException("documentId requerido.", nameof(documentId));

            string root = string.IsNullOrWhiteSpace(baseUrl) ? "https://us1.aconex.com" : baseUrl.TrimEnd('/');
            string url = $"{root}/api/projects/{projectId}/register/{documentId}/supersede";
            return PostMultipartFileAsync(
                url, authorizationHeaderBase64, integrationIdOrNull, xmlDocument, filePath, fileName, boundary, cancellationToken);
        }

        private async Task<AconexRawHttpResponse> PostMultipartStringAsync(
            string requestUrl,
            string authorizationHeaderBase64,
            string integrationIdOrNull,
            string multipartBody,
            string boundary,
            TimeSpan timeout,
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

                return await SendMultipartAsync(request, timeout, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<AconexRawHttpResponse> PostMultipartFileAsync(
            string requestUrl,
            string authorizationHeaderBase64,
            string integrationIdOrNull,
            string xmlDocument,
            string filePath,
            string fileName,
            string boundary,
            CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, requestUrl))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authorizationHeaderBase64);
                if (!string.IsNullOrEmpty(integrationIdOrNull))
                    request.Headers.TryAddWithoutValidation("X-Application-Key", integrationIdOrNull);

                request.Content = new AconexRegisterMultipartMixedContent(boundary, xmlDocument, fileName, filePath);

                return await SendMultipartAsync(request, LargeUploadTimeout, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<AconexRawHttpResponse> SendMultipartAsync(
            HttpRequestMessage request,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeoutCts.CancelAfter(timeout);
                using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token).ConfigureAwait(false))
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
