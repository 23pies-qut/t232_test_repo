using System.Security.Cryptography;
using IdentityKeyApi.Models;

namespace IdentityKeyApi.Services;

public class EncryptionService : IEncryptionService
{
    private const int DekSize = 32;
    private const int NonceSize = 12;
    private const int AuthTagSize = 16;

    private readonly byte[] _keyEncryptionKey;

    public EncryptionService(IConfiguration configuration)
    {
        // The key is never committed to the repository. Deployment reads it
        // from an environment variable; local development from user secrets.
        string? keyEncryptionKey =
            Environment.GetEnvironmentVariable("IDENTITY_KEY_KEK")
            ?? configuration["Encryption:KeyEncryptionKey"];

        if (string.IsNullOrWhiteSpace(keyEncryptionKey))
        {
            throw new InvalidOperationException(
                "No encryption key found. Set the IDENTITY_KEY_KEK "
                + "environment variable, or store one with: "
                + "dotnet user-secrets set \"Encryption:KeyEncryptionKey\" "
                + "\"<base64 key>\""
            );
        }

        try
        {
            _keyEncryptionKey =
                Convert.FromBase64String(keyEncryptionKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "Encryption key must be a valid Base64 string.",
                exception
            );
        }

        if (_keyEncryptionKey.Length != 32)
        {
            throw new InvalidOperationException(
                "Encryption key must be exactly 32 bytes."
            );
        }
    }

    public EncryptedFileData Encrypt(byte[] plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        byte[] dataEncryptionKey =
            RandomNumberGenerator.GetBytes(DekSize);

        byte[] nonce =
            RandomNumberGenerator.GetBytes(NonceSize);

        byte[] ciphertext =
            new byte[plaintext.Length];

        byte[] authTag =
            new byte[AuthTagSize];

        try
        {
            using AesGcm aesGcm =
                new AesGcm(dataEncryptionKey, AuthTagSize);

            aesGcm.Encrypt(
                nonce,
                plaintext,
                ciphertext,
                authTag
            );

            byte[] wrappedDek =
                WrapDataEncryptionKey(dataEncryptionKey);

            string checksum =
                CalculateChecksum(plaintext);

            return new EncryptedFileData
            {
                Ciphertext = ciphertext,
                Nonce = nonce,
                AuthTag = authTag,
                WrappedDek = wrappedDek,
                PlaintextChecksum = checksum
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(
                dataEncryptionKey
            );
        }
    }

    public byte[] Decrypt(EncryptedFileData encryptedData)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);

        ValidateEncryptedData(encryptedData);

        byte[] dataEncryptionKey =
            UnwrapDataEncryptionKey(
                encryptedData.WrappedDek
            );

        byte[] plaintext =
            new byte[encryptedData.Ciphertext.Length];

        try
        {
            using AesGcm aesGcm =
                new AesGcm(dataEncryptionKey, AuthTagSize);

            aesGcm.Decrypt(
                encryptedData.Nonce,
                encryptedData.Ciphertext,
                encryptedData.AuthTag,
                plaintext
            );

            string calculatedChecksum =
                CalculateChecksum(plaintext);

            if (!string.Equals(
                    calculatedChecksum,
                    encryptedData.PlaintextChecksum,
                    StringComparison.OrdinalIgnoreCase))
            {
                CryptographicOperations.ZeroMemory(
                    plaintext
                );

                throw new CryptographicException(
                    "The decrypted file checksum is invalid."
                );
            }

            return plaintext;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(
                dataEncryptionKey
            );
        }
    }

    private byte[] WrapDataEncryptionKey(
        byte[] dataEncryptionKey)
    {
        Span<byte> wrappingNonce =
            stackalloc byte[NonceSize];

        Span<byte> wrappingTag =
            stackalloc byte[AuthTagSize];

        byte[] wrappedDek =
            new byte[DekSize];

        RandomNumberGenerator.Fill(wrappingNonce);

        using AesGcm aesGcm =
            new AesGcm(_keyEncryptionKey, AuthTagSize);

        aesGcm.Encrypt(
            wrappingNonce,
            dataEncryptionKey,
            wrappedDek,
            wrappingTag
        );

        return
        [
            .. wrappingNonce,
            .. wrappingTag,
            .. wrappedDek
        ];
    }

    private byte[] UnwrapDataEncryptionKey(
        byte[] wrappedDekData)
    {
        int expectedLength =
            NonceSize + AuthTagSize + DekSize;

        if (wrappedDekData.Length != expectedLength)
        {
            throw new CryptographicException(
                "The wrapped data encryption key is invalid."
            );
        }

        ReadOnlySpan<byte> wrappingNonce =
            wrappedDekData.AsSpan(0, NonceSize);

        ReadOnlySpan<byte> wrappingTag =
            wrappedDekData.AsSpan(
                NonceSize,
                AuthTagSize
            );

        ReadOnlySpan<byte> wrappedDek =
            wrappedDekData.AsSpan(
                NonceSize + AuthTagSize,
                DekSize
            );

        byte[] dataEncryptionKey =
            new byte[DekSize];

        using AesGcm aesGcm =
            new AesGcm(_keyEncryptionKey, AuthTagSize);

        aesGcm.Decrypt(
            wrappingNonce,
            wrappedDek,
            wrappingTag,
            dataEncryptionKey
        );

        return dataEncryptionKey;
    }

    private static string CalculateChecksum(
        byte[] content)
    {
        byte[] checksum =
            SHA256.HashData(content);

        return Convert.ToHexString(checksum);
    }

    private static void ValidateEncryptedData(
        EncryptedFileData encryptedData)
    {
        if (encryptedData.Nonce.Length != NonceSize)
        {
            throw new CryptographicException(
                "The AES-GCM nonce is invalid."
            );
        }

        if (encryptedData.AuthTag.Length != AuthTagSize)
        {
            throw new CryptographicException(
                "The AES-GCM authentication tag is invalid."
            );
        }

        if (encryptedData.WrappedDek.Length == 0)
        {
            throw new CryptographicException(
                "The wrapped data encryption key is missing."
            );
        }

        if (string.IsNullOrWhiteSpace(
                encryptedData.PlaintextChecksum))
        {
            throw new CryptographicException(
                "The plaintext checksum is missing."
            );
        }
    }
}