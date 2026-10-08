using IdentityKeyApi.Models;

namespace IdentityKeyApi.Services;

public interface IFileService
{
    Task<List<FileMetadata>> GetFilesAsync();

    Task<FileUploadResult> UploadFileAsync(
        string fileName,
        string mimeType,
        byte[] content
    );

    Task<FileDownloadData?> DownloadFileAsync(
        string fileId
    );

    Task<bool> DeleteFileAsync(
        string fileId
    );
}