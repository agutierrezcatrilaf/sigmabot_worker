-- Agrega par origen→destino a TransmittalSyncCampoMapeo (instalaciones que ya crearon la tabla sin estas columnas).
-- Ejecutar solo si la tabla existe SIN ACXProjectIdOrigen / ACXProjectIdDestino.

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TransmittalSyncCampoMapeo')
   AND NOT EXISTS (
       SELECT * FROM sys.columns
       WHERE object_id = OBJECT_ID('TransmittalSyncCampoMapeo') AND name = 'ACXProjectIdOrigen')
BEGIN
    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_TransmittalSyncCampoMapeo_Trabajo_Destino'
               AND object_id = OBJECT_ID('TransmittalSyncCampoMapeo'))
        DROP INDEX UX_TransmittalSyncCampoMapeo_Trabajo_Destino ON TransmittalSyncCampoMapeo;

    ALTER TABLE TransmittalSyncCampoMapeo ADD
        ACXProjectIdOrigen  NVARCHAR(50) NOT NULL CONSTRAINT DF_TransmittalSyncCampoMapeo_Origen DEFAULT (''),
        ACXProjectIdDestino NVARCHAR(50) NOT NULL CONSTRAINT DF_TransmittalSyncCampoMapeo_Destino DEFAULT ('');

    ALTER TABLE TransmittalSyncCampoMapeo DROP CONSTRAINT DF_TransmittalSyncCampoMapeo_Origen;
    ALTER TABLE TransmittalSyncCampoMapeo DROP CONSTRAINT DF_TransmittalSyncCampoMapeo_Destino;

    CREATE UNIQUE INDEX UX_TransmittalSyncCampoMapeo_Trabajo_Par_Destino
        ON TransmittalSyncCampoMapeo (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, CampoDestino);

    PRINT 'Columnas ACXProjectIdOrigen/Destino agregadas. Actualice filas existentes con los IDs de proyecto antes de usar ProjectSync.';
END
ELSE
    PRINT 'Nada que alterar (tabla nueva o columnas ya existen).';
