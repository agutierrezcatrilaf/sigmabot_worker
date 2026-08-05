using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using SigmabotSync.Domain.Ports;

namespace SigmabotSync.Infrastructure.External
{
    /// <summary>GET .../register/{documentId} con descompresión; solo Basic.</summary>
    public sealed class AconexRegisterDocumentContentAdapter : IAconexRegisterDocumentContentPort, IDisposable
    {
        private readonly HttpClient _httpClient;

        public AconexRegisterDocumentContentAdapter()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(10)
            };
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, sdch");
        }

        public async Task<AconexRegisterDocumentDownloadResult> DownloadToFileAsync(
            string baseUrl,
            string projectId,
            string documentId,
            string filePath,
            string authorizationHeaderBase64,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("baseUrl requerido.", nameof(baseUrl));
            if (string.IsNullOrWhiteSpace(projectId))
                throw new ArgumentException("projectId requerido.", nameof(projectId));
            if (string.IsNullOrWhiteSpace(documentId))
                throw new ArgumentException("documentId requerido.", nameof(documentId));

            string root = baseUrl.TrimEnd('/');
            string url = $"{root}/api/projects/{projectId}/register/{documentId}";

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authorizationHeaderBase64);

                    using (var response = await _httpClient
                               .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                               .ConfigureAwait(false))
                    {
                        if (response.StatusCode == HttpStatusCode.BadRequest)
                        {
                            string errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            if (errorBody != null &&
                                errorBody.IndexOf("CANNOT_DOWNLOAD_EMPTY_DOCUMENT", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return new AconexRegisterDocumentDownloadResult
                                {
                                    Status = AconexRegisterDocumentDownloadStatus.OmittedEmptyDocument
                                };
                            }
                        }

                        response.EnsureSuccessStatusCode();

                        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await response.Content.CopyToAsync(fileStream).ConfigureAwait(false);
                        }

                        return new AconexRegisterDocumentDownloadResult
                        {
                            Status = AconexRegisterDocumentDownloadStatus.Saved
                        };
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return new AconexRegisterDocumentDownloadResult
                {
                    Status = AconexRegisterDocumentDownloadStatus.Error,
                    Message = "Timeout o cancelación."
                };
            }
            catch (Exception ex)
            {
                return new AconexRegisterDocumentDownloadResult
                {
                    Status = AconexRegisterDocumentDownloadStatus.Error,
                    Message = ex.Message
                };
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
