namespace IdentityKeyApi.Models;

public class FileDownloadData
{
    public string FileName { get; set; } = string.Empty;

    public string MimeType { get; set; } = "application/octet-stream";

    public byte[] Content { get; set; } = [];
}