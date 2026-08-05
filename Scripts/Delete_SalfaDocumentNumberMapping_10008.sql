-- Ida Codelco→SALFA: quitar mapeo DocumentNumber con @SalfaDocumentNumberFromCodelco (AutoNumber en destino).
DECLARE @IdTrabajo INT = 10008;
DECLARE @Codelco   NVARCHAR(50) = '1207996652';
DECLARE @Salfa     NVARCHAR(50) = '1207996803';

DELETE FROM TransmittalSyncCampoProyecto
WHERE IdTrabajo = @IdTrabajo
  AND ACXProjectIdOrigen = @Codelco
  AND ACXProjectIdDestino = @Salfa
  AND Campo = 'DocumentNumber'
  AND CampoOrigen = '@SalfaDocumentNumberFromCodelco';

PRINT 'Eliminado mapeo DocumentNumber/@SalfaDocumentNumberFromCodelco si existía.';
