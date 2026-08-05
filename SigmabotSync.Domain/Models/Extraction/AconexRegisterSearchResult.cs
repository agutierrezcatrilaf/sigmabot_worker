namespace SigmabotSync.Domain.Models.Extraction
{
    /// <summary>Resultado de POST register/search (incluye error HTTP cuando no se lanza excepción).</summary>
    public sealed class AconexRegisterSearchResult
    {
        public Rootobject Page { get; init; }
        public int StatusCode { get; init; }
        public string ResponseBody { get; init; }
        public string RequestBody { get; init; }
        public bool IsHttpSuccess { get; init; }
        public bool HasAconexError => !string.IsNullOrWhiteSpace(AconexErrorCode);
        public string AconexErrorCode { get; init; }
        public string AconexErrorDescription { get; init; }

        public static AconexRegisterSearchResult Success(Rootobject page, int statusCode, string responseBody, string requestBody = null) =>
            new AconexRegisterSearchResult
            {
                Page = page,
                StatusCode = statusCode,
                ResponseBody = responseBody,
                RequestBody = requestBody,
                IsHttpSuccess = true
            };

        public static AconexRegisterSearchResult Failure(
            int statusCode,
            string responseBody,
            string errorCode = null,
            string errorDescription = null,
            string requestBody = null) =>
            new AconexRegisterSearchResult
            {
                Page = null,
                StatusCode = statusCode,
                ResponseBody = responseBody,
                RequestBody = requestBody,
                IsHttpSuccess = false,
                AconexErrorCode = errorCode,
                AconexErrorDescription = errorDescription
            };
    }
}
