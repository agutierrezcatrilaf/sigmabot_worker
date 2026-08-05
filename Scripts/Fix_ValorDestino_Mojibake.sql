-- Corrige ValorDestino con UTF-8 guardado como dos code points (mojibake en NVARCHAR).
-- Ej.: "InformaciÃ³n" (U+00C3 U+00B3) → "Información" (U+00F3).
-- Aplica a todas las equivalencias activas; revisar con SELECT antes/después.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

DECLARE @Before INT = (
    SELECT COUNT(*)
    FROM dbo.TransmittalSyncEquivalencia
    WHERE Activo = 1 AND ValorDestino LIKE '%' + NCHAR(0x00C3) + NCHAR(0x0080) + '%'
);

PRINT 'Registros con posible mojibake (U+00C3 + Latin-1 suplementario): ' + CAST(@Before AS VARCHAR(20));

UPDATE dbo.TransmittalSyncEquivalencia
SET ValorDestino = REPLACE(ValorDestino, NCHAR(0x00C3) + NCHAR(0x00A1), NCHAR(0x00E1)),
    UpdatedAt = SYSUTCDATETIME()
WHERE ValorDestino LIKE '%' + NCHAR(0x00C3) + NCHAR(0x00A1) + '%';

UPDATE dbo.TransmittalSyncEquivalencia
SET ValorDestino = REPLACE(ValorDestino, NCHAR(0x00C3) + NCHAR(0x00A9), NCHAR(0x00E9)),
    UpdatedAt = SYSUTCDATETIME()
WHERE ValorDestino LIKE '%' + NCHAR(0x00C3) + NCHAR(0x00A9) + '%';

UPDATE dbo.TransmittalSyncEquivalencia
SET ValorDestino = REPLACE(ValorDestino, NCHAR(0x00C3) + NCHAR(0x00AD), NCHAR(0x00ED)),
    UpdatedAt = SYSUTCDATETIME()
WHERE ValorDestino LIKE '%' + NCHAR(0x00C3) + NCHAR(0x00AD) + '%';

UPDATE dbo.TransmittalSyncEquivalencia
SET ValorDestino = REPLACE(ValorDestino, NCHAR(0x00C3) + NCHAR(0x00B3), NCHAR(0x00F3)),
    UpdatedAt = SYSUTCDATETIME()
WHERE ValorDestino LIKE '%' + NCHAR(0x00C3) + NCHAR(0x00B3) + '%';

UPDATE dbo.TransmittalSyncEquivalencia
SET ValorDestino = REPLACE(ValorDestino, NCHAR(0x00C3) + NCHAR(0x00BA), NCHAR(0x00FA)),
    UpdatedAt = SYSUTCDATETIME()
WHERE ValorDestino LIKE '%' + NCHAR(0x00C3) + NCHAR(0x00BA) + '%';

UPDATE dbo.TransmittalSyncEquivalencia
SET ValorDestino = REPLACE(ValorDestino, NCHAR(0x00C3) + NCHAR(0x00B1), NCHAR(0x00F1)),
    UpdatedAt = SYSUTCDATETIME()
WHERE ValorDestino LIKE '%' + NCHAR(0x00C3) + NCHAR(0x00B1) + '%';

UPDATE dbo.TransmittalSyncEquivalencia
SET ValorDestino = REPLACE(ValorDestino, NCHAR(0x00C3) + NCHAR(0x0091), NCHAR(0x00D1)),
    UpdatedAt = SYSUTCDATETIME()
WHERE ValorDestino LIKE '%' + NCHAR(0x00C3) + NCHAR(0x0091) + '%';

UPDATE dbo.TransmittalSyncEquivalencia
SET ValorDestino = REPLACE(ValorDestino, NCHAR(0x00C2) + NCHAR(0x00B0), NCHAR(0x00B0)),
    UpdatedAt = SYSUTCDATETIME()
WHERE ValorDestino LIKE '%' + NCHAR(0x00C2) + NCHAR(0x00B0) + '%';

DECLARE @After INT = (
    SELECT COUNT(*)
    FROM dbo.TransmittalSyncEquivalencia
    WHERE Activo = 1 AND ValorDestino LIKE '%' + NCHAR(0x00C3) + NCHAR(0x0080) + '%'
);

PRINT 'Restantes con posible mojibake: ' + CAST(@After AS VARCHAR(20));

SELECT TOP 20 ValorOrigen, ValorDestino, Tipo, IdTrabajo
FROM dbo.TransmittalSyncEquivalencia
WHERE Activo = 1 AND ValorDestino LIKE '%' + NCHAR(0x00C3) + NCHAR(0x0080) + '%';
