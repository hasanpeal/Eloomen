namespace server.Interfaces;

public interface ICloudflareR2Service
{
    Task<string> UploadFileAsync(IFormFile file, string objectKey, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string objectKey, CancellationToken cancellationToken = default);
    Task<string> GetPresignedUrlAsync(string objectKey, int expirationMinutes = 60, CancellationToken cancellationToken = default);
    Task<Stream> GetFileStreamAsync(string objectKey, CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

