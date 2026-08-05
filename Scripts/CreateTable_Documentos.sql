-- =============================================
-- Tablas Documentos y Documentos_tmp para SigmaBotSync
-- SQL Server. Los nombres de columna deben coincidir con DbColumn en DocumentFieldMappings (settings.json).
-- Fijos: Id, ACXProjectId, TrackingId. Resto según DocumentFieldMappings (todo NVARCHAR).
-- Si agregas un campo nuevo en settings, añade la columna con ALTER TABLE.
-- =============================================

-- USE [TuBaseDeDatos];
-- GO

IF OBJECT_ID(N'dbo.Documentos', N'U') IS NOT NULL
    DROP TABLE dbo.Documentos;
GO

CREATE TABLE dbo.Documentos
(
    Id                              BIGINT          NOT NULL,
    ACXProjectId                    NVARCHAR(50)    NOT NULL,
    TrackingId                      BIGINT          NOT NULL,
    -- Campos estándar (DbColumn en DocumentFieldMappings)
    docno                           NVARCHAR(200)   NULL,
    revision                        NVARCHAR(50)    NULL,
    title                           NVARCHAR(500)   NULL,
    doctype                         NVARCHAR(200)   NULL,
    confidential                    NVARCHAR(100)   NULL,
    revisiondate                    NVARCHAR(50)    NULL,
    registered                      NVARCHAR(50)    NULL,
    milestonedate                   NVARCHAR(50)    NULL,
    plannedsubmissiondate           NVARCHAR(50)    NULL,
    author                          NVARCHAR(500)   NULL,
    reviewstatus                    NVARCHAR(200)   NULL,
    reviewsource                    NVARCHAR(200)   NULL,
    comments                        NVARCHAR(MAX)   NULL,
    versionnumber                   NVARCHAR(20)   NULL,
    statusid                        NVARCHAR(100)   NULL,
    -- Campos custom del proyecto
    Cma_singleSelect                NVARCHAR(500)   NULL,
    Cwa_singleSelect                NVARCHAR(500)   NULL,
    Cwp_singleSelect                NVARCHAR(500)   NULL,
    Description_multiLineText       NVARCHAR(MAX)   NULL,
    Discipline_singleSelect         NVARCHAR(500)   NULL,
    Ewp_singleSelect                NVARCHAR(500)   NULL,
    EstatusBim_singleSelect         NVARCHAR(500)   NULL,
    NDeDocumento2_singleLineText    NVARCHAR(500)   NULL,
    NDeDocumento3_singleLineText    NVARCHAR(500)   NULL,
    Pwp_singleSelect                NVARCHAR(500)   NULL,
    Proceso_singleSelect            NVARCHAR(500)   NULL,
    TipoDeDocumento_singleSelect    NVARCHAR(500)   NULL
);
GO

CREATE NONCLUSTERED INDEX IX_Documentos_ACXProjectId ON dbo.Documentos (ACXProjectId);
GO

-- Documentos_tmp (misma estructura que Documentos)
IF OBJECT_ID(N'dbo.Documentos_tmp', N'U') IS NOT NULL
    DROP TABLE dbo.Documentos_tmp;
GO

CREATE TABLE dbo.Documentos_tmp
(
    Id                              BIGINT          NULL,
    ACXProjectId                    NVARCHAR(50)    NULL,
    TrackingId                      BIGINT          NULL,
    docno                           NVARCHAR(200)   NULL,
    revision                        NVARCHAR(50)    NULL,
    title                           NVARCHAR(500)   NULL,
    doctype                         NVARCHAR(200)   NULL,
    confidential                    NVARCHAR(100)   NULL,
    revisiondate                    NVARCHAR(50)    NULL,
    registered                      NVARCHAR(50)    NULL,
    milestonedate                   NVARCHAR(50)    NULL,
    plannedsubmissiondate           NVARCHAR(50)    NULL,
    author                          NVARCHAR(500)   NULL,
    reviewstatus                    NVARCHAR(200)   NULL,
    reviewsource                    NVARCHAR(200)   NULL,
    comments                        NVARCHAR(MAX)   NULL,
    versionnumber                   NVARCHAR(20)   NULL,
    statusid                        NVARCHAR(100)   NULL,
    Cma_singleSelect                NVARCHAR(500)   NULL,
    Cwa_singleSelect                NVARCHAR(500)   NULL,
    Cwp_singleSelect                NVARCHAR(500)   NULL,
    Description_multiLineText       NVARCHAR(MAX)   NULL,
    Discipline_singleSelect        NVARCHAR(500)   NULL,
    Ewp_singleSelect                NVARCHAR(500)   NULL,
    EstatusBim_singleSelect         NVARCHAR(500)   NULL,
    NDeDocumento2_singleLineText    NVARCHAR(500)   NULL,
    NDeDocumento3_singleLineText    NVARCHAR(500)   NULL,
    Pwp_singleSelect                NVARCHAR(500)   NULL,
    Proceso_singleSelect            NVARCHAR(500)   NULL,
    TipoDeDocumento_singleSelect    NVARCHAR(500)   NULL
);
GO
