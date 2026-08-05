-- Marca mail SALFA como ya procesado (omitir en próximo ProjectSync).
-- IdTrabajo 10008, origen SALFA 1207996803 (sentbox / vuelta).
-- Mail: SalfaM-WTR-000002, WF-000001, rev A.

DECLARE @IdTrabajo INT = 10008;
DECLARE @Origen    NVARCHAR(50) = '1207996803';
DECLARE @MailId    NVARCHAR(50) = '1266815599';

IF NOT EXISTS (
    SELECT 1 FROM TransmittalSyncProcesados
    WHERE IdTrabajo = @IdTrabajo AND ACXProjectId = @Origen AND MailId = @MailId)
BEGIN
    INSERT INTO TransmittalSyncProcesados (IdTrabajo, ACXProjectId, MailId, ProcessedAt)
    VALUES (@IdTrabajo, @Origen, @MailId, SYSUTCDATETIME());
    PRINT 'Insertado.';
END
ELSE
    PRINT 'Ya existía.';

SELECT * FROM TransmittalSyncProcesados
WHERE IdTrabajo = @IdTrabajo AND ACXProjectId = @Origen AND MailId = @MailId;
