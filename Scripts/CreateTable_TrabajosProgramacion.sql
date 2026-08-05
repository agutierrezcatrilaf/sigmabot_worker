-- Tabla TrabajosProgramacion: día y horario en que debe ejecutarse cada trabajo.
-- Asociada a Trabajos. El scheduler consulta esta tabla para saber qué ejecutar
-- y evita repetir si ya existió una ejecución para esa programación (mismo día/hora).

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TrabajosProgramacion')
BEGIN
    CREATE TABLE [dbo].[TrabajosProgramacion] (
        Id          INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdTrabajo   INT             NOT NULL,
        DiaSemana   TINYINT         NOT NULL,  -- 0=Domingo, 1=Lunes, ..., 6=Sábado (igual que .NET DayOfWeek)
        Hora        TIME(0)         NOT NULL,  -- Hora programada (ej. 12:00:00)
        Activo      BIT             NOT NULL DEFAULT 1,
        CONSTRAINT FK_TrabajosProgramacion_Trabajos FOREIGN KEY (IdTrabajo) REFERENCES [dbo].[Trabajos](id)
    );

    CREATE INDEX IX_TrabajosProgramacion_IdTrabajo ON [dbo].[TrabajosProgramacion] (IdTrabajo);
    CREATE INDEX IX_TrabajosProgramacion_DiaHora ON [dbo].[TrabajosProgramacion] (DiaSemana, Hora);

    PRINT 'Tabla TrabajosProgramacion creada.';
END
ELSE
    PRINT 'La tabla TrabajosProgramacion ya existe.';

-- Ejemplo: programar trabajo id=2 para Lunes y Miércoles a las 12:00
/*
INSERT INTO [dbo].[TrabajosProgramacion] (IdTrabajo, DiaSemana, Hora, Activo)
VALUES
  (2, 1, '12:00', 1),  -- Lunes 12:00
  (2, 3, '12:00', 1);  -- Miércoles 12:00
*/
