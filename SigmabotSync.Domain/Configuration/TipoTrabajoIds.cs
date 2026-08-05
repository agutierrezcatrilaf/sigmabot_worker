namespace SigmabotSync.Domain.Configuration
{
    /// <summary>
    /// Valores del campo <c>Tipo</c> en la tabla <c>Trabajos</c>. Deben coincidir con el consola.
    /// </summary>
    public static class TipoTrabajoIds
    {
        public const string FileExtraction = "FileExtraction";
        public const string ProjectSync = "ProjectSync";
        public const string FullExtraction = "FullExtraction";
        public const string FileUploadWithMetadata = "FileUploadWithMetadata";
    }
}
