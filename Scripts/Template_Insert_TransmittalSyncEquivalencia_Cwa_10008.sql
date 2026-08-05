-- Plantilla INSERT equivalencias CWA (Codelco → SALFA), IdTrabajo 10008.
-- Tipo en BD: 'Cwa'  |  Catálogo homologación: EquivalenciaCwa
-- Origen Codelco: Localizador_singleSelect (WBS). Destino SALFA: Cwa_singleSelect.
-- ValorOrigen  = columna D Excel (WBS Codelco ACONEX), texto completo.
-- ValorDestino = columna F Excel (Descripción CWA / valor picklist SALFA).
--
-- Recomendado: borrar filas Cwa del par Codelco→Salfa e insertar el totalizado del Excel.

DECLARE @IdTrabajo INT = 10008;
DECLARE @Codelco   NVARCHAR(50) = '1207996652';
DECLARE @Salfa     NVARCHAR(50) = '1207996803';

DELETE FROM TransmittalSyncEquivalencia
WHERE IdTrabajo = @IdTrabajo
  AND ACXProjectIdOrigen = @Codelco
  AND ACXProjectIdDestino = @Salfa
  AND Tipo = N'Cwa';

INSERT INTO TransmittalSyncEquivalencia
    (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, Tipo, ValorOrigen, ValorDestino)
VALUES
-- Ejemplos (completar con el Excel):
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03712 - ESTACION DE TRANSFERENCIA 1', N'CWA-412.0-19'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03713 - DISTRIBUCION DE ENERGIA',      N'CWA-412.0-20'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03721 - CORREA 2',                    N'General');
-- ... agregar el resto de filas del Excel ...

SELECT Tipo, ValorOrigen, ValorDestino
FROM TransmittalSyncEquivalencia
WHERE IdTrabajo = @IdTrabajo AND Tipo = N'Cwa' AND Activo = 1
ORDER BY ValorOrigen;
