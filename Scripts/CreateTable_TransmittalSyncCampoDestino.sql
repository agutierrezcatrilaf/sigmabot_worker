-- Matriz ProjectSync centrada en proyecto destino (create + supersede read).

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TransmittalSyncCampoDestino')
BEGIN
    CREATE TABLE [dbo].[TransmittalSyncCampoDestino] (
        Id                  INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdTrabajo           INT             NOT NULL,
        ACXProjectIdOrigen  NVARCHAR(50)    NOT NULL,
        ACXProjectIdDestino NVARCHAR(50)    NOT NULL,
        CampoDestino        NVARCHAR(200)   NOT NULL,
        TipoFuente          NVARCHAR(50)    NOT NULL,
        FuenteValor         NVARCHAR(500)   NULL,
        EsObligatorio       BIT             NOT NULL CONSTRAINT DF_TransmittalSyncCampoDestino_Oblig DEFAULT (1),
        ValorDefault        NVARCHAR(500)   NULL,
        Catalogo            NVARCHAR(100)   NULL,
        Orden               INT             NOT NULL CONSTRAINT DF_TransmittalSyncCampoDestino_Orden DEFAULT (0),
        Activo              BIT             NOT NULL CONSTRAINT DF_TransmittalSyncCampoDestino_Activo DEFAULT (1),
        UpdatedAt           DATETIME2       NOT NULL CONSTRAINT DF_TransmittalSyncCampoDestino_UpdatedAt DEFAULT (SYSUTCDATETIME())
    );

    CREATE UNIQUE INDEX UX_TransmittalSyncCampoDestino_Trabajo_Par_Campo
        ON [dbo].[TransmittalSyncCampoDestino] (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, CampoDestino);

    PRINT 'Tabla TransmittalSyncCampoDestino creada.';
END
ELSE
    PRINT 'La tabla TransmittalSyncCampoDestino ya existe.';
