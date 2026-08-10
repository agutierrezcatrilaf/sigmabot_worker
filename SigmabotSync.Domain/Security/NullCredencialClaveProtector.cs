namespace SigmabotSync.Domain.Security
{
    /// <summary>Sin cifrado (desarrollo o migración). Las claves se guardan y leen en texto plano.</summary>
    public sealed class NullCredencialClaveProtector : ICredencialClaveProtector
    {
        public static readonly NullCredencialClaveProtector Instance = new NullCredencialClaveProtector();

        public bool IsEnabled => false;

        public string Protect(string plaintext) => plaintext;

        public string Unprotect(string stored) => stored;
    }
}
