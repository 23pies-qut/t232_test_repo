namespace IdentityKeyApi.Models;

public class FileMetadata
{
    public string FileId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }
}