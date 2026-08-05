namespace SigmabotSync.Domain.Ports
{
    /// <summary>Respuesta HTTP genérica con cuerpo en texto (XML/JSON).</summary>
    public sealed class AconexRawHttpResponse
    {
        public int StatusCode { get; init; }
        public string Body { get; init; }
        public bool IsSuccessStatusCode => StatusCode >= 200 && StatusCode <= 299;
    }
}
