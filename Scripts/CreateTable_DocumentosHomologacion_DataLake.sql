-- Homologación de campos de certificados. Vive en Aconex_DataLake, junto a DocumentosMetadata y DocumentosPath.
-- El trabajo la nombra con TrabajosConfiguracion.TablaHomologacion (valor estándar: DocumentosHomologacion).
-- ValorOrigen es el texto de la metadata (SALFA Forms). ValorDestino es el valor de Aconex.
-- Solo los destinos que el cliente definió en el correo. Lo que no está aquí no se envía.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'DocumentosHomologacion' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE [dbo].[DocumentosHomologacion](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Campo] NVARCHAR(50) NOT NULL,
        [ValorOrigen] NVARCHAR(200) NOT NULL,
        [ValorDestino] NVARCHAR(200) NOT NULL,
        [Activo] BIT NOT NULL CONSTRAINT [DF_DocumentosHomologacion_Activo] DEFAULT (1),
        CONSTRAINT [PK_DocumentosHomologacion] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Tabla DocumentosHomologacion creada.';
END
ELSE
    PRINT 'La tabla DocumentosHomologacion ya existe.';
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DocumentosHomologacion_Campo_Origen' AND object_id = OBJECT_ID(N'dbo.DocumentosHomologacion'))
    DROP INDEX [UX_DocumentosHomologacion_Campo_Origen] ON [dbo].[DocumentosHomologacion];
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DocumentosHomologacion_Campo_Origen_Destino' AND object_id = OBJECT_ID(N'dbo.DocumentosHomologacion'))
    CREATE UNIQUE NONCLUSTERED INDEX [UX_DocumentosHomologacion_Campo_Origen_Destino]
        ON [dbo].[DocumentosHomologacion] ([Campo] ASC, [ValorOrigen] ASC, [ValorDestino] ASC);
GO

DELETE FROM dbo.DocumentosHomologacion WHERE Campo = N'Discipline';
GO

INSERT INTO dbo.DocumentosHomologacion (Campo, ValorOrigen, ValorDestino) VALUES
(N'Discipline', N'E&I', N'Electricidad'),
(N'Discipline', N'GENERAL', N'General'),
(N'Discipline', N'MECÁNICA', N'Mecanica'),
(N'Discipline', N'PINTURA', N'Mecanica'),
(N'Discipline', N'SSOMA', N'SSOMA'),
(N'Discipline', N'EEMM', N'Equipos');
GO
