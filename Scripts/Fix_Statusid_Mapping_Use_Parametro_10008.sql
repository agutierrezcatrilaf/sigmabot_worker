-- Homologación vuelta: statusid toma el valor del parámetro IdEstatusDocumentoDestino (no del proyecto origen).

DECLARE @IdTrabajo INT = 10008;
DECLARE @Salfa   NVARCHAR(50) = N'1207996803';
DECLARE @Codelco NVARCHAR(50) = N'1207996652';

UPDATE TransmittalSyncCampoProyecto
SET CampoOrigen = N'@IdEstatusDocumentoDestino',
    ValorDefault = NULL,
    EsObligatorio = 0
WHERE IdTrabajo = @IdTrabajo
  AND ACXProjectIdOrigen = @Salfa
  AND ACXProjectIdDestino = @Codelco
  AND Campo = N'statusid';

PRINT 'statusid vuelta: CampoOrigen=@IdEstatusDocumentoDestino (valor en parámetros del trabajo).';
