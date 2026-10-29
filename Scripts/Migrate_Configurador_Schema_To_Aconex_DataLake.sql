-- =============================================================================
-- Migracion CONFIGURADOR (sigmabot @ 155.254.24.155) -> Aconex_DataLake @ 172.20.1.219
--
-- Crea solo tablas que NO existen en destino.
-- Data: solo parametricas / equivalencias / mapeos / credenciales.
-- Sin data: Trabajos*, Ejecuciones, Procesados, usuarios, Flujos* legacy.
-- Tambien siembra EstatusDocumentos/TiposDocumentos si estan vacias en destino.
-- Idempotente.
-- =============================================================================
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.Credenciales', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Credenciales] (
    [id] DECIMAL(18,0) IDENTITY(1,1) NOT NULL,
    [Nombre] NVARCHAR(200) NOT NULL,
    [Tipo] NVARCHAR(50) NOT NULL,
    [Aconex_Instancia] NVARCHAR(100) NULL,
    [Aconex_Usuario] NVARCHAR(200) NULL,
    [Aconex_Clave] NVARCHAR(200) NULL,
    [Aconex_IntegrationId] NVARCHAR(200) NULL,
    [Aconex_OrganizationId] NVARCHAR(100) NULL,
    [Aconex_UserId] NVARCHAR(100) NULL,
    [BD_Servidor] NVARCHAR(300) NULL,
    [BD_TipoConexion] NVARCHAR(50) NULL,
    [BD_Usuario] NVARCHAR(200) NULL,
    [BD_Clave] NVARCHAR(200) NULL,
    [BD_BaseDatos] NVARCHAR(300) NULL
    );
    PRINT 'Creada: Credenciales';
END
ELSE
    PRINT 'Ya existe: Credenciales';
GO

IF OBJECT_ID(N'dbo.FlujosdeTrabajo', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FlujosdeTrabajo] (
    [ACXProjectId] VARCHAR(100) NULL,
    [Numero] VARCHAR(50) NULL,
    [Nombre] VARCHAR(500) NULL,
    [Estado] VARCHAR(250) NULL,
    [Plantilla] VARCHAR(500) NULL,
    [NombreEmisor] VARCHAR(500) NULL,
    [ACXIdEmisor] VARCHAR(100) NULL,
    [OrganizacionEmisora] VARCHAR(500) NULL,
    [MotivodeEmision] VARCHAR(500) NULL
    );
    PRINT 'Creada: FlujosdeTrabajo';
END
ELSE
    PRINT 'Ya existe: FlujosdeTrabajo';
GO

IF OBJECT_ID(N'dbo.FlujosdeTrabajoDocumentos', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FlujosdeTrabajoDocumentos] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [ACXProjectId] VARCHAR(100) NULL,
    [IdFlujodeTrabajo] VARCHAR(100) NULL,
    [NumeroFlujodeTrabajo] VARCHAR(100) NULL,
    [Numero] VARCHAR(200) NULL,
    [Titulo] VARCHAR(500) NULL,
    [Revision] VARCHAR(50) NULL,
    [ACXTrackingId] VARCHAR(100) NULL,
    [Version] VARCHAR(50) NULL,
    [Archivo] VARCHAR(255) NULL,
    [Tamaño] BIGINT NULL
    );
    PRINT 'Creada: FlujosdeTrabajoDocumentos';
END
ELSE
    PRINT 'Ya existe: FlujosdeTrabajoDocumentos';
GO

IF OBJECT_ID(N'dbo.FlujosdeTrabajoPasos', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FlujosdeTrabajoPasos] (
    [ACXProjectId] VARCHAR(100) NULL,
    [IdFlujodeTrabajo] VARCHAR(100) NULL,
    [NumeroFlujodeTrabajo] VARCHAR(100) NULL,
    [Nombre] VARCHAR(500) NULL,
    [Estado] VARCHAR(100) NULL,
    [Resultado] VARCHAR(100) NULL,
    [Comentarios] VARCHAR(4000) NULL,
    [FechaFinalizacion] DATETIME NULL,
    [FechaLimite] DATETIME NULL,
    [FechaInicio] DATETIME NULL,
    [Duracion] INT NULL,
    [FechaLimiteOriginal] DATETIME NULL,
    [DiasAtraso] INT NULL,
    [RevisorOrganizacion] VARCHAR(500) NULL,
    [RevisorNombre] VARCHAR(500) NULL,
    [RevisorACXId] VARCHAR(50) NULL,
    [AtrasoFC] INT NULL,
    [AtrasoFCHoy] INT NULL,
    [EstadoFC] VARCHAR(50) NULL,
    [TrackingId] VARCHAR(50) NULL,
    [DocVersion] VARCHAR(50) NULL,
    [Grupo] VARCHAR(200) NULL
    );
    PRINT 'Creada: FlujosdeTrabajoPasos';
END
ELSE
    PRINT 'Ya existe: FlujosdeTrabajoPasos';
GO

IF OBJECT_ID(N'dbo.MetadataDocumento', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MetadataDocumento] (
    [Id] BIGINT NOT NULL,
    [ACXProjectId] NVARCHAR(50) NOT NULL,
    [NombreArchivo] VARCHAR(500) NOT NULL,
    [Procesado] BIT NOT NULL DEFAULT ((0)),
    [revision] NVARCHAR(50) NULL,
    [title] NVARCHAR(500) NULL,
    [doctype] NVARCHAR(200) NULL,
    [confidential] NVARCHAR(100) NULL,
    [revisiondate] NVARCHAR(50) NULL,
    [registered] NVARCHAR(50) NULL,
    [milestonedate] NVARCHAR(50) NULL,
    [plannedsubmissiondate] NVARCHAR(50) NULL,
    [author] NVARCHAR(500) NULL,
    [reviewstatus] NVARCHAR(200) NULL,
    [reviewsource] NVARCHAR(200) NULL,
    [comments] NVARCHAR(MAX) NULL,
    [versionnumber] NVARCHAR(20) NULL,
    [statusid] NVARCHAR(100) NULL,
    [iscurrent] NVARCHAR(50) NULL,
    [Cma_singleSelect] NVARCHAR(500) NULL,
    [Cwa_singleSelect] NVARCHAR(500) NULL,
    [Cwp_singleSelect] NVARCHAR(500) NULL,
    [Description_multiLineText] NVARCHAR(MAX) NULL,
    [Discipline_singleSelect] NVARCHAR(500) NULL,
    [Ewp_singleSelect] NVARCHAR(500) NULL,
    [EstatusBim_singleSelect] NVARCHAR(500) NULL,
    [NDeDocumento2_singleLineText] NVARCHAR(500) NULL,
    [NDeDocumento3_singleLineText] NVARCHAR(500) NULL,
    [Pwp_singleSelect] NVARCHAR(500) NULL,
    [Proceso_singleSelect] NVARCHAR(500) NULL,
    [TipoDeDocumento_singleSelect] NVARCHAR(500) NULL
    );
    PRINT 'Creada: MetadataDocumento';
END
ELSE
    PRINT 'Ya existe: MetadataDocumento';
GO

IF OBJECT_ID(N'dbo.Periodicidad', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Periodicidad] (
    [id] DECIMAL(18,0) IDENTITY(1,1) NOT NULL,
    [idTrabajo] DECIMAL(18,0) NULL,
    [Dia] INT NULL,
    [Hora] NVARCHAR(50) NULL
    );
    PRINT 'Creada: Periodicidad';
END
ELSE
    PRINT 'Ya existe: Periodicidad';
GO

IF OBJECT_ID(N'dbo.Proyectos', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Proyectos] (
    [id] DECIMAL(18,0) IDENTITY(1,1) NOT NULL,
    [Nombre] NVARCHAR(200) NOT NULL,
    [projectId] NVARCHAR(50) NOT NULL,
    [Estado] NVARCHAR(200) NULL,
    [usuario] DECIMAL(10,8) NULL
    );
    PRINT 'Creada: Proyectos';
END
ELSE
    PRINT 'Ya existe: Proyectos';
GO

IF OBJECT_ID(N'dbo.TiposTrabajo', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TiposTrabajo] (
    [Codigo] NVARCHAR(100) NOT NULL,
    [Nombre] NVARCHAR(200) NOT NULL,
    [Descripcion] NVARCHAR(500) NULL,
    [Orden] INT NOT NULL DEFAULT ((0)),
    [Activo] BIT NOT NULL DEFAULT ((1))
    );
    ALTER TABLE [dbo].[TiposTrabajo] ADD CONSTRAINT [PK__TiposTra__06370DAD61FB93A9] PRIMARY KEY CLUSTERED ([Codigo]);
    PRINT 'Creada: TiposTrabajo';
END
ELSE
    PRINT 'Ya existe: TiposTrabajo';
GO

IF OBJECT_ID(N'dbo.Trabajos', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Trabajos] (
    [id] DECIMAL(18,0) IDENTITY(1,1) NOT NULL,
    [Nombre] NVARCHAR(200) NOT NULL,
    [Tipo] NVARCHAR(100) NOT NULL,
    [FechaUltimaEjecucion] DATETIME NULL,
    [ResultadoUltimaEjecucion] NVARCHAR(200) NULL,
    [Estado] NVARCHAR(100) NULL,
    [UltCorrEjecucion] DECIMAL(10,0) NULL
    );
    PRINT 'Creada: Trabajos';
END
ELSE
    PRINT 'Ya existe: Trabajos';
GO

IF OBJECT_ID(N'dbo.TrabajosConfiguracion', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TrabajosConfiguracion] (
    [id] DECIMAL(18,0) IDENTITY(1,1) NOT NULL,
    [idTrabajo] DECIMAL(18,0) NOT NULL,
    [Nombre] NVARCHAR(50) NULL,
    [ValorTexto] NVARCHAR(1000) NULL,
    [ValorFechaHora] NVARCHAR(50) NULL,
    [ValorNumero] NVARCHAR(50) NULL,
    [tipo] VARCHAR(50) NULL
    );
    ALTER TABLE [dbo].[TrabajosConfiguracion] ADD CONSTRAINT [PK_TrabajosConfiguracion] PRIMARY KEY CLUSTERED ([id]);
    PRINT 'Creada: TrabajosConfiguracion';
END
ELSE
    PRINT 'Ya existe: TrabajosConfiguracion';
GO

IF OBJECT_ID(N'dbo.TrabajosEjecucion', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TrabajosEjecucion] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [IdTrabajo] INT NOT NULL,
    [FechaHoraInicio] DATETIME2(7) NOT NULL,
    [FechaHoraFin] DATETIME2(7) NULL,
    [Exito] BIT NOT NULL,
    [MensajeError] NVARCHAR(MAX) NULL,
    [EtapasEjecutadas] NVARCHAR(500) NULL,
    [DetalleEjecucion] NVARCHAR(MAX) NULL,
    [TipoEjecucion] NVARCHAR(50) NULL,
    [RutaLog] NVARCHAR(500) NULL
    );
    ALTER TABLE [dbo].[TrabajosEjecucion] ADD CONSTRAINT [PK__Trabajos__3214EC0779A8ECD6] PRIMARY KEY CLUSTERED ([Id]);
    PRINT 'Creada: TrabajosEjecucion';
END
ELSE
    PRINT 'Ya existe: TrabajosEjecucion';
GO

IF OBJECT_ID(N'dbo.TrabajosProgramacion', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TrabajosProgramacion] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [IdTrabajo] INT NOT NULL,
    [DiaSemana] TINYINT NOT NULL,
    [Hora] TIME NOT NULL,
    [Activo] BIT NOT NULL DEFAULT ((1))
    );
    ALTER TABLE [dbo].[TrabajosProgramacion] ADD CONSTRAINT [PK__Trabajos__3214EC076AC98F44] PRIMARY KEY CLUSTERED ([Id]);
    PRINT 'Creada: TrabajosProgramacion';
END
ELSE
    PRINT 'Ya existe: TrabajosProgramacion';
GO

IF OBJECT_ID(N'dbo.TrabajosProgramados', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TrabajosProgramados] (
    [id] DECIMAL(18,0) IDENTITY(1,1) NOT NULL,
    [Nombre] NVARCHAR(50) NOT NULL,
    [Tipo] NVARCHAR(50) NOT NULL,
    [FrecuenciaDescripcion] NVARCHAR(100) NOT NULL,
    [FrecuenciaTipo] NVARCHAR(50) NOT NULL,
    [Inicio] DATETIME NOT NULL,
    [Estado] NVARCHAR(50) NOT NULL,
    [UltimaEjecucion] DATETIME NULL,
    [Resultado] NVARCHAR(50) NULL
    );
    PRINT 'Creada: TrabajosProgramados';
END
ELSE
    PRINT 'Ya existe: TrabajosProgramados';
GO

IF OBJECT_ID(N'dbo.TransmittalSyncCampoDestino', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TransmittalSyncCampoDestino] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [IdTrabajo] INT NOT NULL,
    [ACXProjectIdOrigen] NVARCHAR(50) NOT NULL,
    [ACXProjectIdDestino] NVARCHAR(50) NOT NULL,
    [CampoDestino] NVARCHAR(200) NOT NULL,
    [TipoFuente] NVARCHAR(50) NOT NULL,
    [FuenteValor] NVARCHAR(500) NULL,
    [EsObligatorio] BIT NOT NULL DEFAULT ((1)),
    [ValorDefault] NVARCHAR(500) NULL,
    [Catalogo] NVARCHAR(100) NULL,
    [Orden] INT NOT NULL DEFAULT ((0)),
    [Activo] BIT NOT NULL DEFAULT ((1)),
    [UpdatedAt] DATETIME2(7) NOT NULL DEFAULT (sysutcdatetime())
    );
    ALTER TABLE [dbo].[TransmittalSyncCampoDestino] ADD CONSTRAINT [PK__Transmit__3214EC07832475C6] PRIMARY KEY CLUSTERED ([Id]);
    PRINT 'Creada: TransmittalSyncCampoDestino';
END
ELSE
    PRINT 'Ya existe: TransmittalSyncCampoDestino';
GO

IF OBJECT_ID(N'dbo.TransmittalSyncDocumentProcesados', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TransmittalSyncDocumentProcesados] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [IdTrabajo] INT NOT NULL,
    [SourceProjectId] NVARCHAR(50) NOT NULL,
    [SourceDocumentNo] NVARCHAR(100) NOT NULL,
    [SourceRevision] NVARCHAR(20) NOT NULL,
    [SourceVersionNumber] NVARCHAR(20) NOT NULL DEFAULT (N''),
    [SourceDocumentId] NVARCHAR(50) NULL,
    [DestProjectId] NVARCHAR(50) NOT NULL,
    [DestDocumentId] NVARCHAR(50) NOT NULL,
    [DestDocumentNo] NVARCHAR(100) NULL,
    [DestRevision] NVARCHAR(20) NULL,
    [MailId] NVARCHAR(50) NULL,
    [ProcessedAt] DATETIME2(7) NOT NULL DEFAULT (sysutcdatetime())
    );
    ALTER TABLE [dbo].[TransmittalSyncDocumentProcesados] ADD CONSTRAINT [PK__Transmit__3214EC070F5C36D7] PRIMARY KEY CLUSTERED ([Id]);
    PRINT 'Creada: TransmittalSyncDocumentProcesados';
END
ELSE
    PRINT 'Ya existe: TransmittalSyncDocumentProcesados';
GO

IF OBJECT_ID(N'dbo.TransmittalSyncEquivalencia', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TransmittalSyncEquivalencia] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [IdTrabajo] INT NOT NULL,
    [ACXProjectIdOrigen] NVARCHAR(50) NOT NULL,
    [ACXProjectIdDestino] NVARCHAR(50) NOT NULL,
    [Tipo] NVARCHAR(50) NOT NULL,
    [ValorOrigen] NVARCHAR(200) NOT NULL,
    [ValorDestino] NVARCHAR(200) NOT NULL,
    [Activo] BIT NOT NULL DEFAULT ((1)),
    [UpdatedAt] DATETIME2(7) NOT NULL DEFAULT (sysutcdatetime()),
    [CodigoDestino] NVARCHAR(50) NOT NULL
    );
    ALTER TABLE [dbo].[TransmittalSyncEquivalencia] ADD CONSTRAINT [PK_TransmittalSyncEquivalencia] PRIMARY KEY CLUSTERED ([Id]);
    PRINT 'Creada: TransmittalSyncEquivalencia';
END
ELSE
    PRINT 'Ya existe: TransmittalSyncEquivalencia';
GO

IF OBJECT_ID(N'dbo.TransmittalSyncMapeo', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TransmittalSyncMapeo] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [IdTrabajo] INT NOT NULL,
    [ACXProjectId] NVARCHAR(50) NOT NULL,
    [DocumentNo] NVARCHAR(100) NOT NULL,
    [Revision] NVARCHAR(20) NOT NULL,
    [LocalDocumentId] NVARCHAR(50) NOT NULL,
    [UpdatedAt] DATETIME2(7) NOT NULL DEFAULT (sysutcdatetime())
    );
    ALTER TABLE [dbo].[TransmittalSyncMapeo] ADD CONSTRAINT [PK__Transmit__3214EC07969B6D8F] PRIMARY KEY CLUSTERED ([Id]);
    PRINT 'Creada: TransmittalSyncMapeo';
END
ELSE
    PRINT 'Ya existe: TransmittalSyncMapeo';
GO

IF OBJECT_ID(N'dbo.TransmittalSyncProcesados', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TransmittalSyncProcesados] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [IdTrabajo] INT NOT NULL,
    [ACXProjectId] NVARCHAR(50) NOT NULL,
    [MailId] NVARCHAR(50) NOT NULL,
    [ProcessedAt] DATETIME2(7) NOT NULL DEFAULT (sysutcdatetime())
    );
    ALTER TABLE [dbo].[TransmittalSyncProcesados] ADD CONSTRAINT [PK__Transmit__3214EC07309CB578] PRIMARY KEY CLUSTERED ([Id]);
    PRINT 'Creada: TransmittalSyncProcesados';
END
ELSE
    PRINT 'Ya existe: TransmittalSyncProcesados';
GO

IF OBJECT_ID(N'dbo.usuarios', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[usuarios] (
    [id] DECIMAL(18,0) IDENTITY(1,1) NOT NULL,
    [username] NVARCHAR(200) NOT NULL,
    [password_hash] NVARCHAR(200) NOT NULL,
    [rol] NVARCHAR(10) NULL,
    [Organizacion] NVARCHAR(200) NULL,
    [OrganizacionId] NVARCHAR(100) NULL,
    [Nombre] NVARCHAR(200) NULL
    );
    ALTER TABLE [dbo].[usuarios] ADD CONSTRAINT [PK__usuarios__3213E83F38D63F5D] PRIMARY KEY CLUSTERED ([id]);
    PRINT 'Creada: usuarios';
END
ELSE
    PRINT 'Ya existe: usuarios';
GO

IF OBJECT_ID(N'dbo.TrabajosEjecucion', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_TrabajosEjecucion_FechaHoraInicio' AND object_id=OBJECT_ID(N'dbo.TrabajosEjecucion'))
    CREATE NONCLUSTERED INDEX [IX_TrabajosEjecucion_FechaHoraInicio] ON [dbo].[TrabajosEjecucion] ([FechaHoraInicio] ASC);
GO
IF OBJECT_ID(N'dbo.TrabajosEjecucion', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_TrabajosEjecucion_IdTrabajo' AND object_id=OBJECT_ID(N'dbo.TrabajosEjecucion'))
    CREATE NONCLUSTERED INDEX [IX_TrabajosEjecucion_IdTrabajo] ON [dbo].[TrabajosEjecucion] ([IdTrabajo] ASC);
GO
IF OBJECT_ID(N'dbo.TrabajosProgramacion', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_TrabajosProgramacion_DiaHora' AND object_id=OBJECT_ID(N'dbo.TrabajosProgramacion'))
    CREATE NONCLUSTERED INDEX [IX_TrabajosProgramacion_DiaHora] ON [dbo].[TrabajosProgramacion] ([DiaSemana] ASC, [Hora] ASC);
GO
IF OBJECT_ID(N'dbo.TrabajosProgramacion', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_TrabajosProgramacion_IdTrabajo' AND object_id=OBJECT_ID(N'dbo.TrabajosProgramacion'))
    CREATE NONCLUSTERED INDEX [IX_TrabajosProgramacion_IdTrabajo] ON [dbo].[TrabajosProgramacion] ([IdTrabajo] ASC);
GO
IF OBJECT_ID(N'dbo.TransmittalSyncCampoDestino', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_TransmittalSyncCampoDestino_Trabajo_Par_Campo' AND object_id=OBJECT_ID(N'dbo.TransmittalSyncCampoDestino'))
    CREATE UNIQUE NONCLUSTERED INDEX [UX_TransmittalSyncCampoDestino_Trabajo_Par_Campo] ON [dbo].[TransmittalSyncCampoDestino] ([IdTrabajo] ASC, [ACXProjectIdOrigen] ASC, [ACXProjectIdDestino] ASC, [CampoDestino] ASC);
GO
IF OBJECT_ID(N'dbo.TransmittalSyncDocumentProcesados', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_TransmittalSyncDocProc_Origen_DocRevVer' AND object_id=OBJECT_ID(N'dbo.TransmittalSyncDocumentProcesados'))
    CREATE UNIQUE NONCLUSTERED INDEX [UX_TransmittalSyncDocProc_Origen_DocRevVer] ON [dbo].[TransmittalSyncDocumentProcesados] ([IdTrabajo] ASC, [SourceProjectId] ASC, [SourceDocumentNo] ASC, [SourceRevision] ASC, [SourceVersionNumber] ASC);
GO
IF OBJECT_ID(N'dbo.TransmittalSyncDocumentProcesados', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_TransmittalSyncDocProc_Origen_DocumentId' AND object_id=OBJECT_ID(N'dbo.TransmittalSyncDocumentProcesados'))
    CREATE UNIQUE NONCLUSTERED INDEX [UX_TransmittalSyncDocProc_Origen_DocumentId] ON [dbo].[TransmittalSyncDocumentProcesados] ([IdTrabajo] ASC, [SourceProjectId] ASC, [SourceDocumentId] ASC);
GO
IF OBJECT_ID(N'dbo.TransmittalSyncEquivalencia', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_TransmittalSyncEquivalencia_Lookup' AND object_id=OBJECT_ID(N'dbo.TransmittalSyncEquivalencia'))
    CREATE UNIQUE NONCLUSTERED INDEX [UX_TransmittalSyncEquivalencia_Lookup] ON [dbo].[TransmittalSyncEquivalencia] ([IdTrabajo] ASC, [ACXProjectIdOrigen] ASC, [ACXProjectIdDestino] ASC, [Tipo] ASC, [ValorOrigen] ASC);
GO
IF OBJECT_ID(N'dbo.TransmittalSyncMapeo', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_TransmittalSyncMapeo_Trabajo_Proyecto_DocRev' AND object_id=OBJECT_ID(N'dbo.TransmittalSyncMapeo'))
    CREATE UNIQUE NONCLUSTERED INDEX [UX_TransmittalSyncMapeo_Trabajo_Proyecto_DocRev] ON [dbo].[TransmittalSyncMapeo] ([IdTrabajo] ASC, [ACXProjectId] ASC, [DocumentNo] ASC, [Revision] ASC);
GO
IF OBJECT_ID(N'dbo.TransmittalSyncProcesados', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_TransmittalSyncProcesados_Trabajo_Proyecto_Mail' AND object_id=OBJECT_ID(N'dbo.TransmittalSyncProcesados'))
    CREATE UNIQUE NONCLUSTERED INDEX [UX_TransmittalSyncProcesados_Trabajo_Proyecto_Mail] ON [dbo].[TransmittalSyncProcesados] ([IdTrabajo] ASC, [ACXProjectId] ASC, [MailId] ASC);
GO
IF OBJECT_ID(N'dbo.usuarios', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UQ__usuarios__F3DBC5724F33E457' AND object_id=OBJECT_ID(N'dbo.usuarios'))
    CREATE UNIQUE NONCLUSTERED INDEX [UQ__usuarios__F3DBC5724F33E457] ON [dbo].[usuarios] ([username] ASC);
GO

-- FKs entre tablas creadas (solo si ambas existen)
IF OBJECT_ID(N'dbo.Trabajos', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.TiposTrabajo', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Trabajos_TiposTrabajo')
    ALTER TABLE [dbo].[Trabajos] WITH NOCHECK ADD CONSTRAINT [FK_Trabajos_TiposTrabajo] FOREIGN KEY ([Tipo]) REFERENCES [dbo].[TiposTrabajo] ([Codigo]);
GO

PRINT 'Esquema configurador -> Aconex_DataLake listo (sin data).';
PRINT 'Usar Migrate-Configurador-To-DataLake.ps1 -Phase Data para parametricas/equivalencias.';
GO
