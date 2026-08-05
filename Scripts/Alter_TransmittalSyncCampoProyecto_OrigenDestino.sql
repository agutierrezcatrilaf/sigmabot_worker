-- Migra TransmittalSyncCampoProyecto a par origen→destino explícito.

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TransmittalSyncCampoProyecto')
BEGIN
    PRINT 'Tabla TransmittalSyncCampoProyecto no existe; ejecute CreateTable_TransmittalSync.sql.';
    RETURN;
END

IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID('TransmittalSyncCampoProyecto') AND name = 'ACXProjectIdOrigen')
BEGIN
  IF EXISTS (
      SELECT * FROM sys.columns
      WHERE object_id = OBJECT_ID('TransmittalSyncCampoProyecto') AND name = 'ACXProjectId')
  BEGIN
    EXEC sp_rename 'TransmittalSyncCampoProyecto.ACXProjectId', 'ACXProjectIdDestino', 'COLUMN';
  END

  IF NOT EXISTS (
      SELECT * FROM sys.columns
      WHERE object_id = OBJECT_ID('TransmittalSyncCampoProyecto') AND name = 'ACXProjectIdDestino')
  BEGIN
    ALTER TABLE TransmittalSyncCampoProyecto ADD ACXProjectIdDestino NVARCHAR(50) NOT NULL DEFAULT ('');
  END

  ALTER TABLE TransmittalSyncCampoProyecto ADD ACXProjectIdOrigen NVARCHAR(50) NOT NULL DEFAULT ('');

  IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_TransmittalSyncCampoProyecto_Trabajo_Proyecto_Campo'
             AND object_id = OBJECT_ID('TransmittalSyncCampoProyecto'))
    DROP INDEX UX_TransmittalSyncCampoProyecto_Trabajo_Proyecto_Campo ON TransmittalSyncCampoProyecto;

  IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_TransmittalSyncCampoProyecto_Trabajo_Par_Campo'
                 AND object_id = OBJECT_ID('TransmittalSyncCampoProyecto'))
    CREATE UNIQUE INDEX UX_TransmittalSyncCampoProyecto_Trabajo_Par_Campo
      ON TransmittalSyncCampoProyecto (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, Campo);

  PRINT 'Agregadas ACXProjectIdOrigen/Destino. Complete ACXProjectIdOrigen en filas existentes y vuelva a ejecutar el seed.';
END
ELSE
  PRINT 'Columna ACXProjectIdOrigen ya existe.';
