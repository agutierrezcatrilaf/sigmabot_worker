namespace SigmabotSync.Domain.Models.Synchronization
{
    /// <summary>Tablas / catálogos soportados para homologación ProjectSync.</summary>
    public static class AconexDocumentCatalogNames
    {
        public const string TiposDocumentos = "TiposDocumentos";
        public const string EstatusDocumentos = "EstatusDocumentos";
        /// <summary>Equivalencia origen → destino (texto) para Discipline_singleSelect.</summary>
        public const string EquivalenciaDiscipline = "EquivalenciaDiscipline";
        /// <summary>Equivalencia origen → destino (texto) para TipoDeDocumento_singleSelect.</summary>
        public const string EquivalenciaTipoDocumento = "EquivalenciaTipoDocumento";
        /// <summary>Equivalencia Localizador_singleSelect (WBS Codelco) → Cwa_singleSelect SALFA.</summary>
        public const string EquivalenciaCwa = "EquivalenciaCwa";
    }
}
