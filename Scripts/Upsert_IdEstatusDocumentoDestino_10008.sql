-- Estatus fijo al registrar en Codelco (vuelta SALFA → Codelco).
-- Fuente única: TrabajosConfiguracion.IdEstatusDocumentoDestino (parámetros en configurador web).
-- Preferir idEstatus numérico. Emitido para Revisión = 1207959768.

DECLARE @IdTrabajo INT = 10008;
DECLARE @IdEstatus NVARCHAR(50) = N'1207959768';

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

PRINT 'IdEstatusDocumentoDestino=' + @IdEstatus + ' para IdTrabajo=' + CAST(@IdTrabajo AS VARCHAR(20));
