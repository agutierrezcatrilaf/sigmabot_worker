namespace SigmabotSync.Domain.Entities
{
    /// <summary>
    /// Campo resuelto para Register: origen → destino en una pasada A→B.
    /// </summary>
    public sealed class TransmittalSyncCampoMapeoItem
    {
        public string CampoOrigen { get; set; }
        public string CampoDestino { get; set; }
        public string ValorDefault { get; set; }
        /// <summary>Tabla paramétrica CredencialBD (TiposDocumentos, EstatusDocumentos). NULL = enviar valor tal cual.</summary>
        public string Catalogo { get; set; }
        public bool EsObligatorio { get; set; }
        public int Orden { get; set; }
    }
}
