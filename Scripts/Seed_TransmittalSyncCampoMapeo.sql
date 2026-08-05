-- Seed ProjectSync. Ajuste @IdTrabajo al suyo.
-- Defaults alineados al register Postman exitoso (SALFA 1207996803).
-- Catalogo: TiposDocumentos / EstatusDocumentos / EquivalenciaDiscipline / EquivalenciaTipoDocumento / EquivalenciaCwa.
-- ValorDefault TBD = opción válida Aconex (placeholder hasta definir mapeo).

DECLARE @IdTrabajo INT = 10008;
DECLARE @Codelco   NVARCHAR(50) = '1207996652';
DECLARE @Salfa     NVARCHAR(50) = '1207996803';

DELETE FROM TransmittalSyncCampoProyecto WHERE IdTrabajo = @IdTrabajo;

-- ── Codelco → SALFA ──────────────────────────────────────────────────────────
INSERT INTO TransmittalSyncCampoProyecto
    (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, Campo, CampoOrigen, EsObligatorio, ValorDefault, Catalogo, Orden)
VALUES
(@IdTrabajo, @Codelco, @Salfa, 'Title',            NULL,                   1, NULL, NULL,                 20),
(@IdTrabajo, @Codelco, @Salfa, 'Revision',         NULL,                   1, NULL, NULL,                 30),
(@IdTrabajo, @Codelco, @Salfa, 'DocumentTypeId',   '@DocumentTypeFromTipoDocumento', 1, NULL, 'TiposDocumentos', 40),
(@IdTrabajo, @Codelco, @Salfa, 'DocumentStatusId', 'statusid',             1, NULL, 'EstatusDocumentos',  50),
(@IdTrabajo, @Codelco, @Salfa, 'Author',           'author',               0, NULL, NULL,                 60),
(@IdTrabajo, @Codelco, @Salfa, 'RevisionDate',     'revisiondate',         1, '@UtcNow', NULL,           70),
(@IdTrabajo, @Codelco, @Salfa, 'ReviewStatusId',   'reviewstatus',         0, NULL, NULL,                 80),
-- Project fields (Postman exitoso). TBD = placeholder Aconex.
(@IdTrabajo, @Codelco, @Salfa, 'Cma_singleSelect',                    NULL,                                1, 'TBD',       NULL,                         100),
(@IdTrabajo, @Codelco, @Salfa, 'Cwa_singleSelect',                    'Localizador_singleSelect',          1, 'General',   'EquivalenciaCwa',            110),
(@IdTrabajo, @Codelco, @Salfa, 'EstadoDeControlRedLine_singleSelect', NULL,                                1, 'TBD',       NULL,                         120),
(@IdTrabajo, @Codelco, @Salfa, 'Discipline_singleSelect',             'Especialidad_singleSelect',         1, 'General',   'EquivalenciaDiscipline',    130),
(@IdTrabajo, @Codelco, @Salfa, 'EstadoDeCubicacin_singleSelect',      NULL,                                1, 'TBD',       NULL,                         140),
(@IdTrabajo, @Codelco, @Salfa, 'EstatusBim_singleSelect',             NULL,                                1, 'TBD',       NULL,                         150),
(@IdTrabajo, @Codelco, @Salfa, 'Entidad_singleSelect',                NULL,                                1, 'TBD',       NULL,                         160),
(@IdTrabajo, @Codelco, @Salfa, 'Proceso_singleSelect',                NULL,                                1, 'Ingenieria', NULL,                        170),
(@IdTrabajo, @Codelco, @Salfa, 'EstadoDeCompletitud_singleSelect',    NULL,                                1, 'TBD',       NULL,                         180),
(@IdTrabajo, @Codelco, @Salfa, 'EstadoDeGestinDelCambio_singleSelect',NULL,                                1, 'TBD',       NULL,                         190),
(@IdTrabajo, @Codelco, @Salfa, 'TipoDeDocumento_singleSelect',        'TipoDeDocumento_singleSelect',      1, NULL, 'EquivalenciaTipoDocumento', 200),
(@IdTrabajo, @Codelco, @Salfa, 'Origen_singleSelect',                 NULL,                                1, 'Externo',   NULL,                         210),
(@IdTrabajo, @Codelco, @Salfa, 'EstadoDeConsistencia_singleSelect',   NULL,                                1, 'TBD',       NULL,                         220),
(@IdTrabajo, @Codelco, @Salfa, 'NDeDocumento3_singleLineText',        'MailNo',                             0, NULL,       NULL,                         230),
(@IdTrabajo, @Codelco, @Salfa, 'CdigoCodelco_singleLineText',         'DocumentNumber',                     0, NULL,       NULL,                         240);

-- ── SALFA → Codelco (mínimo; se completa luego) ───────────────────────────────
INSERT INTO TransmittalSyncCampoProyecto
    (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, Campo, CampoOrigen, EsObligatorio, ValorDefault, Catalogo, Orden)
VALUES
(@IdTrabajo, @Salfa, @Codelco, 'DocumentNumber', 'CdigoCodelco_singleLineText', 1, NULL, NULL,                10),
(@IdTrabajo, @Salfa, @Codelco, 'Title',          NULL,             1, NULL, NULL,                20),
(@IdTrabajo, @Salfa, @Codelco, 'Revision',       NULL,             1, NULL, NULL,                30),
(@IdTrabajo, @Salfa, @Codelco, 'doctype',        'DocumentTypeId', 0, NULL, 'TiposDocumentos',   40),
(@IdTrabajo, @Salfa, @Codelco, 'statusid',       '@IdEstatusDocumentoDestino', 0, NULL, 'EstatusDocumentos', 50),
(@IdTrabajo, @Salfa, @Codelco, 'author',         'Author',         0, NULL, NULL,                60),
(@IdTrabajo, @Salfa, @Codelco, 'revisiondate',   'RevisionDate',   0, NULL, NULL,                70);

PRINT 'TransmittalSyncCampoProyecto: seed IdTrabajo=' + CAST(@IdTrabajo AS VARCHAR(20));
