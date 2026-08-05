-- Tabla TrabajosEjecucion: historial de cada ejecución por trabajo.
-- Un INSERT por ejecución con detalle, error (si aplica) y etapas ejecutadas (FileExtraction, DocumentExtraction, etc.).

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TrabajosEjecucion')
BEGIN
    CREATE TABLE [dbo].[TrabajosEjecucion] (
        Id                  INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdTrabajo           INT             NOT NULL,
        FechaHoraInicio     DATETIME2(7)    NOT NULL,
        FechaHoraFin        DATETIME2(7)    NOT NULL,
        Exito               BIT             NOT NULL,
        MensajeError        NVARCHAR(MAX)   NULL,
        EtapasEjecutadas    NVARCHAR(500)   NULL,
        DetalleEjecucion    NVARCHAR(MAX)   NULL,
        TipoEjecucion       NVARCHAR(20)   NULL
    );

    CREATE INDEX IX_TrabajosEjecucion_IdTrabajo ON [dbo].[TrabajosEjecucion] (IdTrabajo);
    CREATE INDEX IX_TrabajosEjecucion_FechaHoraInicio ON [dbo].[TrabajosEjecucion] (FechaHoraInicio);

    PRINT 'Tabla TrabajosEjecucion creada.';
END
ELSE
    PRINT 'La tabla TrabajosEjecucion ya existe.';

-- Migración: añadir TipoEjecucion si la tabla ya existía sin esta columna ('Manual' | 'Scheduler').
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TrabajosEjecucion')
   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrabajosEjecucion') AND name = 'TipoEjecucion')
BEGIN
    ALTER TABLE [dbo].[TrabajosEjecucion] ADD TipoEjecucion NVARCHAR(20) NULL;
    PRINT 'Columna TrabajosEjecucion.TipoEjecucion agregada.';
END
