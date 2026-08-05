-- returnFields extra para register/search en CODELCO (supersede vuelta SALFA→Codelco).
-- No mezclar con CamposConsultaRegistroDestinoSalfa (proyecto SALFA).
DECLARE @IdTrabajo INT = 10008;
DECLARE @Nombre   NVARCHAR(100) = N'CamposConsultaRegistroDestino';
DECLARE @Valor    NVARCHAR(MAX) = N'Emisororigen_singleSelect,Especialidad_singleSelect,Fase_singleSelect,Localizador_singleSelect,NroDeContrato_singleSelect,Proveedor_singleSelect,Rendicin_singleSelect,TipoDeDocumento_singleSelect';

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

PRINT 'OK: CamposConsultaRegistroDestino IdTrabajo=10008';
