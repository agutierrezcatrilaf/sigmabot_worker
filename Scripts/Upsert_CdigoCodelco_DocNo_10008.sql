-- Mapea docno origen → CdigoCodelco_singleLineText en destino SALFA (ida Codelco→SALFA).
DECLARE @IdTrabajo INT = 10008;
DECLARE @Codelco   NVARCHAR(50) = '1207996652';
DECLARE @Salfa     NVARCHAR(50) = '1207996803';

IF EXISTS (
    SELECT 1 FROM TransmittalSyncCampoProyecto
    WHERE IdTrabajo = @IdTrabajo
      AND ACXProjectIdOrigen = @Codelco
      AND ACXProjectIdDestino = @Salfa
      AND Campo = 'CdigoCodelco_singleLineText')
BEGIN
    UPDATE TransmittalSyncCampoProyecto
    SET CampoOrigen = 'DocumentNumber',
        EsObligatorio = 0,
        ValorDefault = NULL,
        Catalogo = NULL,
        Orden = 240,
        UpdatedAt = SYSUTCDATETIME()
    WHERE IdTrabajo = @IdTrabajo
      AND ACXProjectIdOrigen = @Codelco
      AND ACXProjectIdDestino = @Salfa
      AND Campo = 'CdigoCodelco_singleLineText';
END
ELSE
BEGIN
    INSERT INTO TransmittalSyncCampoProyecto
        (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, Campo, CampoOrigen, EsObligatorio, ValorDefault, Catalogo, Orden)
    VALUES
        (@IdTrabajo, @Codelco, @Salfa, 'CdigoCodelco_singleLineText', 'DocumentNumber', 0, NULL, NULL, 240);
END

PRINT 'OK: CdigoCodelco_singleLineText ← DocumentNumber (IdTrabajo=10008, Codelco→SALFA).';
