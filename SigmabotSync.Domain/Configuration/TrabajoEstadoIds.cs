namespace SigmabotSync.Domain.Configuration
{
    /// <summary>Valores de <c>Trabajos.Estado</c> gestionados por el configurador.</summary>
    public static class TrabajoEstadoIds
    {
        /// <summary>El scheduler y la carga de config consideran solo trabajos en este estado.</summary>
        public const string Activo = "Activo";

        /// <summary>No programar ni ejecutar como trabajo operativo.</summary>
        public const string Desactivado = "Desactivado";

        /// <summary>Borrador / configuración incompleta (compatibilidad).</summary>
        public const string Pendiente = "Pendiente";
    }
}
