-- CWA equivalencias IdTrabajo 10008 desde HomologacionCWA.xlsx
-- Origen: columna D (WBS Codelco ACONEX) -> Destino: columna F (Descripción CWA)
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

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
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'00000 - GENERAL', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02411 - PISCINAS DECANTADORAS (ARENAS Y AGUAS SERVIDAS) NV. DRENAJE', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02418 - SISTEMA AGUA CONTACTO TUNEL', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03700 - TRANSPORTE DE MINERAL', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03720 - SISTEMA DE CORREA 2', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03724 - AUTOMATIZACION Y CONTROL', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03730 - SISTEMA DE CORREA 3', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03734 - AUTOMATIZACION Y CONTROL', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03750 - SISTEMA DE CORREA 4A', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'06836 - SISTEMA VENTILACION TAP Y TC', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02000 - MINERIA SUBTERRANEA', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'06113 - EDIFICIO MAITENES', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02113 - POLVORIN NV. HUNDIMIENTO', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02183 - SUMINISTRO ENERGIA (SUBESTACION Y RED DE DISTRIBUCION) NV. HUNDIMIENTO', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02200 - NIVEL PRODUCCION', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02230 - BARRIO CIVICO NV. PRODUCCION', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02231 - BARRIO CIVICO (OFICINAS, CASA DE CAMBIO, REFUGIOS PEATONALES, ETC.) NV. PRODUCCION', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02232 - TALLER NV. PRODUCCION', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02251 - CALLES NV. PRODUCCION', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02272 - PUNTOS DE VACIADO NV. PRODUCCION', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02291 - SUMINISTRO AGUA NV. PRODUCCION', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02292 - SUMINISTRO ENERGIA (SUBESTACION Y RED DE DISTRIBUCION) NV. PRODUCCION', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02294 - SUMINISTROS DE COMBUSTIBLES NV. PRODUCCION', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02297 - SISTEMA EXTINCION DE INCENDIOS', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'06335 - TUBERIAS DE ALIMENTACION', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'06342 - CANALIZACION Y COMPONENTES DE RED AGUA CONTRA INCENDIOS', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'70215 - HABILITACION AREAS INSTALACION FAENAS', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02700 - SISTEMAS', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'70233 - CASAS DE CAMBIO Y CASINO', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02371 - SUMINISTRO AGUA NTI', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02310 - INFRAESTRUCTURA NIVEL DE TRANSPORTE', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02314 - AREA MANTENCION (TALLER)Â  NV. TRANSPORTE', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02318 - BARRIO CIVICO (OFICINAS, CASA DE CAMBIO, REFUGIOS PEATONALES, ETC) NV. TRANSPORTE', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02372 - SUMINISTRO ENERGIA NTI', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02374 - SUMINISTROS DE COMBUSTIBLES NTI', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02377 - SISTEMA EXTINCION DE INCENDIOS', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02381 - BUZONES', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02412 - SISTEMA DE DRENAJE NV. DRENAJE', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02491 - SUMINISTRO DE AGUA NV. DRENAJE', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02493 - AUTOMATIZACION Y CONTROL NV. DRENAJE', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02554 - SISTEMA DE EXTINCION DE INCENDIOS', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02556 - SUMINISTRO ENERGIA (SUBESTACION Y RED DE DISTRIBUCION) NV. VENTILACION-EXTRACCION', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02558 - SUMINISTROS DE COMBUSTIBLES NV. VENTILACION', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'02561 - ALMACENAMIENTOÂ  Y DISTRIBUCIÃ“NÂ  COMBUSTIBLE', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03623 - PUNTO DE VACIADO Y PIQUE', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03624 - BUZÃ“N TT8', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03631 - INSTALACIONES VENTILACIÃ“N INYECCIÃ“N/ EXTRACCIÃ“N', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03710 - SISTEMA DE CORREA 1', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03760 - BOVEDA DE TRASPASO CORREA N?4A-CV-11', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03920 - TORRE DE TRANSFERENCIA NÂ°2 EXISTENTE', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'06200 - OBRAS VIALES', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03712 - ESTACION DE TRANSFERENCIA 1', N'CWA-412.0-19'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03713 - DISTRIBUCION DE ENERGIA', N'CWA-412.0-20'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03723 - DISTRIBUCION DE ENERGIA', N'CWA-412.0-27'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03733 - DISTRIBUCION DE ENERGIA', N'CWA-412.0-32'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03763 - DISTRIBUCION DE ENERGIA', N'CWA-412.0-34'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'06838 - SUMINISTRO ELECTRICO', N'CWA-412.0-27'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'03721 - CORREA 2', N'General'),
(@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'06832 - TUNEL DE SERVICIO (CORREA)', N'CWA-412.0-02');

DECLARE @Filas INT;
SELECT @Filas = COUNT(*) FROM TransmittalSyncEquivalencia WHERE IdTrabajo=@IdTrabajo AND Tipo=N'Cwa' AND Activo=1;
PRINT 'OK: Cwa equivalencias cargadas (' + CAST(@Filas AS VARCHAR(20)) + ').';
GO
