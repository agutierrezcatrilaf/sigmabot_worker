-- Ida Codelco→SALFA: DocumentTypeId desde TipoDeDocumento (prefijo numérico/letras), no doctype Codelco.

DECLARE @IdTrabajo INT = 10008;
DECLARE @Codelco   NVARCHAR(50) = N'1207996652';
DECLARE @Salfa     NVARCHAR(50) = N'1207996803';

UPDATE TransmittalSyncCampoProyecto
SET CampoOrigen = N'@DocumentTypeFromTipoDocumento'
WHERE IdTrabajo = @IdTrabajo
  AND ACXProjectIdOrigen = @Codelco
  AND ACXProjectIdDestino = @Salfa
  AND Campo = N'DocumentTypeId';

PRINT 'DocumentTypeId ida: CampoOrigen=@DocumentTypeFromTipoDocumento (IdTrabajo=10008).';
GO
