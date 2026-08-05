namespace SigmabotSync.Domain.Configuration
{
    /// <summary>
    /// Valores de la columna <c>Nombre</c> en <c>TrabajosConfiguracion</c> (clave-valor).
    /// Deben coincidir con los nombres leídos por TrabajosService en Infrastructure.
    /// </summary>
    public static class TrabajosConfiguracionKeyNames
    {
        public const string Proyecto = "Proyecto";
        public const string IdProyecto = "IdProyecto";
        public const string IdProyecto2 = "IdProyecto2";
        public const string Proyecto2 = "Proyecto2";
        public const string DiasLookbackTransmittal = "DiasLookbackTransmittal";
        /// <summary>Primer segmento del docno SALFA (ida Codelco→SALFA). No es el N° contrato Codelco.</summary>
        public const string CodigoProyectoSalfa = "CodigoProyectoSalfa";
        public const string CredencialAconex = "CredencialAconex";
        public const string CredencialBD = "CredencialBD";
        public const string CamposConsulta = "CamposConsulta";
        public const string CamposResponse = "CamposResponse";
        public const string CamposBD = "CamposBD";
        public const string BasePath = "BasePath";
        public const string TablaMetadata = "TablaMetadata";
        public const string TablaPaths = "TablaPaths";
        /// <summary>idEstatus fijo al registrar en el proyecto destino (lado 1). Acepta id o nombre en EstatusDocumentos.</summary>
        public const string IdEstatusDocumentoDestino = "IdEstatusDocumentoDestino";
        /// <summary>En vuelta (SALFA→Codelco), solo procesar transmitals cuyo Subject contenga este texto (ej. Final).</summary>
        public const string SubjectFiltroTransmittalVuelta = "SubjectFiltroTransmittalVuelta";
        /// <summary>returnFields extra register/search destino Codelco (supersede vuelta SALFA→Codelco).</summary>
        public const string CamposConsultaRegistroDestino = "CamposConsultaRegistroDestino";
        /// <summary>returnFields extra register/search destino SALFA (supersede ida Codelco→SALFA).</summary>
        public const string CamposConsultaRegistroDestinoSalfa = "CamposConsultaRegistroDestinoSalfa";
    }
}
