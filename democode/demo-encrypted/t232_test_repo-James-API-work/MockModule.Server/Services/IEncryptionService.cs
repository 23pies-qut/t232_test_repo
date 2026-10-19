using MockModule.Server.Models;

namespace MockModule.Server.Services;

public interface IEncryptionService
{
    EncryptedFileData Encrypt(byte[] plaintext);
    byte[] Decrypt(EncryptedFileData encryptedData);
}
