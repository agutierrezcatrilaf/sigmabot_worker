-- Elimina columnas de Trabajos no usadas por el motor (programación en TrabajosProgramacion).
-- Ejecutar cuando los datos no son productivos o tras respaldo.
-- Columnas que permanecen: id, Nombre, Tipo, Estado, FechaUltimaEjecucion, ResultadoUltimaEjecucion, UltCorrEjecucion

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Trabajos') AND name = N'Perioricidad')
BEGIN
    ALTER TABLE [dbo].[Trabajos] DROP COLUMN [Perioricidad];
    PRINT 'Columna Perioricidad eliminada.';
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Trabajos') AND name = N'FechaProximaEjecucion')
BEGIN
    ALTER TABLE [dbo].[Trabajos] DROP COLUMN [FechaProximaEjecucion];
    PRINT 'Columna FechaProximaEjecucion eliminada.';
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Trabajos') AND name = N'ControldeEjecucion')
BEGIN
    ALTER TABLE [dbo].[Trabajos] DROP COLUMN [ControldeEjecucion];
    PRINT 'Columna ControldeEjecucion eliminada.';
END

PRINT 'Trabajos: limpieza de columnas legado finalizada.';
