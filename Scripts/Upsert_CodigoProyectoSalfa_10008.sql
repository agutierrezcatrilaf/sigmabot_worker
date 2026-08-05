-- Código proyecto SALFA (primer segmento docno ida Codelco → SALFA). No es el N° contrato Codelco.

DECLARE @IdTrabajo INT = 10008;
DECLARE @Codigo    NVARCHAR(50) = N'10031671';

IF NOT EXISTS (
    SELECT 1 FROM TrabajosConfiguracion
    WHERE idTrabajo = @IdTrabajo AND Nombre = N'CodigoProyectoSalfa')
BEGIN
    INSERT INTO TrabajosConfiguracion (idTrabajo, Nombre, ValorTexto)
    VALUES (@IdTrabajo, N'CodigoProyectoSalfa', @Codigo);
END
ELSE
BEGIN
    UPDATE TrabajosConfiguracion
    SET ValorTexto = @Codigo
    WHERE idTrabajo = @IdTrabajo AND Nombre = N'CodigoProyectoSalfa';
END

PRINT 'CodigoProyectoSalfa=' + @Codigo + ' para IdTrabajo=' + CAST(@IdTrabajo AS VARCHAR(20));
GO
