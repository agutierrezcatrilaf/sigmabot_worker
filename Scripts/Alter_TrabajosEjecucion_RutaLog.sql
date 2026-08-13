-- Columna RutaLog: path del archivo job-{IdTrabajo}-ejec-{Id}.log de cada ejecución.

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TrabajosEjecucion')
   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrabajosEjecucion') AND name = 'RutaLog')
BEGIN
    ALTER TABLE [dbo].[TrabajosEjecucion] ADD RutaLog NVARCHAR(500) NULL;
    PRINT 'Columna TrabajosEjecucion.RutaLog agregada.';
END
ELSE IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TrabajosEjecucion')
    PRINT 'La tabla TrabajosEjecucion no existe. Ejecute CreateTable_TrabajosEjecucion.sql primero.';
ELSE
    PRINT 'Columna TrabajosEjecucion.RutaLog ya existe.';
