-- Permite NULL en FechaHoraFin para registrar el inicio de una ejecución y actualizar al finalizar.
-- Así se puede detectar "ejecución en curso" (FechaHoraFin IS NULL) y evitar lanzar duplicados.

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TrabajosEjecucion')
BEGIN
    ALTER TABLE [dbo].[TrabajosEjecucion]
    ALTER COLUMN FechaHoraFin DATETIME2(7) NULL;
    PRINT 'TrabajosEjecucion.FechaHoraFin permite NULL (ejecución en curso).';
END
ELSE
    PRINT 'La tabla TrabajosEjecucion no existe. Ejecute CreateTable_TrabajosEjecucion.sql primero.';
