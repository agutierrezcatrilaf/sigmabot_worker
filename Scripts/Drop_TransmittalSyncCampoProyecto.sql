-- Elimina homologación legacy (reemplazada por TransmittalSyncCampoDestino).
-- Ejecutar solo cuando CampoDestino esté poblada y validada para los trabajos activos.

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TransmittalSyncCampoProyecto')
BEGIN
    DROP TABLE [dbo].[TransmittalSyncCampoProyecto];
    PRINT 'Tabla TransmittalSyncCampoProyecto eliminada.';
END
ELSE
    PRINT 'La tabla TransmittalSyncCampoProyecto no existe.';
