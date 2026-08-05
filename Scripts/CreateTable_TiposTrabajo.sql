-- Catálogo de tipos de trabajo: código técnico (consola/API) + nombre visible (UI).
-- Trabajos.Tipo guarda TiposTrabajo.Codigo (p. ej. FileExtraction).
-- Para cambiar el texto del combo: UPDATE TiposTrabajo SET Nombre = '...' WHERE Codigo = '...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TiposTrabajo')
BEGIN
    CREATE TABLE [dbo].[TiposTrabajo] (
        Codigo        NVARCHAR(100)  NOT NULL PRIMARY KEY,
        Nombre        NVARCHAR(200)  NOT NULL,
        Descripcion   NVARCHAR(500)  NULL,
        Orden         INT            NOT NULL CONSTRAINT DF_TiposTrabajo_Orden DEFAULT (0),
        Activo        BIT            NOT NULL CONSTRAINT DF_TiposTrabajo_Activo DEFAULT (1)
    );

    PRINT 'Tabla TiposTrabajo creada.';
END
ELSE
    PRINT 'La tabla TiposTrabajo ya existe.';

-- Datos iniciales (idempotente)
MERGE [dbo].[TiposTrabajo] AS t
USING (VALUES
    (N'FileExtraction', N'Descarga de archivos',
     N'Descarga documentos de Aconex a una carpeta y sincroniza metadata en BD.', 10, 1),
    (N'FullExtraction', N'Extracción a base de datos',
     N'Guarda metadata de documentos, correos y flujos de trabajo en BD (sin descargar archivos).', 20, 1),
    (N'ProjectSync', N'Sincronización entre proyectos',
     N'Copia documentos de un proyecto Aconex a otro mediante transmitals del inbox.', 30, 1),
    (N'FileUploadWithMetadata', N'Carga a Aconex',
     N'Sube archivos desde BD (DocumentosMetadata + DocumentosPath) al register de Aconex.', 40, 1)
) AS s (Codigo, Nombre, Descripcion, Orden, Activo)
ON t.Codigo = s.Codigo
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Codigo, Nombre, Descripcion, Orden, Activo)
    VALUES (s.Codigo, s.Nombre, s.Descripcion, s.Orden, s.Activo);

PRINT 'Catálogo TiposTrabajo sembrado (filas nuevas únicamente).';
