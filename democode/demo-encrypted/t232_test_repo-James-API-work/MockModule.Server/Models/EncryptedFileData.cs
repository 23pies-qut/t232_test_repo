namespace MockModule.Server.Models;

// One encrypted file, matching the columns of dbo.file_contents.
public class EncryptedFileData
{
    public byte[] Ciphertext { get; set; } = [];
    public byte[] Nonce { get; set; } = [];        // 12 bytes  -> nonce VARBINARY(16)
    public byte[] AuthTag { get; set; } = [];      // 16 bytes  -> auth_tag VARBINARY(16)
    public byte[] WrappedDek { get; set; } = [];   // 60 bytes  -> wrapped_dek VARBINARY(512)
    public string PlaintextChecksum { get; set; } = ""; // SHA-256 hex, 64 chars -> VARCHAR(128)
}
