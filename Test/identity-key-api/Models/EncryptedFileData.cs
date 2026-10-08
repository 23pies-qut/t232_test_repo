namespace IdentityKeyApi.Models;

public class EncryptedFileData
{
    public byte[] Ciphertext { get; set; } = [];

    public byte[] Nonce { get; set; } = [];

    public byte[] AuthTag { get; set; } = [];

    public byte[] WrappedDek { get; set; } = [];

    public string PlaintextChecksum { get; set; } = string.Empty;
}