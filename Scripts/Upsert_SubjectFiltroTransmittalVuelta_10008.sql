-- Vuelta SALFA→Codelco: solo transmitals cuyo Subject contenga "Final".
-- Ejemplo: Final (WF-000002) Prueba 2

DECLARE @IdTrabajo INT = 10008;
DECLARE @Filtro NVARCHAR(100) = N'Final';

IF NOT EXISTS (
    SELECT 1 FROM TrabajosConfiguracion
    WHERE idTrabajo = @IdTrabajo AND Nombre = N'SubjectFiltroTransmittalVuelta')
BEGIN
    INSERT INTO TrabajosConfiguracion (idTrabajo, Nombre, ValorTexto)
    VALUES (@IdTrabajo, N'SubjectFiltroTransmittalVuelta', @Filtro);
END
ELSE
BEGIN
    UPDATE TrabajosConfiguracion
    SET ValorTexto = @Filtro
    WHERE idTrabajo = @IdTrabajo AND Nombre = N'SubjectFiltroTransmittalVuelta';
END

PRINT 'SubjectFiltroTransmittalVuelta=' + @Filtro + ' para IdTrabajo=' + CAST(@IdTrabajo AS VARCHAR(20));
