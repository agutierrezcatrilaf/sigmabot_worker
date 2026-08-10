using System;
using System.Security.Cryptography;
using System.Text;

namespace SigmabotSync.Domain.Security
{
    /// <summary>
    /// AES-256-GCM. Valores almacenados con prefijo <c>enc:v1:</c> + Base64(nonce|tag|ciphertext).
    /// </summary>
    public sealed class CredencialClaveProtector : ICredencialClaveProtector
    {
        public const string Prefix = "enc:v1:";

        private readonly byte[] _key;

        public bool IsEnabled => true;

        public CredencialClaveProtector(string encryptionKeyBase64)
        {
            if (string.IsNullOrWhiteSpace(encryptionKeyBase64))
                throw new ArgumentException("La clave de cifrado no puede estar vacía.", nameof(encryptionKeyBase64));

            _key = Convert.FromBase64String(encryptionKeyBase64.Trim());
            if (_key.Length != 32)
                throw new ArgumentException(
                    "Credenciales:EncryptionKey debe ser 32 bytes codificados en Base64 (AES-256).",
                    nameof(encryptionKeyBase64));
        }

        public string Protect(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
                return plaintext;

            if (plaintext.StartsWith(Prefix, StringComparison.Ordinal))
                return plaintext;

            var plainBytes = Encoding.UTF8.GetBytes(plaintext);
            var nonce = new byte[12];
            RandomNumberGenerator.Fill(nonce);
            var cipher = new byte[plainBytes.Length];
            var tag = new byte[16];

            using (var aes = new AesGcm(_key, tag.Length))
            {
                aes.Encrypt(nonce, plainBytes, cipher, tag);
            }

            var payload = new byte[nonce.Length + tag.Length + cipher.Length];
            Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipher, 0, payload, nonce.Length + tag.Length, cipher.Length);

            return Prefix + Convert.ToBase64String(payload);
        }

        public string Unprotect(string stored)
        {
            if (string.IsNullOrEmpty(stored))
                return stored;

            if (!stored.StartsWith(Prefix, StringComparison.Ordinal))
                return stored;

            var payload = Convert.FromBase64String(stored.Substring(Prefix.Length));
            if (payload.Length < 28)
                throw new InvalidOperationException("Valor cifrado de credencial inválido o corrupto.");

            var nonce = new byte[12];
            var tag = new byte[16];
            var cipher = new byte[payload.Length - 28];
            Buffer.BlockCopy(payload, 0, nonce, 0, nonce.Length);
            Buffer.BlockCopy(payload, 12, tag, 0, tag.Length);
            Buffer.BlockCopy(payload, 28, cipher, 0, cipher.Length);

            var plain = new byte[cipher.Length];
            using (var aes = new AesGcm(_key, tag.Length))
            {
                aes.Decrypt(nonce, cipher, tag, plain);
            }

            return Encoding.UTF8.GetString(plain);
        }
    }
}
