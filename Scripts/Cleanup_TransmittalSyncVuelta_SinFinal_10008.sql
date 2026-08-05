-- Limpia estado local de vuelta SALFA→Codelco procesada por error (filtro subject vacío).
-- Transmittals sin «Final» en subject: WTR-000003, WTR-000005, WTR-000007.
-- IdTrabajo=10008 | SALFA origen=1207996803 | Codelco destino=1207996652

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @IdTrabajo INT = 10008;
DECLARE @SalfaProjectId NVARCHAR(50) = N'1207996803';
DECLARE @CodelcoProjectId NVARCHAR(50) = N'1207996652';

-- MailIds procesados por error (2026-08-04 ~05:22–05:34, subject sin Final)
DECLARE @BadMails TABLE (MailId NVARCHAR(50) NOT NULL PRIMARY KEY);
INSERT INTO @BadMails (MailId) VALUES
    (N'1266988062'),  -- SalfaM-WTR-000003  (WF-000002) Prueba 2
    (N'1267247395'),  -- SalfaM-WTR-000005  (WF-000003) Prueba 2
    (N'1267250215');  -- SalfaM-WTR-000007  (WF-000004) Prueba 2

PRINT '--- Antes ---';
SELECT COUNT(*) AS ProcesadosBad FROM TransmittalSyncProcesados p
    INNER JOIN @BadMails b ON b.MailId = p.MailId
    WHERE p.IdTrabajo = @IdTrabajo AND p.ACXProjectId = @SalfaProjectId;

SELECT COUNT(*) AS DocProcBad FROM TransmittalSyncDocumentProcesados d
    INNER JOIN @BadMails b ON b.MailId = d.MailId
    WHERE d.IdTrabajo = @IdTrabajo;

-- Mapeo rev B creado/actualizado en la corrida errónea (supersedes indebidos)
DECLARE @BadMapeo TABLE (
    DocumentNo NVARCHAR(100) NOT NULL,
    Revision NVARCHAR(20) NOT NULL,
    LocalDocumentId NVARCHAR(50) NOT NULL
);
INSERT INTO @BadMapeo (DocumentNo, Revision, LocalDocumentId) VALUES
    (N'4600023154-00000-201OH-00001', N'B', N'1353331688722162661'),
    (N'4600023154-00000-206CA-00001', N'B', N'1353331688722162665'),
    (N'4600023154-00000-500AR-00001', N'B', N'1353331688722162675'),
    (N'4600023154-00000-ESPEL-00001', N'B', N'1353331688722162691'),
    (N'4600023154-00000-HDDES-00001', N'B', N'1353331688722162733'),
    (N'4600023154-00000-MNLCI-00001', N'B', N'1353331688722088911'),
    (N'4600023154-02000-502CA-00001', N'B', N'1353331688722089880'),
    (N'4600023154-02232-502CA-00340', N'B', N'1353331688722163824'),
    (N'4600023154-02297-502CA-00042', N'B', N'1353331688722089904');

BEGIN TRANSACTION;

DELETE d
FROM TransmittalSyncDocumentProcesados d
INNER JOIN @BadMails b ON b.MailId = d.MailId
WHERE d.IdTrabajo = @IdTrabajo;

DELETE p
FROM TransmittalSyncProcesados p
INNER JOIN @BadMails b ON b.MailId = p.MailId
WHERE p.IdTrabajo = @IdTrabajo AND p.ACXProjectId = @SalfaProjectId;

DELETE m
FROM TransmittalSyncMapeo m
INNER JOIN @BadMapeo b
    ON m.IdTrabajo = @IdTrabajo
   AND m.ACXProjectId = @CodelcoProjectId
   AND m.DocumentNo = b.DocumentNo
   AND m.Revision = b.Revision
   AND m.LocalDocumentId = b.LocalDocumentId;

COMMIT TRANSACTION;

PRINT '--- Después ---';
SELECT COUNT(*) AS ProcesadosBad FROM TransmittalSyncProcesados p
    INNER JOIN @BadMails b ON b.MailId = p.MailId
    WHERE p.IdTrabajo = @IdTrabajo AND p.ACXProjectId = @SalfaProjectId;

SELECT COUNT(*) AS DocProcBad FROM TransmittalSyncDocumentProcesados d
    INNER JOIN @BadMails b ON b.MailId = d.MailId
    WHERE d.IdTrabajo = @IdTrabajo;

SELECT COUNT(*) AS MapeoRevB_Bad FROM TransmittalSyncMapeo m
    INNER JOIN @BadMapeo b
        ON m.IdTrabajo = @IdTrabajo
       AND m.ACXProjectId = @CodelcoProjectId
       AND m.DocumentNo = b.DocumentNo
       AND m.Revision = b.Revision;

PRINT 'Limpieza completada.';
