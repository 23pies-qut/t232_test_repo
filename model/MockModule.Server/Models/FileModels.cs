namespace MockModule.Server.Models
{
    /// <summary>One row returned by files_CRUD_JSON 'SELECT'.</summary>
    public record FileRow(string FileId, string Filename, string MimeType, long SizeBytes, string Status, DateTime CreatedUtc, DateTime UpdatedUtc);

    /// <summary>Result of files_CRUD_JSON 'INSERT' / 'DELETE': { "fileId": "..." }.</summary>
    public record FileIdResult(string FileId);

    /// <summary>Response of POST api/Files. Trace lists every server-side step of the upload; the frontend uses it for the storage animation.</summary>
    public record UploadResponse(string FileId, string Filename, long SizeBytes, string Checksum, IReadOnlyList<Step> Trace);

    /// <summary>A decrypted and verified file, returned by the controller as the download.</summary>
    public record DownloadedFile(string Filename, string MimeType, byte[] Content);

    /// <summary>One file_contents row: ciphertext, nonce, auth_tag and the data key wrapped with the master key.</summary>
    public record EncryptedFile(byte[] Ciphertext, byte[] Nonce, byte[] AuthTag, byte[] WrappedDek);

    /// <summary>Response of GET api/Health.</summary>
    public record HealthReport(bool Ok, string Message);
}
