-- Adjuntos origen ya sincronizados (idempotencia Opción A: docno+rev+versión en proyecto ORIGEN).
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TransmittalSyncDocumentProcesados')
BEGIN
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
