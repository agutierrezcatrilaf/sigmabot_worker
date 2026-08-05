-- Reemplaza ResolverPicklist por Catalogo (tabla paramétrica explícita).

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TransmittalSyncCampoProyecto')
BEGIN
    PRINT 'Tabla TransmittalSyncCampoProyecto no existe.';
    RETURN;
END

IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID('TransmittalSyncCampoProyecto') AND name = 'Catalogo')
BEGIN
    ALTER TABLE TransmittalSyncCampoProyecto ADD Catalogo NVARCHAR(100) NULL;
    PRINT 'Columna Catalogo agregada.';
END

IF EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID('TransmittalSyncCampoProyecto') AND name = 'ResolverPicklist')
BEGIN
    UPDATE TransmittalSyncCampoProyecto SET Catalogo = 'EstatusDocumentos'
    WHERE ResolverPicklist = 1 AND Campo = 'DocumentStatusId' AND Catalogo IS NULL;

    UPDATE TransmittalSyncCampoProyecto SET Catalogo = 'TiposDocumentos'
    WHERE ResolverPicklist = 1 AND Campo = 'DocumentTypeId' AND Catalogo IS NULL;

    -- ReviewStatusId: texto directo (ej. Ninguno), sin catálogo paramétrico.

    DECLARE @df NVARCHAR(200);
    SELECT @df = name FROM sys.default_constraints
    WHERE parent_object_id = OBJECT_ID('TransmittalSyncCampoProyecto')
      AND parent_column_id = (SELECT column_id FROM sys.columns
          WHERE object_id = OBJECT_ID('TransmittalSyncCampoProyecto') AND name = 'ResolverPicklist');

    IF @df IS NOT NULL
        EXEC('ALTER TABLE TransmittalSyncCampoProyecto DROP CONSTRAINT ' + @df);

    ALTER TABLE TransmittalSyncCampoProyecto DROP COLUMN ResolverPicklist;
    PRINT 'Columna ResolverPicklist eliminada (datos migrados a Catalogo).';
END
ELSE
    PRINT 'ResolverPicklist ya no existe; revise Catalogo en filas existentes.';
