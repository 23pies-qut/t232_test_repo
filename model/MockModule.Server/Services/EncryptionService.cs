using System.Security.Cryptography;
using System.Text;
using MockModule.Server.Models;

namespace MockModule.Server.Services
{
    /// <summary>
    /// Envelope encryption (AES-256-GCM); the output maps to the file_contents columns:
    ///   ciphertext / nonce / auth_tag   the file encrypted with the data encryption key (DEK)
    ///   wrapped_dek                     the DEK encrypted with the master key: [nonce 12][tag 16][ciphertext 32] = 60 bytes
    /// Every file gets a random DEK; the master key lives only in configuration (user-secrets) and never in the database.
    /// AAD binds the ciphertext to tenant + file id: copied to another row or tenant, decryption fails.
    /// </summary>
    public class EncryptionService
    {
        public const string MasterKeySetting = "MockModuleSecurity:MasterKeyBase64";
        private const int KeySize = 32, NonceSize = 12, TagSize = 16;
        private readonly byte[] _masterKey;

        public EncryptionService(IConfiguration configuration)
        {
            _masterKey = Convert.FromBase64String(configuration[MasterKeySetting] ?? throw new InvalidOperationException($"{MasterKeySetting} is missing."));
            if (_masterKey.Length != KeySize)
                throw new InvalidOperationException($"{MasterKeySetting} must be 32 bytes (Base64).");
        }

        public EncryptedFile Encrypt(byte[] plaintext, string tenantId, string fileId)
        {
            var dek = RandomNumberGenerator.GetBytes(KeySize);
            try
            {
                var (ciphertext, nonce, tag) = Seal(dek, plaintext, Aad("content", tenantId, fileId));
                var (keyCipher, keyNonce, keyTag) = Seal(_masterKey, dek, Aad("dek", tenantId, fileId));
                return new EncryptedFile(ciphertext, nonce, tag, [.. keyNonce, .. keyTag, .. keyCipher]);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(dek); // do not leave the key in memory
            }
        }

        /// <summary>Any change (ciphertext, nonce, tag, or a different tenant / file id) throws CryptographicException.</summary>
        public byte[] Decrypt(EncryptedFile file, string tenantId, string fileId)
        {
            var w = file.WrappedDek;
            if (w.Length != NonceSize + TagSize + KeySize)
                throw new CryptographicException("wrapped_dek has an unexpected length.");
            var dek = Open(_masterKey, w[(NonceSize + TagSize)..], w[..NonceSize], w[NonceSize..(NonceSize + TagSize)], Aad("dek", tenantId, fileId));
            try
            {
                return Open(dek, file.Ciphertext, file.Nonce, file.AuthTag, Aad("content", tenantId, fileId));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(dek);
            }
        }

        private static (byte[] Ciphertext, byte[] Nonce, byte[] Tag) Seal(byte[] key, byte[] plaintext, byte[] aad)
        {
            var nonce = RandomNumberGenerator.GetBytes(NonceSize); // never reuse a nonce with the same key
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TagSize];
            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, aad);
            return (ciphertext, nonce, tag);
        }

        private static byte[] Open(byte[] key, byte[] ciphertext, byte[] nonce, byte[] tag, byte[] aad)
        {
            if (nonce.Length != NonceSize || tag.Length != TagSize)
                throw new CryptographicException("Nonce or tag has an unexpected length.");
            var plaintext = new byte[ciphertext.Length];
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, aad);
            return plaintext;
        }

        private static byte[] Aad(string purpose, string tenantId, string fileId) =>
            Encoding.UTF8.GetBytes($"{purpose}|{SqlConnectionFactory.NormalizeId(tenantId)}|{SqlConnectionFactory.NormalizeId(fileId)}");
    }
}
