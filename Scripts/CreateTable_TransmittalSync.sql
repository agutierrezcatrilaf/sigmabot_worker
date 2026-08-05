-- Estado de ProjectSync cross-project.
-- TransmittalSyncProcesados: mails ya leídos del proyecto ORIGEN (ACXProjectId = origen).
-- TransmittalSyncMapeo: DocumentNo+Revision → DocumentId en el proyecto DESTINO (ACXProjectId = destino).
-- TransmittalSyncCampoProyecto: mapeo por par origen→destino (IdTrabajo + ambos ACXProjectId).
-- Campo = tag XML destino; CampoOrigen = nombre en origen; Catalogo = tabla paramétrica (NULL = passthrough).

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TransmittalSyncProcesados')
BEGIN
    CREATE TABLE [dbo].[TransmittalSyncProcesados] (
        Id              INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdTrabajo       INT             NOT NULL,
        ACXProjectId    NVARCHAR(50)    NOT NULL,
        MailId          NVARCHAR(50)    NOT NULL,
        ProcessedAt     DATETIME2       NOT NULL CONSTRAINT DF_TransmittalSyncProcesados_ProcessedAt DEFAULT (SYSUTCDATETIME())
    );

    CREATE UNIQUE INDEX UX_TransmittalSyncProcesados_Trabajo_Proyecto_Mail
        ON [dbo].[TransmittalSyncProcesados] (IdTrabajo, ACXProjectId, MailId);

    PRINT 'Tabla TransmittalSyncProcesados creada.';
END
ELSE
    PRINT 'La tabla TransmittalSyncProcesados ya existe.';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TransmittalSyncMapeo')
BEGIN
    CREATE TABLE [dbo].[TransmittalSyncMapeo] (
        Id              INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdTrabajo       INT             NOT NULL,
        ACXProjectId    NVARCHAR(50)    NOT NULL,
        DocumentNo      NVARCHAR(100)   NOT NULL,
        Revision        NVARCHAR(20)    NOT NULL,
        LocalDocumentId NVARCHAR(50)    NOT NULL,
        UpdatedAt       DATETIME2       NOT NULL CONSTRAINT DF_TransmittalSyncMapeo_UpdatedAt DEFAULT (SYSUTCDATETIME())
    );

    CREATE UNIQUE INDEX UX_TransmittalSyncMapeo_Trabajo_Proyecto_DocRev
        ON [dbo].[TransmittalSyncMapeo] (IdTrabajo, ACXProjectId, DocumentNo, Revision);

    PRINT 'Tabla TransmittalSyncMapeo creada.';
END
ELSE
    PRINT 'La tabla TransmittalSyncMapeo ya existe.';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TransmittalSyncDocumentProcesados')
BEGIN
    SET QUOTED_IDENTIFIER ON;
    CREATE TABLE [dbo].[TransmittalSyncDocumentProcesados] (
        Id                  INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdTrabajo           INT             NOT NULL,
        SourceProjectId     NVARCHAR(50)    NOT NULL,
        SourceDocumentNo    NVARCHAR(100)   NOT NULL,
        SourceRevision      NVARCHAR(20)    NOT NULL,
        SourceVersionNumber NVARCHAR(20)    NOT NULL CONSTRAINT DF_TransmittalSyncDocProc_SourceVersion DEFAULT (N''),
        SourceDocumentId    NVARCHAR(50)    NULL,
        DestProjectId       NVARCHAR(50)    NOT NULL,
        DestDocumentId      NVARCHAR(50)    NOT NULL,
        DestDocumentNo      NVARCHAR(100)   NULL,
        DestRevision        NVARCHAR(20)    NULL,
        MailId              NVARCHAR(50)    NULL,
        ProcessedAt         DATETIME2       NOT NULL CONSTRAINT DF_TransmittalSyncDocProc_ProcessedAt DEFAULT (SYSUTCDATETIME())
    );

    CREATE UNIQUE INDEX UX_TransmittalSyncDocProc_Origen_DocRevVer
        ON [dbo].[TransmittalSyncDocumentProcesados]
        (IdTrabajo, SourceProjectId, SourceDocumentNo, SourceRevision, SourceVersionNumber);

    CREATE UNIQUE INDEX UX_TransmittalSyncDocProc_Origen_DocumentId
        ON [dbo].[TransmittalSyncDocumentProcesados]
        (IdTrabajo, SourceProjectId, SourceDocumentId)
        WHERE SourceDocumentId IS NOT NULL AND SourceDocumentId <> N'';

    PRINT 'Tabla TransmittalSyncDocumentProcesados creada.';
END
ELSE
    PRINT 'La tabla TransmittalSyncDocumentProcesados ya existe.';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TransmittalSyncCampoProyecto')
BEGIN
    CREATE TABLE [dbo].[TransmittalSyncCampoProyecto] (
        Id               INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdTrabajo            INT             NOT NULL,
        ACXProjectIdOrigen   NVARCHAR(50)    NOT NULL,
        ACXProjectIdDestino  NVARCHAR(50)    NOT NULL,
        Campo                NVARCHAR(100)   NOT NULL,
        CampoOrigen          NVARCHAR(100)   NULL,
        EsObligatorio        BIT             NOT NULL CONSTRAINT DF_TransmittalSyncCampoProyecto_Oblig DEFAULT (0),
        ValorDefault         NVARCHAR(500)   NULL,
        Catalogo             NVARCHAR(100)   NULL,
        Orden                INT             NOT NULL CONSTRAINT DF_TransmittalSyncCampoProyecto_Orden DEFAULT (0),
        UpdatedAt            DATETIME2       NOT NULL CONSTRAINT DF_TransmittalSyncCampoProyecto_UpdatedAt DEFAULT (SYSUTCDATETIME())
    );

    CREATE UNIQUE INDEX UX_TransmittalSyncCampoProyecto_Trabajo_Par_Campo
        ON [dbo].[TransmittalSyncCampoProyecto] (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, Campo);

    PRINT 'Tabla TransmittalSyncCampoProyecto creada.';
END
ELSE
    PRINT 'La tabla TransmittalSyncCampoProyecto ya existe.';
