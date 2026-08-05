using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using SigmabotSync.Domain.Ports;

namespace SigmabotSync.Infrastructure.External
{
    public sealed class AconexHttpGetAdapter : IAconexHttpGetPort, IDisposable
    {
        private readonly HttpClient _httpClient;

        public AconexHttpGetAdapter()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10)
            };
        }

        public async Task<string> GetStringAsync(AconexHttpGetRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Url))
                throw new ArgumentException("Url requerida.", nameof(request));

            using (var msg = new HttpRequestMessage(HttpMethod.Get, request.Url))
            {
                msg.Headers.Authorization = new AuthenticationHeaderValue("Basic", request.AuthorizationHeaderBase64);

                if (!string.IsNullOrWhiteSpace(request.Accept))
                    msg.Headers.TryAddWithoutValidation("Accept", request.Accept);
                if (!string.IsNullOrWhiteSpace(request.ContentType))
                    msg.Headers.TryAddWithoutValidation("Content-Type", request.ContentType);

                if (request.ExtraHeaders != null)
                {
                    foreach (var (name, value) in request.ExtraHeaders)
                    {
                        if (!string.IsNullOrEmpty(name))
                            msg.Headers.TryAddWithoutValidation(name, value);
                    }
                }

                using (var response = await _httpClient.SendAsync(msg, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    string text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return text.Replace("\u0003", "");
                }
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
