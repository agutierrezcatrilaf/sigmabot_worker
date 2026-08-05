-- Tabla Trabajos: definición del trabajo y resumen de la última ejecución.
-- Programación: tabla TrabajosProgramacion (día/hora).
-- Historial: tabla TrabajosEjecucion.

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Trabajos')
BEGIN
    CREATE TABLE [dbo].[Trabajos] (
        id                      INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Nombre                  NVARCHAR(200)   NULL,
        Tipo                    NVARCHAR(100)   NULL,  -- Código de TiposTrabajo.Codigo (p. ej. FileExtraction)
        Estado                  NVARCHAR(50)    NULL,
        FechaUltimaEjecucion    DATETIME        NULL,
        ResultadoUltimaEjecucion NVARCHAR(50)   NULL,
        UltCorrEjecucion        NVARCHAR(MAX)   NULL
    );

    CREATE INDEX IX_Trabajos_Estado ON [dbo].[Trabajos] (Estado);

    PRINT 'Tabla Trabajos creada.';
END
ELSE
    PRINT 'La tabla Trabajos ya existe.';

-- Ejemplo: insertar trabajo (id se asigna por IDENTITY)
/*
INSERT INTO [dbo].[Trabajos] (Nombre, Tipo, Estado)
VALUES ('Extracción documentos Aconex', 'FileExtraction', 'Activo');
*/
