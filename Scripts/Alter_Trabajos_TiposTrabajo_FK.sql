-- Vincula Trabajos.Tipo con TiposTrabajo.Codigo.
-- Ejecutar DESPUÉS de CreateTable_TiposTrabajo.sql.
-- Trabajos.Tipo no cambia de nombre ni de significado: sigue siendo el código técnico.

-- 0) Alinear tipos de columna (FK exige mismo tipo, longitud y escala)
--    Instalaciones legado: Trabajos.Tipo suele ser NVARCHAR(50) NOT NULL; TiposTrabajo.Codigo es NVARCHAR(100).
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Trabajos')
BEGIN
    ALTER TABLE [dbo].[Trabajos] ALTER COLUMN [Tipo] NVARCHAR(100) NOT NULL;
    PRINT 'Trabajos.Tipo alineado a NVARCHAR(100) NOT NULL.';
END

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TiposTrabajo')
BEGIN
    ALTER TABLE [dbo].[TiposTrabajo] ALTER COLUMN [Codigo] NVARCHAR(100) NOT NULL;
    PRINT 'TiposTrabajo.Codigo alineado a NVARCHAR(100).';
END

-- 1) Registrar en el catálogo cualquier Tipo ya usado en Trabajos y ausente en TiposTrabajo
INSERT INTO [dbo].[TiposTrabajo] (Codigo, Nombre, Descripcion, Orden, Activo)
SELECT DISTINCT
    LTRIM(RTRIM(t.Tipo)),
    LTRIM(RTRIM(t.Tipo)),
    N'Tipo heredado detectado en Trabajos; revise nombre y descripción.',
    99,
    1
FROM [dbo].[Trabajos] t
WHERE t.Tipo IS NOT NULL
  AND LTRIM(RTRIM(t.Tipo)) <> N''
  AND NOT EXISTS (
      SELECT 1 FROM [dbo].[TiposTrabajo] tt
      WHERE tt.Codigo = LTRIM(RTRIM(t.Tipo))
  );

-- 2) FK (integridad referencial)
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Trabajos_TiposTrabajo'
)
BEGIN
    ALTER TABLE [dbo].[Trabajos] WITH CHECK
    ADD CONSTRAINT FK_Trabajos_TiposTrabajo
        FOREIGN KEY (Tipo) REFERENCES [dbo].[TiposTrabajo] (Codigo);

    PRINT 'FK FK_Trabajos_TiposTrabajo creada.';
END
ELSE
    PRINT 'La FK FK_Trabajos_TiposTrabajo ya existe.';

-- Nota: si Trabajos.Tipo es NULL, SQL Server no valida la FK (NULL permitido).
-- Nuevos trabajos deben usar un Codigo existente y Activo=1 (validado también en la API).

-- Diagnóstico (opcional): ver tipos reales si algo falla
/*
SELECT
    OBJECT_NAME(c.object_id) AS Tabla,
    c.name AS Columna,
    t.name AS TipoSql,
    c.max_length,
    c.collation_name
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE (c.object_id = OBJECT_ID(N'dbo.Trabajos') AND c.name = N'Tipo')
   OR (c.object_id = OBJECT_ID(N'dbo.TiposTrabajo') AND c.name = N'Codigo');
*/
