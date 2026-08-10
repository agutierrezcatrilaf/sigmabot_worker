namespace SigmabotSync.Domain.Config
{
    /// <summary>
    /// Cifrado de Aconex_Clave y BD_Clave en tabla Credenciales. La clave no se guarda en la BD.
    /// Sección <c>Credenciales</c> en settings.json de la consola (misma clave que API appsettings).
    /// </summary>
    public sealed class CredencialesSettings
    {
        /// <summary>32 bytes en Base64 (AES-256). Vacío = sin cifrado (solo desarrollo).</summary>
        public string EncryptionKey { get; set; }
    }
}
