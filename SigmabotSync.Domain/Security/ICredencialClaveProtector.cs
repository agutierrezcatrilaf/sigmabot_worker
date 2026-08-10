namespace SigmabotSync.Domain.Security
{
    /// <summary>
    /// Cifra/descifra Aconex_Clave y BD_Clave al persistir en tabla Credenciales.
    /// La clave de cifrado vive en appsettings (API) o settings.json (consola), no en la BD.
    /// </summary>
    public interface ICredencialClaveProtector
    {
        bool IsEnabled { get; }

        string Protect(string plaintext);

        /// <summary>Devuelve texto plano. Valores sin prefijo <c>enc:v1:</c> se tratan como legado (texto plano).</summary>
        string Unprotect(string stored);
    }
}
