-- returnFields extra para register/search en SALFA (supersede ida Codelco→SALFA).
-- Solo nombres de project fields SALFA. No incluir campos Codelco (Proveedor_singleSelect, etc.).
-- La homologación ida ya aporta CampoDestino; aquí solo extras que falten en pruebas.
DECLARE @IdTrabajo INT = 10008;
DECLARE @Nombre   NVARCHAR(100) = N'CamposConsultaRegistroDestinoSalfa';
-- Vacío hasta que Aconex reclame un mandatory no cubierto por schema + homologación.
DECLARE @Valor    NVARCHAR(MAX) = N'';

IF NOT EXISTS (
    SELECT 1 FROM TrabajosConfiguracion
    WHERE idTrabajo = @IdTrabajo AND Nombre = @Nombre)
BEGIN
    INSERT INTO TrabajosConfiguracion (idTrabajo, Nombre, ValorTexto)
    VALUES (@IdTrabajo, @Nombre, @Valor);
END
ELSE
BEGIN
    UPDATE TrabajosConfiguracion
    SET ValorTexto = @Valor
    WHERE idTrabajo = @IdTrabajo AND Nombre = @Nombre;
END

PRINT 'OK: CamposConsultaRegistroDestinoSalfa IdTrabajo=10008';
