-- Tabla TrabajosConfiguracion: configuración por trabajo en formato clave-valor.
-- Cada parámetro es una fila: idTrabajo, Nombre (clave), ValorTexto (valor).
-- IdProyecto, CamposConsulta, CamposResponse, CamposBD reemplazan ProjectId y DocumentFieldMappings del settings.
-- CamposConsulta/Response/BD son listas separadas por comas, mismo orden (ApiField, JsonProperty, DbColumn).

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TrabajosConfiguracion')
BEGIN
    CREATE TABLE [dbo].[TrabajosConfiguracion] (
        id              INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
        idTrabajo       INT             NOT NULL,
        Nombre          NVARCHAR(100)   NOT NULL,
        ValorTexto      NVARCHAR(MAX)   NULL,
        ValorFechaHora  DATETIME        NULL,
        ValorNumero     FLOAT           NULL,
        tipo            NVARCHAR(50)    NULL
    );

    CREATE INDEX IX_TrabajosConfiguracion_idTrabajo ON [dbo].[TrabajosConfiguracion] (idTrabajo);

    PRINT 'Tabla TrabajosConfiguracion creada.';
END
ELSE
    PRINT 'La tabla TrabajosConfiguracion ya existe.';

-- Ejemplo de datos para idTrabajo = 1 (listas separadas por comas, mismo orden en los tres):
/*
INSERT INTO [dbo].[TrabajosConfiguracion] (idTrabajo, Nombre, ValorTexto) VALUES
(1, 'Proyecto', 'SalfaDemo'),
(1, 'IdProyecto', '1207993566'),
(1, 'CredencialAconex', '1'),
(1, 'CamposConsulta', 'docno,revision,title,doctype,confidential,revisiondate,registered,milestonedate,plannedsubmissiondate,author,reviewstatus,reviewsource,comments,versionnumber,statusid,current,Cma_singleSelect,Cwa_singleSelect,Cwp_singleSelect,Description_multiLineText,Discipline_singleSelect,Ewp_singleSelect,EstatusBim_singleSelect,NDeDocumento2_singleLineText,NDeDocumento3_singleLineText,Pwp_singleSelect,Proceso_singleSelect,TipoDeDocumento_singleSelect'),
(1, 'CamposResponse', 'documentNumber,revision,title,documentType,confidential,revisionDate,dateModified,milestoneDate,plannedSubmissionDate,author,reviewStatus,reviewSource,comments,versionNumber,documentStatus,current,Cma_singleSelect,Cwa_singleSelect,Cwp_singleSelect,Description_multiLineText,Discipline_singleSelect,Ewp_singleSelect,EstatusBim_singleSelect,NDeDocumento2_singleLineText,NDeDocumento3_singleLineText,Pwp_singleSelect,Proceso_singleSelect,TipoDeDocumento_singleSelect'),
(1, 'CamposBD', 'docno,revision,title,doctype,confidential,revisiondate,registered,milestonedate,plannedsubmissiondate,author,reviewstatus,reviewsource,comments,versionnumber,statusid,iscurrent,Cma_singleSelect,Cwa_singleSelect,Cwp_singleSelect,Description_multiLineText,Discipline_singleSelect,Ewp_singleSelect,EstatusBim_singleSelect,NDeDocumento2_singleLineText,NDeDocumento3_singleLineText,Pwp_singleSelect,Proceso_singleSelect,TipoDeDocumento_singleSelect');
*/
