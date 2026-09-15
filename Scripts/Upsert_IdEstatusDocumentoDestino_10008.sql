-- IdEstatusDocumentoDestino: allowlist de filtro en vuelta SALFA → Codelco.
-- Solo sincroniza adjuntos cuyo RegisteredDocumentAttachment.Status coincida (por id o nombre).
-- El status escrito en Codelco sale del Status del adjunto (matriz: statusid ← Adjunto/Status).
-- Preferir idEstatus numérico. Emitido para Revisión = 1207959768.
-- Varios: CSV (ej. 1207959768,otroId). Vacío = sin filtro por status.

DECLARE @IdTrabajo INT = 10008;
DECLARE @IdEstatus NVARCHAR(200) = N'1207959768';

IF NOT EXISTS (
    SELECT 1 FROM TrabajosConfiguracion
    WHERE idTrabajo = @IdTrabajo AND Nombre = N'IdEstatusDocumentoDestino')
BEGIN
    INSERT INTO TrabajosConfiguracion (idTrabajo, Nombre, ValorTexto)
    VALUES (@IdTrabajo, N'IdEstatusDocumentoDestino', @IdEstatus);
END
ELSE
BEGIN
    UPDATE TrabajosConfiguracion
    SET ValorTexto = @IdEstatus
    WHERE idTrabajo = @IdTrabajo AND Nombre = N'IdEstatusDocumentoDestino';
END

-- Matriz vuelta: statusid desde Status del adjunto (no parámetro fijo).
UPDATE TransmittalSyncCampoDestino
SET TipoFuente = N'Adjunto',
    FuenteValor = N'Status',
    Catalogo = N'EstatusDocumentos',
    UpdatedAt = SYSUTCDATETIME()
WHERE IdTrabajo = @IdTrabajo
  AND ACXProjectIdOrigen = N'1207996803'
  AND ACXProjectIdDestino = N'1207996652'
  AND CampoDestino = N'statusid';

IF @@ROWCOUNT = 0
BEGIN
    INSERT INTO TransmittalSyncCampoDestino
        (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, CampoDestino, TipoFuente, FuenteValor,
         EsObligatorio, ValorDefault, Catalogo, Orden, Activo)
    VALUES
        (@IdTrabajo, N'1207996803', N'1207996652', N'statusid', N'Adjunto', N'Status',
         0, NULL, N'EstatusDocumentos', 50, 1);
END

PRINT 'IdEstatusDocumentoDestino=' + @IdEstatus + ' (allowlist vuelta) IdTrabajo=' + CAST(@IdTrabajo AS VARCHAR(20));
PRINT 'statusid vuelta: TipoFuente=Adjunto FuenteValor=Status.';
