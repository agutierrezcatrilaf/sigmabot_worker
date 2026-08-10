using System;
using System.Security.Cryptography;

namespace SigmabotSync.Domain.Security
{
    public static class CredencialClaveProtectorFactory
    {
        public static ICredencialClaveProtector CreateOptional(string encryptionKeyBase64)
        {
            if (string.IsNullOrWhiteSpace(encryptionKeyBase64))
                return NullCredencialClaveProtector.Instance;

            return new CredencialClaveProtector(encryptionKeyBase64);
        }

        /// <summary>Genera una clave AES-256 lista para appsettings / settings.json.</summary>
        public static string GenerateEncryptionKeyBase64()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }
    }
}
