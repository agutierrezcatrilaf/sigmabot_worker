-- Tabla Credenciales: almacena credenciales para Aconex (Tipo='Aconex') y para la BD de documentos (Tipo='BD').
-- La aplicación lee desde aquí; en settings.json solo se configura la conexión a la BD donde está esta tabla.

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Credenciales')
BEGIN
    CREATE TABLE Credenciales (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Nombre NVARCHAR(200) NULL,
        Tipo NVARCHAR(50) NOT NULL,
        -- Aconex (usado cuando Tipo = 'Aconex')
        Aconex_Instancia NVARCHAR(200) NULL,
        Aconex_Usuario NVARCHAR(200) NULL,
        Aconex_Clave NVARCHAR(500) NULL,
        Aconex_IntegrationId NVARCHAR(200) NULL,
        Aconex_OrganizationId NVARCHAR(100) NULL,
        Aconex_UserId NVARCHAR(100) NULL,
        -- BD (usado cuando Tipo = 'BD')
        BD_Servidor NVARCHAR(200) NULL,
        BD_TipoConexion NVARCHAR(50) NULL,
        BD_Usuario NVARCHAR(200) NULL,
        BD_Clave NVARCHAR(500) NULL,
        BD_BaseDatos NVARCHAR(200) NULL
    );

    -- Ejemplo: registro Aconex
    -- INSERT INTO Credenciales (Nombre, Tipo, Aconex_Instancia, Aconex_Usuario, Aconex_Clave, Aconex_IntegrationId, Aconex_OrganizationId, Aconex_UserId, BD_Servidor, BD_TipoConexion, BD_Usuario, BD_Clave, BD_BaseDatos)
    -- VALUES ('AconexSalfa', 'Aconex', 'us1.aconex.com', 'usuario', 'clave', 'integration-id', '1207961395', '1208669376', NULL, NULL, NULL, NULL, NULL);

    -- Ejemplo: registro BD (metadata de documentos)
    -- INSERT INTO Credenciales (Nombre, Tipo, Aconex_Instancia, Aconex_Usuario, Aconex_Clave, Aconex_IntegrationId, Aconex_OrganizationId, Aconex_UserId, BD_Servidor, BD_TipoConexion, BD_Usuario, BD_Clave, BD_BaseDatos)
    -- VALUES ('BdSalfa', 'BD', NULL, NULL, NULL, NULL, NULL, NULL, '155.254.24.155', 'SQL', 'sigmatecuser', 'password', 'sigmabot');
END
GO
