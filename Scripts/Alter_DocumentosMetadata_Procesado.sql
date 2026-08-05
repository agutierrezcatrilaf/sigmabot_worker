-- Opcional: marcar filas ya subidas a Aconex (FileUploadWithMetadata).
-- Sin esta columna el worker procesa todas las filas en cada ejecución.

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'DocumentosMetadata')
   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DocumentosMetadata') AND name = 'Procesado')
BEGIN
    ALTER TABLE [dbo].[DocumentosMetadata] ADD [Procesado] BIT NOT NULL CONSTRAINT [DF_DocumentosMetadata_Procesado] DEFAULT (0);
    PRINT 'Columna DocumentosMetadata.Procesado agregada.';
END
ELSE
    PRINT 'DocumentosMetadata.Procesado ya existe o la tabla no está creada.';
