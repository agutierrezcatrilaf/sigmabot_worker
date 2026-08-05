-- Agrega CampoOrigen a TransmittalSyncCampoProyecto (si la tabla ya existe sin esa columna).

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TransmittalSyncCampoProyecto')
   AND NOT EXISTS (
       SELECT * FROM sys.columns
       WHERE object_id = OBJECT_ID('TransmittalSyncCampoProyecto') AND name = 'CampoOrigen')
BEGIN
    ALTER TABLE TransmittalSyncCampoProyecto ADD CampoOrigen NVARCHAR(100) NULL;
    PRINT 'Columna CampoOrigen agregada a TransmittalSyncCampoProyecto.';
END
ELSE
    PRINT 'Nada que alterar.';
