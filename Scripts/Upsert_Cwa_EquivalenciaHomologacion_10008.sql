-- Homologación equivalencias Codelco → SALFA (IdTrabajo 10008).
-- CWA: origen WBS = Localizador_singleSelect (Codelco) → Cwa_singleSelect (SALFA).
-- TipoDocumento: sin ValorDefault (sin fallback IAD si no hay equivalencia).

DECLARE @IdTrabajo INT = 10008;
DECLARE @Codelco   NVARCHAR(50) = '1207996652';
DECLARE @Salfa     NVARCHAR(50) = '1207996803';

UPDATE TransmittalSyncCampoProyecto
SET CampoOrigen = N'Localizador_singleSelect',
    ValorDefault = N'General',
    Catalogo = N'EquivalenciaCwa'
WHERE IdTrabajo = @IdTrabajo
  AND ACXProjectIdOrigen = @Codelco
  AND ACXProjectIdDestino = @Salfa
  AND Campo = N'Cwa_singleSelect';

IF @@ROWCOUNT = 0
BEGIN
    INSERT INTO TransmittalSyncCampoProyecto
        (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, Campo, CampoOrigen, EsObligatorio, ValorDefault, Catalogo, Orden)
    VALUES
    (@IdTrabajo, @Codelco, @Salfa, N'Cwa_singleSelect', N'Localizador_singleSelect', 1, N'General', N'EquivalenciaCwa', 110);
END

UPDATE TransmittalSyncCampoProyecto
SET ValorDefault = NULL
WHERE IdTrabajo = @IdTrabajo
  AND ACXProjectIdOrigen = @Codelco
  AND ACXProjectIdDestino = @Salfa
  AND Campo = N'TipoDeDocumento_singleSelect';

PRINT 'OK: homologación equivalencias IdTrabajo=10008 (CWA←Localizador, TipoDocumento sin default).';
