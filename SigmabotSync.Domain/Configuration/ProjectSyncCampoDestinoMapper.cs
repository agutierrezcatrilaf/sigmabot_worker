using SigmabotSync.Domain.Entities;

namespace SigmabotSync.Domain.Configuration
{
    /// <summary>Traduce filas de matriz destino al modelo runtime del worker.</summary>
    public static class ProjectSyncCampoDestinoMapper
    {
        public static TransmittalSyncCampoMapeoItem ToMapeoItem(
            string campoDestino,
            string tipoFuente,
            string fuenteValor,
            bool esObligatorio,
            string valorDefault,
            string catalogo,
            int orden)
        {
            string destino = (campoDestino ?? "").Trim();
            string tipo = (tipoFuente ?? "").Trim();
            string fuente = (fuenteValor ?? "").Trim();

            return new TransmittalSyncCampoMapeoItem
            {
                CampoDestino = destino,
                CampoOrigen = ResolveCampoOrigen(destino, tipo, fuente),
                EsObligatorio = esObligatorio,
                ValorDefault = string.IsNullOrWhiteSpace(valorDefault) ? null : valorDefault.Trim(),
                Catalogo = string.IsNullOrWhiteSpace(catalogo) ? null : catalogo.Trim(),
                Orden = orden
            };
        }

        public static string ResolveCampoOrigen(string campoDestino, string tipoFuente, string fuenteValor)
        {
            string tipo = (tipoFuente ?? "").Trim();
            string fuente = (fuenteValor ?? "").Trim();
            string destino = (campoDestino ?? "").Trim();

            switch (tipo)
            {
                case ProjectSyncCampoDestinoTipoFuente.ReglaDocumentTypeFromTipo:
                    return ProjectSyncCampoOrigenTokens.DocumentTypeFromTipoDocumento;
                case ProjectSyncCampoDestinoTipoFuente.ParametroIdEstatusDestino:
                    return ProjectSyncCampoOrigenTokens.IdEstatusDocumentoDestino;
                case ProjectSyncCampoDestinoTipoFuente.Adjunto:
                    return string.IsNullOrWhiteSpace(fuente) ? destino : fuente;
                case ProjectSyncCampoDestinoTipoFuente.CampoOrigen:
                    if (!string.IsNullOrWhiteSpace(fuente))
                        return fuente;
                    return string.IsNullOrWhiteSpace(destino) ? null : destino;
                case ProjectSyncCampoDestinoTipoFuente.Constante:
                case ProjectSyncCampoDestinoTipoFuente.SoloPreservar:
                    return null;
                default:
                    if (!string.IsNullOrWhiteSpace(fuente))
                        return fuente;
                    return string.IsNullOrWhiteSpace(destino) ? null : destino;
            }
        }
    }
}
