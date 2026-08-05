namespace SigmabotSync.Domain.Configuration
{
    /// <summary>
    /// Convención oficial para trabajos <see cref="TipoTrabajoIds.FileUploadWithMetadata"/> (DataLake).
    /// </summary>
    public static class FileUploadWithMetadataDefaults
    {
        public const string TablaMetadata = "DocumentosMetadata";
        public const string TablaPaths = "DocumentosPath";

        public static string ResolverTablaMetadata(string configurado)
        {
            return string.IsNullOrWhiteSpace(configurado) ? TablaMetadata : configurado.Trim();
        }

        public static string ResolverTablaPaths(string configurado)
        {
            return string.IsNullOrWhiteSpace(configurado) ? TablaPaths : configurado.Trim();
        }
    }
}
