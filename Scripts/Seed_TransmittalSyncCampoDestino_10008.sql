-- Seed matriz destino IdTrabajo 10008 (equivalente a homologación legacy + CSV supersede Codelco).
-- TipoFuente: CampoOrigen | ReglaDocumentTypeFromTipo | ParametroIdEstatusDestino | Adjunto | Constante | SoloPreservar

DECLARE @IdTrabajo INT = 10008;
DECLARE @Codelco   NVARCHAR(50) = '1207996652';
DECLARE @Salfa     NVARCHAR(50) = '1207996803';

DELETE FROM TransmittalSyncCampoDestino WHERE IdTrabajo = @IdTrabajo;

-- ── Destino SALFA (ida Codelco → SALFA) ─────────────────────────────────────
INSERT INTO TransmittalSyncCampoDestino
    (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, CampoDestino, TipoFuente, FuenteValor, EsObligatorio, ValorDefault, Catalogo, Orden, Activo)
VALUES
(@IdTrabajo, @Codelco, @Salfa, 'Title',            'CampoOrigen', 'title',             1, NULL,      NULL,                    20, 1),
(@IdTrabajo, @Codelco, @Salfa, 'Revision',         'Adjunto',     'Revision',          1, NULL,      NULL,                    30, 1),
(@IdTrabajo, @Codelco, @Salfa, 'DocumentTypeId',   'ReglaDocumentTypeFromTipo', NULL,  1, NULL,      'TiposDocumentos',       40, 1),
(@IdTrabajo, @Codelco, @Salfa, 'DocumentStatusId', 'CampoOrigen', 'statusid',          1, NULL,      'EstatusDocumentos',     50, 1),
(@IdTrabajo, @Codelco, @Salfa, 'Author',           'CampoOrigen', 'author',            0, NULL,      NULL,                    60, 1),
(@IdTrabajo, @Codelco, @Salfa, 'RevisionDate',     'CampoOrigen', 'revisiondate',      1, '@UtcNow', NULL,                    70, 1),
(@IdTrabajo, @Codelco, @Salfa, 'ReviewStatusId',   'CampoOrigen', 'reviewstatus',      0, NULL,      NULL,                    80, 1),
(@IdTrabajo, @Codelco, @Salfa, 'Cma_singleSelect',                    'Constante', NULL, 1, 'TBD',       NULL,                         100, 1),
(@IdTrabajo, @Codelco, @Salfa, 'Cwa_singleSelect',                    'CampoOrigen', 'Localizador_singleSelect', 1, 'General', 'EquivalenciaCwa',            110, 1),
(@IdTrabajo, @Codelco, @Salfa, 'EstadoDeControlRedLine_singleSelect', 'Constante', NULL, 1, 'TBD',       NULL,                         120, 1),
(@IdTrabajo, @Codelco, @Salfa, 'Discipline_singleSelect',             'CampoOrigen', 'Especialidad_singleSelect', 1, 'General', 'EquivalenciaDiscipline', 130, 1),
(@IdTrabajo, @Codelco, @Salfa, 'EstadoDeCubicacin_singleSelect',      'Constante', NULL, 1, 'TBD',       NULL,                         140, 1),
(@IdTrabajo, @Codelco, @Salfa, 'EstatusBim_singleSelect',             'Constante', NULL, 1, 'TBD',       NULL,                         150, 1),
(@IdTrabajo, @Codelco, @Salfa, 'Entidad_singleSelect',                'Constante', NULL, 1, 'TBD',       NULL,                         160, 1),
(@IdTrabajo, @Codelco, @Salfa, 'Proceso_singleSelect',                'Constante', NULL, 1, 'Ingenieria', NULL,                        170, 1),
(@IdTrabajo, @Codelco, @Salfa, 'EstadoDeCompletitud_singleSelect',    'Constante', NULL, 1, 'TBD',       NULL,                         180, 1),
(@IdTrabajo, @Codelco, @Salfa, 'EstadoDeGestinDelCambio_singleSelect','Constante', NULL, 1, 'TBD',       NULL,                         190, 1),
(@IdTrabajo, @Codelco, @Salfa, 'TipoDeDocumento_singleSelect',        'CampoOrigen', 'TipoDeDocumento_singleSelect', 1, NULL, 'EquivalenciaTipoDocumento', 200, 1),
(@IdTrabajo, @Codelco, @Salfa, 'Origen_singleSelect',                 'Constante', NULL, 1, 'Externo',   NULL,                         210, 1),
(@IdTrabajo, @Codelco, @Salfa, 'EstadoDeConsistencia_singleSelect',   'Constante', NULL, 1, 'TBD',       NULL,                         220, 1),
(@IdTrabajo, @Codelco, @Salfa, 'NDeDocumento3_singleLineText',        'Adjunto', 'MailNo',             0, NULL,       NULL,                         230, 1),
(@IdTrabajo, @Codelco, @Salfa, 'CdigoCodelco_singleLineText',         'CampoOrigen', 'DocumentNumber', 0, NULL,       NULL,                         240, 1);

-- ── Destino Codelco (vuelta SALFA → Codelco) ────────────────────────────────
INSERT INTO TransmittalSyncCampoDestino
    (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, CampoDestino, TipoFuente, FuenteValor, EsObligatorio, ValorDefault, Catalogo, Orden, Activo)
VALUES
(@IdTrabajo, @Salfa, @Codelco, 'DocumentNumber', 'CampoOrigen', 'CdigoCodelco_singleLineText', 1, NULL, NULL,                10, 1),
(@IdTrabajo, @Salfa, @Codelco, 'Title',          'CampoOrigen', 'Title',                       1, NULL, NULL,                20, 1),
(@IdTrabajo, @Salfa, @Codelco, 'Revision',       'Adjunto',     'Revision',                    1, NULL, NULL,                30, 1),
(@IdTrabajo, @Salfa, @Codelco, 'doctype',        'CampoOrigen', 'DocumentTypeId',              0, NULL, 'TiposDocumentos',   40, 1),
(@IdTrabajo, @Salfa, @Codelco, 'statusid',       'ParametroIdEstatusDestino', NULL,            0, NULL, 'EstatusDocumentos', 50, 1),
(@IdTrabajo, @Salfa, @Codelco, 'author',         'CampoOrigen', 'Author',                      0, NULL, NULL,                60, 1),
(@IdTrabajo, @Salfa, @Codelco, 'revisiondate',   'CampoOrigen', 'RevisionDate',                0, NULL, NULL,                70, 1),
-- Solo supersede read (antes en CamposConsultaRegistroDestino CSV)
(@IdTrabajo, @Salfa, @Codelco, 'Emisororigen_singleSelect',   'SoloPreservar', NULL, 1, NULL, NULL, 200, 1),
(@IdTrabajo, @Salfa, @Codelco, 'Especialidad_singleSelect',   'SoloPreservar', NULL, 1, NULL, NULL, 210, 1),
(@IdTrabajo, @Salfa, @Codelco, 'Fase_singleSelect',           'SoloPreservar', NULL, 1, NULL, NULL, 220, 1),
(@IdTrabajo, @Salfa, @Codelco, 'Localizador_singleSelect',    'SoloPreservar', NULL, 1, NULL, NULL, 230, 1),
(@IdTrabajo, @Salfa, @Codelco, 'NroDeContrato_singleSelect',  'SoloPreservar', NULL, 1, NULL, NULL, 240, 1),
(@IdTrabajo, @Salfa, @Codelco, 'Proveedor_singleSelect',      'SoloPreservar', NULL, 1, NULL, NULL, 250, 1),
(@IdTrabajo, @Salfa, @Codelco, 'Rendicin_singleSelect',       'SoloPreservar', NULL, 1, NULL, NULL, 260, 1),
(@IdTrabajo, @Salfa, @Codelco, 'TipoDeDocumento_singleSelect','SoloPreservar', NULL, 1, NULL, NULL, 270, 1);

PRINT 'TransmittalSyncCampoDestino: seed IdTrabajo=' + CAST(@IdTrabajo AS VARCHAR(20));
