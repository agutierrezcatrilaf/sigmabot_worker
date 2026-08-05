-- CodigoDestino: código corto SALFA (docno / nomenclatura). ValorDestino = picklist register.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TransmittalSyncEquivalencia')
      AND name = N'CodigoDestino')
BEGIN
    ALTER TABLE dbo.TransmittalSyncEquivalencia
        ADD CodigoDestino NVARCHAR(50) NULL;
    PRINT 'Columna CodigoDestino agregada (nullable).';
END
ELSE
    PRINT 'Columna CodigoDestino ya existe.';
GO

-- TipoDocumento: prefijo antes del primer '-' en ValorDestino (columna F del Excel).
UPDATE dbo.TransmittalSyncEquivalencia
SET CodigoDestino = LTRIM(RTRIM(
        LEFT(ValorDestino, CHARINDEX(N'-', ValorDestino + N'-') - 1)))
WHERE Tipo = N'TipoDocumento'
  AND Activo = 1
  AND ValorDestino IS NOT NULL
  AND LTRIM(RTRIM(ValorDestino)) <> N''
  AND (CodigoDestino IS NULL OR LTRIM(RTRIM(CodigoDestino)) = N'');

PRINT 'Backfill TipoDocumento CodigoDestino desde ValorDestino: ' + CAST(@@ROWCOUNT AS VARCHAR(20)) + ' fila(s).';
GO

-- Discipline / Cwa: ejecutar Scripts/Apply_CodigoDestino_From_HomologacionExcel.ps1

IF EXISTS (
    SELECT 1 FROM dbo.TransmittalSyncEquivalencia
    WHERE Activo = 1 AND (CodigoDestino IS NULL OR LTRIM(RTRIM(CodigoDestino)) = N''))
BEGIN
    PRINT 'AVISO: hay equivalencias activas sin CodigoDestino. Ejecute Apply_CodigoDestino_From_HomologacionExcel.ps1 y vuelva a correr el ALTER NOT NULL.';
END
ELSE IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TransmittalSyncEquivalencia')
      AND name = N'CodigoDestino'
      AND is_nullable = 1)
BEGIN
    ALTER TABLE dbo.TransmittalSyncEquivalencia
        ALTER COLUMN CodigoDestino NVARCHAR(50) NOT NULL;
    PRINT 'CodigoDestino ahora NOT NULL.';
END
GO
