-- Mapea MailNo del transmittal → NDeDocumento3_singleLineText en destino SALFA (ida Codelco→SALFA).
DECLARE @IdTrabajo INT = 10008;
DECLARE @Codelco   NVARCHAR(50) = '1207996652';
DECLARE @Salfa     NVARCHAR(50) = '1207996803';

IF EXISTS (
    SELECT 1 FROM TransmittalSyncCampoProyecto
    WHERE IdTrabajo = @IdTrabajo
      AND ACXProjectIdOrigen = @Codelco
      AND ACXProjectIdDestino = @Salfa
      AND Campo = 'NDeDocumento3_singleLineText')
BEGIN
    UPDATE TransmittalSyncCampoProyecto
    SET CampoOrigen = 'MailNo',
        EsObligatorio = 0,
        ValorDefault = NULL,
        Catalogo = NULL,
        Orden = 230,
        UpdatedAt = SYSUTCDATETIME()
    WHERE IdTrabajo = @IdTrabajo
      AND ACXProjectIdOrigen = @Codelco
      AND ACXProjectIdDestino = @Salfa
      AND Campo = 'NDeDocumento3_singleLineText';
END
ELSE
BEGIN
    INSERT INTO TransmittalSyncCampoProyecto
        (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, Campo, CampoOrigen, EsObligatorio, ValorDefault, Catalogo, Orden)
    VALUES
        (@IdTrabajo, @Codelco, @Salfa, 'NDeDocumento3_singleLineText', 'MailNo', 0, NULL, NULL, 230);
END

PRINT 'OK: NDeDocumento3_singleLineText ← MailNo (IdTrabajo=10008, Codelco→SALFA).';
