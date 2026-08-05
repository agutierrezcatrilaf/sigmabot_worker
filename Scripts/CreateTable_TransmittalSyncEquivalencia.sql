-- Equivalencias Codelco → SALFA para campos project (Discipline, TipoDocumento, etc.).
-- Tipo: 'Discipline' | 'TipoDocumento' (extensible).
-- Uso: Lookup ValorOrigen (del origen) → ValorDestino (para register destino).

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TransmittalSyncEquivalencia')
BEGIN
    CREATE TABLE [dbo].[TransmittalSyncEquivalencia] (
        Id                   INT             IDENTITY(1,1) NOT NULL CONSTRAINT PK_TransmittalSyncEquivalencia PRIMARY KEY,
        IdTrabajo            INT             NOT NULL,
        ACXProjectIdOrigen   NVARCHAR(50)    NOT NULL,
        ACXProjectIdDestino  NVARCHAR(50)    NOT NULL,
        Tipo                 NVARCHAR(50)    NOT NULL,  -- Discipline | TipoDocumento | Cwa
        ValorOrigen          NVARCHAR(200)   NOT NULL,
        ValorDestino         NVARCHAR(200)   NOT NULL,
        CodigoDestino        NVARCHAR(50)    NOT NULL,
        Activo               BIT             NOT NULL CONSTRAINT DF_TransmittalSyncEquivalencia_Activo DEFAULT (1),
        UpdatedAt            DATETIME2       NOT NULL CONSTRAINT DF_TransmittalSyncEquivalencia_UpdatedAt DEFAULT (SYSUTCDATETIME())
    );

    PRINT 'Tabla TransmittalSyncEquivalencia creada.';
END
ELSE
    PRINT 'La tabla TransmittalSyncEquivalencia ya existe.';
GO

SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TransmittalSyncEquivalencia')
   AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_TransmittalSyncEquivalencia_Lookup' AND object_id = OBJECT_ID('TransmittalSyncEquivalencia'))
BEGIN
    CREATE UNIQUE INDEX UX_TransmittalSyncEquivalencia_Lookup
        ON [dbo].[TransmittalSyncEquivalencia] (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, Tipo, ValorOrigen)
        WHERE Activo = 1;
    PRINT 'Índice UX_TransmittalSyncEquivalencia_Lookup creado.';
END
GO

-- Seed ejemplo IdTrabajo 10008 (Codelco → SALFA). Completar filas reales luego.
DECLARE @IdTrabajo INT = 10008;
DECLARE @Codelco   NVARCHAR(50) = '1207996652';
DECLARE @Salfa     NVARCHAR(50) = '1207996803';

IF NOT EXISTS (
    SELECT 1 FROM TransmittalSyncEquivalencia
    WHERE IdTrabajo = @IdTrabajo AND Tipo = 'TipoDocumento'
      AND ValorOrigen = N'INDCP - INFORME DIARIO (SOLO PARA ESPECIALIDAD CP)')
BEGIN
    INSERT INTO TransmittalSyncEquivalencia
        (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, Tipo, ValorOrigen, ValorDestino, CodigoDestino)
    VALUES
    (@IdTrabajo, @Codelco, @Salfa, 'TipoDocumento',
     N'INDCP - INFORME DIARIO (SOLO PARA ESPECIALIDAD CP)',
     N'IAD-Informe de Avance Diario',
     N'IAD');
END

PRINT 'TransmittalSyncEquivalencia: seed mínimo TipoDocumento listo.';
GO
