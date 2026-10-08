using IdentityKeyApi.Models;

namespace IdentityKeyApi.Services;

public interface IEncryptionService
{
    EncryptedFileData Encrypt(byte[] plaintext);

    byte[] Decrypt(EncryptedFileData encryptedData);
}