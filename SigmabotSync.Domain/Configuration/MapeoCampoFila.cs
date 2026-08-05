namespace SigmabotSync.Domain.Configuration
{
    /// <summary>Una fila del mapeo documentos: API consulta, propiedad JSON, columna BD.</summary>
    public sealed class MapeoCampoFila
    {
        public string Api { get; set; } = string.Empty;
        public string Json { get; set; } = string.Empty;
        public string Bd { get; set; } = string.Empty;
    }
}
