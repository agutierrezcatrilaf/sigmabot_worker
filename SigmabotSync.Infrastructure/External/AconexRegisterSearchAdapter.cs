using System.Globalization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SigmabotSync.Domain.Models.Extraction;
using SigmabotSync.Domain.Ports;

namespace SigmabotSync.Infrastructure.External
{
    /// <summary>POST .../register/search con Basic únicamente (sin X-Application-Key).</summary>
    public sealed class AconexRegisterSearchAdapter : IAconexRegisterSearchPort, IDisposable
    {
        private readonly HttpClient _httpClient;

        public AconexRegisterSearchAdapter()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10)
            };
        }

        public async Task<AconexRegisterSearchResult> SearchRegisterPageAsync(
            string baseUrl,
            string projectId,
            string orgId,
            string userId,
            string authorizationHeaderBase64,
            IReadOnlyList<string> returnFields,
            int resultSize,
            int pageNumber,
            bool throwIfNotSuccess = true,
            CancellationToken cancellationToken = default,
            string searchQuery = null,
            string filterDocumentNo = null,
            string filterRevision = null,
            string filterVersionNumber = null)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("baseUrl requerido.", nameof(baseUrl));
            if (string.IsNullOrWhiteSpace(projectId))
                throw new ArgumentException("projectId requerido.", nameof(projectId));
            if (resultSize <= 0)
                resultSize = 25;
            if (pageNumber <= 0)
                pageNumber = 1;

            string root = baseUrl.TrimEnd('/');
            string uri = $"{root}/api/projects/{projectId}/register/search";

            bool filterByDocNo = !string.IsNullOrWhiteSpace(filterDocumentNo);
            var body = new Dictionary<string, object>
            {
                ["orgId"] = orgId,
                ["userId"] = userId,
                ["returnFields"] = returnFields?.ToList() ?? new List<string>(),
                ["resultSize"] = resultSize.ToString(CultureInfo.InvariantCulture),
                ["showDocHistory"] = filterByDocNo ? "false" : "true",
                ["pageNumber"] = pageNumber.ToString(CultureInfo.InvariantCulture)
            };
            if (filterByDocNo)
            {
                body["docno"] = filterDocumentNo.Trim();
                if (!string.IsNullOrWhiteSpace(filterVersionNumber))
                    body["versionnumber"] = filterVersionNumber.Trim();
                else if (!string.IsNullOrWhiteSpace(filterRevision))
                    body["revision"] = filterRevision.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                body["searchQuery"] = searchQuery.Trim();
            }

            string jsonBody = JsonConvert.SerializeObject(body);
            using (var request = new HttpRequestMessage(HttpMethod.Post, uri))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authorizationHeaderBase64);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                using (var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    responseString = responseString?.Replace("\u0003", "") ?? "";

                    if (!response.IsSuccessStatusCode)
                    {
                        if (!throwIfNotSuccess)
                            return AconexRegisterSearchResult.Failure((int)response.StatusCode, responseString, requestBody: jsonBody);
                        response.EnsureSuccessStatusCode();
                    }

                    if (TryParseAconexError(responseString, out string errorCode, out string errorDescription))
                    {
                        if (!throwIfNotSuccess)
                        {
                            return AconexRegisterSearchResult.Failure(
                                (int)response.StatusCode,
                                responseString,
                                errorCode,
                                errorDescription,
                                jsonBody);
                        }

                        throw new InvalidOperationException(
                            $"Aconex register/search ({errorCode}): {errorDescription}");
                    }

                    var page = JsonConvert.DeserializeObject<Rootobject>(responseString);
                    return AconexRegisterSearchResult.Success(page, (int)response.StatusCode, responseString, jsonBody);
                }
            }
        }

        private static bool TryParseAconexError(string responseString, out string errorCode, out string errorDescription)
        {
            errorCode = null;
            errorDescription = null;
            if (string.IsNullOrWhiteSpace(responseString))
                return false;

            try
            {
                JObject json = JObject.Parse(responseString);
                errorCode = json["errorCode"]?.ToString();
                if (string.IsNullOrWhiteSpace(errorCode))
                    return false;
                errorDescription = json["errorDescription"]?.ToString();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
