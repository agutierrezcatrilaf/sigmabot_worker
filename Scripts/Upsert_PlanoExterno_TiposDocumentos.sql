-- Plano Externo (SALFA / Aconex) para ida Codelco → SALFA.
-- Documento Externo ya existe: 1207961277.

DECLARE @Instancia NVARCHAR(100) = N'us1.aconex.com';
DECLARE @Nombre    NVARCHAR(200) = N'Plano Externo';
DECLARE @IdTipo    NVARCHAR(50)  = N'1207961528';

IF NOT EXISTS (SELECT 1 FROM TiposDocumentos WHERE idTipo = @IdTipo)
BEGIN
    INSERT INTO TiposDocumentos (Instancia, Nombre, idTipo)
    VALUES (@Instancia, @Nombre, @IdTipo);
    PRINT 'Insertado TiposDocumentos: ' + @Nombre + ' = ' + @IdTipo;
END
ELSE
BEGIN
    UPDATE TiposDocumentos
    SET Instancia = @Instancia, Nombre = @Nombre
    WHERE idTipo = @IdTipo;
    PRINT 'Actualizado TiposDocumentos: ' + @Nombre + ' = ' + @IdTipo;
END
GO
