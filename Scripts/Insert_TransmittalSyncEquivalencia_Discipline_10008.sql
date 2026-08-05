-- Discipline equivalencias IdTrabajo 10008 desde HomologacionDiscipline.xlsx
-- Origen: columna D -> ValorOrigen, columna F -> ValorDestino, columna G -> CodigoDestino (usar Apply_CodigoDestino_From_HomologacionExcel.ps1)
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
  AND Tipo = N'Discipline';

INSERT INTO TransmittalSyncEquivalencia
    (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, Tipo, ValorOrigen, ValorDestino)
VALUES
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'AR - ARQUITECTURA', N'Arquitectura'),
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'AT - AUTOMATIZACION', N'Automatizacion'),
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'BM - BUILDING INFORMATION MODELING', N'BIM'),
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'CA - CANERIAS', N'Piping'),
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'CI - CIVIL', N'Civil'),
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'CL - CALIDAD', N'Calidad'),
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'CO - TELECOMUNICACIONES', N'Instrumentacion y Control'),
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'EL - ELECTRICIDAD', N'Electricidad'),
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'ES - ESTRUCTURAL', N'Estructuras'),
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'GE - GEOLOGIA', N'Geotecnia'),
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'GM - GEOMENSURA', N'Movimiento de Tierra'),
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'HD - HIDRAULICA', N'Hidraulica'),
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'II - INFORMATICA INDUSTRIAL', N'Instrumentacion y Control'),
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'MD - MULTIDISCIPLINA', N'Multidisciplina'),
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'ME - MECANICA', N'Mecanica'),
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'MI - MINERIA', N'Mineria'),
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'MS - MECANICA DE SUELOS', N'Civil'),
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'OH - OLEO HIDRAULICA', N'Piping'),
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'PM - PUESTA EN MARCHA', N'PEM'),
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'SA - SANITARIA', N'Multidisciplina'),
(@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'VC - VENTILACION - CLIMATIZACION (HVAC)', N'HVAC');

SELECT COUNT(*) AS DisciplineActivos
FROM TransmittalSyncEquivalencia
WHERE IdTrabajo = @IdTrabajo AND Tipo = N'Discipline' AND Activo = 1;
