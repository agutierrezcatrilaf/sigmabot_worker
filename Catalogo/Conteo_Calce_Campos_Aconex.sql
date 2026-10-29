-- Filas de Aconex_DataLake.dbo.DocumentosMetadata que calzan
-- con los valores disponibles en Aconex (Campos de Proyectos.xlsx).
-- La coincidencia es exacta: distingue mayúsculas y acentos.
-- PWP de Aconex se evalúa contra la columna FWP.

USE Aconex_DataLake;
GO

/* 1) Por campo: cuántas filas calzan, no calzan o vienen sin valor. */
SELECT
    v.Campo,
    COUNT(*) AS TotalFilas,
    SUM(CASE WHEN v.Calza = 1 THEN 1 ELSE 0 END) AS Calzan,
    SUM(CASE WHEN v.Calza = 0 AND v.TieneValor = 1 THEN 1 ELSE 0 END) AS NoCalzan,
    SUM(CASE WHEN v.TieneValor = 0 THEN 1 ELSE 0 END) AS SinValor,
    CAST(100.0 * SUM(CASE WHEN v.Calza = 1 THEN 1 ELSE 0 END) / NULLIF(COUNT(*), 0) AS decimal(6, 2)) AS PctCalzan
FROM dbo.DocumentosMetadata AS d
CROSS APPLY (VALUES
    (N'CWA',
        CASE WHEN LTRIM(RTRIM(d.CWA)) COLLATE Latin1_General_BIN2 IN (
            N'CWA-412.0-02', N'CWA-412.0-18', N'CWA-412.0-19', N'CWA-412.0-20',
            N'CWA-412.0-27', N'CWA-412.0-28', N'CWA-412.0-32', N'CWA-412.0-34',
            N'CWA-412.0-36', N'CWA-412.0-38', N'General'
        ) THEN 1 ELSE 0 END,
        CASE WHEN NULLIF(LTRIM(RTRIM(d.CWA)), N'') IS NULL THEN 0 ELSE 1 END),
    (N'CWP',
        CASE WHEN LTRIM(RTRIM(d.CWP)) COLLATE Latin1_General_BIN2 IN (N'CWP00') THEN 1 ELSE 0 END,
        CASE WHEN NULLIF(LTRIM(RTRIM(d.CWP)), N'') IS NULL THEN 0 ELSE 1 END),
    (N'EWP',
        CASE WHEN LTRIM(RTRIM(d.EWP)) COLLATE Latin1_General_BIN2 IN (N'SIN EWP') THEN 1 ELSE 0 END,
        CASE WHEN NULLIF(LTRIM(RTRIM(d.EWP)), N'') IS NULL THEN 0 ELSE 1 END),
    (N'PWP',
        CASE WHEN LTRIM(RTRIM(d.FWP)) COLLATE Latin1_General_BIN2 IN (N'PWP00') THEN 1 ELSE 0 END,
        CASE WHEN NULLIF(LTRIM(RTRIM(d.FWP)), N'') IS NULL THEN 0 ELSE 1 END),
    (N'CMA',
        CASE WHEN LTRIM(RTRIM(d.CMA)) COLLATE Latin1_General_BIN2 IN (
            N'3731', N'3700', N'CMA00', N'GENERAL', N'TBD'
        ) THEN 1 ELSE 0 END,
        CASE WHEN NULLIF(LTRIM(RTRIM(d.CMA)), N'') IS NULL THEN 0 ELSE 1 END),
    (N'Discipline',
        CASE WHEN LTRIM(RTRIM(d.Discipline)) COLLATE Latin1_General_BIN2 IN (
            N'General', N'BIM', N'Bodega', N'Calidad', N'Correspondencia', N'Contrato',
            N'Costos', N'Desarrollo e Innovacion', N'Equipos', N'Ingresos', N'Legal',
            N'Materiales', N'Mano de Obra', N'Permisos', N'Planificacion', N'Productividad',
            N'Relaciones Laborales', N'SSOMA', N'Subcontrato', N'Multidisciplina',
            N'Arquitectura', N'Automatizacion', N'Civil', N'Electricidad', N'Estructuras',
            N'Geotecnia', N'HVAC', N'Instrumentacion y Control', N'Mecanica', N'Mineria',
            N'Movimiento de Tierra', N'Piping', N'Rigging', N'Sanitario', N'Soldadura',
            N'Lean Construction', N'Advanced Work Packaging', N'Riesgos', N'Sostenibilidad',
            N'Hidraulica', N'PEM'
        ) THEN 1 ELSE 0 END,
        CASE WHEN NULLIF(LTRIM(RTRIM(d.Discipline)), N'') IS NULL THEN 0 ELSE 1 END)
) AS v(Campo, Calza, TieneValor)
GROUP BY v.Campo
ORDER BY v.Campo;
GO

/* 2) Cuántas filas traen los seis campos dentro de lo disponible en Aconex. */
SELECT
    COUNT(*) AS TotalFilas,
    SUM(CASE
            WHEN LTRIM(RTRIM(d.CWA)) COLLATE Latin1_General_BIN2 IN (
                    N'CWA-412.0-02', N'CWA-412.0-18', N'CWA-412.0-19', N'CWA-412.0-20',
                    N'CWA-412.0-27', N'CWA-412.0-28', N'CWA-412.0-32', N'CWA-412.0-34',
                    N'CWA-412.0-36', N'CWA-412.0-38', N'General')
             AND LTRIM(RTRIM(d.CWP)) COLLATE Latin1_General_BIN2 IN (N'CWP00')
             AND LTRIM(RTRIM(d.EWP)) COLLATE Latin1_General_BIN2 IN (N'SIN EWP')
             AND LTRIM(RTRIM(d.FWP)) COLLATE Latin1_General_BIN2 IN (N'PWP00')
             AND LTRIM(RTRIM(d.CMA)) COLLATE Latin1_General_BIN2 IN (N'3731', N'3700', N'CMA00', N'GENERAL', N'TBD')
             AND LTRIM(RTRIM(d.Discipline)) COLLATE Latin1_General_BIN2 IN (
                    N'General', N'BIM', N'Bodega', N'Calidad', N'Correspondencia', N'Contrato',
                    N'Costos', N'Desarrollo e Innovacion', N'Equipos', N'Ingresos', N'Legal',
                    N'Materiales', N'Mano de Obra', N'Permisos', N'Planificacion', N'Productividad',
                    N'Relaciones Laborales', N'SSOMA', N'Subcontrato', N'Multidisciplina',
                    N'Arquitectura', N'Automatizacion', N'Civil', N'Electricidad', N'Estructuras',
                    N'Geotecnia', N'HVAC', N'Instrumentacion y Control', N'Mecanica', N'Mineria',
                    N'Movimiento de Tierra', N'Piping', N'Rigging', N'Sanitario', N'Soldadura',
                    N'Lean Construction', N'Advanced Work Packaging', N'Riesgos', N'Sostenibilidad',
                    N'Hidraulica', N'PEM')
            THEN 1 ELSE 0
        END) AS LosSeisCalzan,
    SUM(CASE
            WHEN LTRIM(RTRIM(d.CWA)) COLLATE Latin1_General_BIN2 IN (
                    N'CWA-412.0-02', N'CWA-412.0-18', N'CWA-412.0-19', N'CWA-412.0-20',
                    N'CWA-412.0-27', N'CWA-412.0-28', N'CWA-412.0-32', N'CWA-412.0-34',
                    N'CWA-412.0-36', N'CWA-412.0-38', N'General')
             AND LTRIM(RTRIM(d.CWP)) COLLATE Latin1_General_BIN2 IN (N'CWP00')
             AND LTRIM(RTRIM(d.EWP)) COLLATE Latin1_General_BIN2 IN (N'SIN EWP')
             AND LTRIM(RTRIM(d.FWP)) COLLATE Latin1_General_BIN2 IN (N'PWP00')
             AND LTRIM(RTRIM(d.CMA)) COLLATE Latin1_General_BIN2 IN (N'3731', N'3700', N'CMA00', N'GENERAL', N'TBD')
             AND LTRIM(RTRIM(d.Discipline)) COLLATE Latin1_General_BIN2 IN (
                    N'General', N'BIM', N'Bodega', N'Calidad', N'Correspondencia', N'Contrato',
                    N'Costos', N'Desarrollo e Innovacion', N'Equipos', N'Ingresos', N'Legal',
                    N'Materiales', N'Mano de Obra', N'Permisos', N'Planificacion', N'Productividad',
                    N'Relaciones Laborales', N'SSOMA', N'Subcontrato', N'Multidisciplina',
                    N'Arquitectura', N'Automatizacion', N'Civil', N'Electricidad', N'Estructuras',
                    N'Geotecnia', N'HVAC', N'Instrumentacion y Control', N'Mecanica', N'Mineria',
                    N'Movimiento de Tierra', N'Piping', N'Rigging', N'Sanitario', N'Soldadura',
                    N'Lean Construction', N'Advanced Work Packaging', N'Riesgos', N'Sostenibilidad',
                    N'Hidraulica', N'PEM')
            THEN 0 ELSE 1
        END) AS AlgunoNoCalza
FROM dbo.DocumentosMetadata AS d;
GO
