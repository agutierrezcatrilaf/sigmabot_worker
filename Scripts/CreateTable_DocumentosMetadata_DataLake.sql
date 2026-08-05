-- Metadata de documentos para carga a Aconex (FileUploadWithMetadata).
-- Base de datos destino: Aconex_DataLake (o la que indique CredencialBD del trabajo).

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DocumentosMetadata')
BEGIN
    CREATE TABLE [dbo].[DocumentosMetadata](
        [Id] [nvarchar](32) NOT NULL,
        [Doctype] [nvarchar](100) NOT NULL CONSTRAINT [DF_DocumentosMetadata_Doctype] DEFAULT ('Documento Interno'),
        [TipoDocumento] [nvarchar](100) NOT NULL CONSTRAINT [DF_DocumentosMetadata_TipoDocumento] DEFAULT ('Certificado'),
        [CreadoPor] [nvarchar](100) NOT NULL CONSTRAINT [DF_DocumentosMetadata_CreadoPor] DEFAULT ('SALFAMontajes'),
        [NumeroDocumento] [nvarchar](50) NULL,
        [Titulo] [nvarchar](500) NULL,
        [Revision] [nvarchar](50) NULL,
        [Status] [nvarchar](50) NULL,
        [FechaRevision] [datetime] NULL,
        [NumeroTransmittal] [nvarchar](100) NULL,
        [ACXProjectId] [nvarchar](32) NULL,
        [CWA] [nvarchar](50) NULL,
        [CWP] [nvarchar](50) NULL,
        [EWP] [nvarchar](50) NULL,
        [PWP] [nvarchar](50) NULL,
        [CMA] [nvarchar](50) NULL,
        [Discipline] [nvarchar](50) NULL,
        [Proceso] [nvarchar](100) NULL,
        [EstatusBim] [nvarchar](50) NULL,
        [CreadoEn] [datetime] NOT NULL CONSTRAINT [DF_DocumentosMetadata_CreadoEn] DEFAULT (getutcdate()),
        CONSTRAINT [PK_DocumentosMetadata] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Tabla DocumentosMetadata creada.';
END
ELSE
    PRINT 'La tabla DocumentosMetadata ya existe.';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DocumentosPath')
BEGIN
    CREATE TABLE [dbo].[DocumentosPath](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [DocumentoId] [nvarchar](32) NOT NULL,
        [PathFisico] [nvarchar](500) NOT NULL,
        [HashArchivo] [nvarchar](200) NULL,
        [Size] [bigint] NULL,
        [Extension] [nvarchar](20) NULL,
        [CreadoEn] [datetime] NOT NULL CONSTRAINT [DF_DocumentosPath_CreadoEn] DEFAULT (getutcdate()),
        CONSTRAINT [PK_DocumentosPath] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_DocumentosPath_DocumentosMetadata] FOREIGN KEY ([DocumentoId])
            REFERENCES [dbo].[DocumentosMetadata] ([Id])
    );
    PRINT 'Tabla DocumentosPath creada.';
END
ELSE
    PRINT 'La tabla DocumentosPath ya existe.';
