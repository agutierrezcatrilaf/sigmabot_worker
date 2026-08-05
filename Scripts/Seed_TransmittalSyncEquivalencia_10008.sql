-- Equivalencias ProjectSync IdTrabajo 10008 (Codelco → SALFA).
-- Discipline: usar Insert_TransmittalSyncEquivalencia_Discipline_10008.sql (HomologacionDiscipline.xlsx).
-- TipoDocumento: usar Insert_TransmittalSyncEquivalencia_TipoDocumento_10008.sql (HomologacionTipoDocumento.xlsx).
-- Cwa: usar Insert_TransmittalSyncEquivalencia_Cwa_10008.sql (HomologacionCWA.xlsx).
-- Discipline: match EXACTO de ValorOrigen (texto completo desde Aconex).
-- TipoDocumento: ValorOrigen = código; el texto Aconex se resuelve por prefijo al inicio.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

DECLARE @IdTrabajo INT = 10008;
DECLARE @Codelco   NVARCHAR(50) = '1207996652';
DECLARE @Salfa     NVARCHAR(50) = '1207996803';

DECLARE @DestDiagrama NVARCHAR(200) = N'DPI-Diagrama de Procesos E Instrumentaci' + NCHAR(243) + N'n';
DECLARE @DestEspec    NVARCHAR(200) = N'ETT-Especificaci' + NCHAR(243) + N'n T' + NCHAR(233) + N'cnica';

-- Discipline (Especialidad Codelco → Disciplina SALFA)
MERGE TransmittalSyncEquivalencia AS t
USING (VALUES
    (@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'OH - OLEO HIDRAULICA', N'Piping'),
    (@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'CA - CANERIAS',        N'Piping'),
    (@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'AR - ARQUITECTURA',    N'Arquitectura'),
    (@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'EL - ELECTRICIDAD',    N'Electricidad'),
    (@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'ES - ESTRUCTURAL',     N'Estructuras'),
    (@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'CI - CIVIL',           N'Civil')
) AS s (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, Tipo, ValorOrigen, ValorDestino)
ON  t.IdTrabajo = s.IdTrabajo
AND t.ACXProjectIdOrigen = s.ACXProjectIdOrigen
AND t.ACXProjectIdDestino = s.ACXProjectIdDestino
AND t.Tipo = s.Tipo
AND t.ValorOrigen = s.ValorOrigen
WHEN MATCHED THEN
    UPDATE SET ValorDestino = s.ValorDestino, Activo = 1, UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, Tipo, ValorOrigen, ValorDestino)
    VALUES (s.IdTrabajo, s.ACXProjectIdOrigen, s.ACXProjectIdDestino, s.Tipo, s.ValorOrigen, s.ValorDestino);

-- TipoDocumento (Codelco → SALFA). ValorOrigen = código al inicio del texto Aconex (prefijo).
MERGE TransmittalSyncEquivalencia AS t
USING (VALUES
    (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'300CA',  N'PDD-Plano de Detalles'),
    (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'301CA',  N'PDN-Plano de Disposici' + NCHAR(243) + N'n'),
    (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'303CA',  N'PDD-Plano de Detalles'),
    (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'400CA',  N'PDM-Plano de Montaje'),
    (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'500CA',  N'PDD-Plano de Detalles'),
    (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'502CA',  N'ISO-Isom' + NCHAR(233) + N'trico'),
    (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'600CA',  N'PLA-Plano'),
    (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'000OH',  N'EST-Est' + NCHAR(225) + N'ndar'),
    (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'1000OH', N'PDN-Plano de Disposici' + NCHAR(243) + N'n'),
    (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'101OH',  N'PDG-Plano de Disposici' + NCHAR(243) + N'n General'),
    (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'102OH',  N'PDG-Plano de Disposici' + NCHAR(243) + N'n General'),
    (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'103OH',  N'PDD-Plano de Detalles'),
    (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'200OH',  N'PDO-Plano de Dise' + NCHAR(241) + N'o'),
    (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'201OH',  @DestDiagrama),
    (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'206CA',  N'PDD-Plano de Detalles'),
    (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'500AR',  N'PDD-Plano de Detalles'),
    (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'ESPEL',   @DestEspec),
    (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'HDDES',   N'HDD-Hoja de Datos'),
    (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'MNLCI',   N'MAN-Manual'),
    (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'INDCP',   N'IAD-Informe de Avance Diario')
) AS s (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, Tipo, ValorOrigen, ValorDestino)
ON  t.IdTrabajo = s.IdTrabajo
AND t.ACXProjectIdOrigen = s.ACXProjectIdOrigen
AND t.ACXProjectIdDestino = s.ACXProjectIdDestino
AND t.Tipo = s.Tipo
AND t.ValorOrigen = s.ValorOrigen
WHEN MATCHED THEN
    UPDATE SET ValorDestino = s.ValorDestino, Activo = 1, UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, Tipo, ValorOrigen, ValorDestino)
    VALUES (s.IdTrabajo, s.ACXProjectIdOrigen, s.ACXProjectIdDestino, s.Tipo, s.ValorOrigen, s.ValorDestino);

-- Desactivar filas TipoDocumento legacy (texto completo) reemplazadas por código.
UPDATE TransmittalSyncEquivalencia
SET Activo = 0, UpdatedAt = SYSUTCDATETIME()
WHERE IdTrabajo = @IdTrabajo
  AND ACXProjectIdOrigen = @Codelco
  AND ACXProjectIdDestino = @Salfa
  AND Tipo = N'TipoDocumento'
  AND Activo = 1
  AND ValorOrigen IN (
    N'201OH - P & ID',
    N'206CA-PLANOS DE PIEZAS ESPECIALES',
    N'500AR - ESQUEMAS',
    N'ESPEL - ESPECIFICACION',
    N'HDDES - HOJA DE DATOS',
    N'MNLCI - MANUAL',
    N'INDCP - INFORME DIARIO (SOLO PARA ESPECIALIDAD CP)'
  );

SELECT Tipo, ValorOrigen, ValorDestino
FROM TransmittalSyncEquivalencia
WHERE IdTrabajo = @IdTrabajo AND Activo = 1
ORDER BY Tipo, ValorOrigen;

PRINT 'TransmittalSyncEquivalencia: seed IdTrabajo=10008 (Discipline + TipoDocumento).';
GO
