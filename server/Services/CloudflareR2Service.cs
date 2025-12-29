using Minio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using server.Interfaces;
using System.Text.RegularExpressions;

namespace server.Services;

public class CloudflareR2Service : ICloudflareR2Service
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private readonly string _accountId;
    private readonly ILogger<CloudflareR2Service> _logger;

    public CloudflareR2Service(IConfiguration configuration, ILogger<CloudflareR2Service> logger)
    {
        _logger = logger;

        _bucketName = configuration["CloudflareR2:BucketName"]
            ?? throw new InvalidOperationException("CloudflareR2:BucketName is not configured");

        var endpoint = configuration["CloudflareR2:Endpoint"]
            ?? throw new InvalidOperationException("CloudflareR2:Endpoint is not configured");

        var accessKeyId = configuration["CloudflareR2:AccessKeyId"]
            ?? throw new InvalidOperationException("CloudflareR2:AccessKeyId is not configured");

        var secretAccessKey = configuration["CloudflareR2:SecretAccessKey"]
            ?? throw new InvalidOperationException("CloudflareR2:SecretAccessKey is not configured");

        endpoint = endpoint.Trim().TrimEnd('/');
        accessKeyId = accessKeyId.Trim();
        secretAccessKey = secretAccessKey.Trim();

        _logger.LogInformation(
            "Initializing Cloudflare R2 service with MinIO | Endpoint: {Endpoint} | Bucket: {Bucket}",
            endpoint, _bucketName
        );

        _logger.LogInformation(
            "Access Key ID: {Preview} | Length: {Length}",
            accessKeyId.Length > 8 ? accessKeyId[..8] + "..." : "***", accessKeyId.Length
        );

        // Extract Account ID from endpoint (for diagnostics only)
        var match = Regex.Match(endpoint, @"https://([a-f0-9]+)\.r2\.cloudflarestorage\.com");
        _accountId = match.Success ? match.Groups[1].Value : "unknown";

        _logger.LogInformation("Detected R2 Account ID: {AccountId}", _accountId);

        // Initialize MinIO client for Cloudflare R2
        var endpointUri = new Uri(endpoint);
        _minioClient = new MinioClient()
            .WithEndpoint(endpointUri.Host, endpointUri.Port == 443 ? 443 : (endpointUri.Port == -1 ? 443 : endpointUri.Port))
            .WithCredentials(accessKeyId, secretAccessKey)
            .WithSSL(endpointUri.Scheme == "https")
            .Build();

        _logger.LogInformation("Cloudflare R2 MinIO client initialized successfully");
    }

    public async Task<string> UploadFileAsync(
        IFormFile file,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting upload to R2: {ObjectKey}, Size: {Size} bytes, ContentType: {ContentType}",
                objectKey, file.Length, file.ContentType);

            // Ensure bucket exists
            var bucketExists = await _minioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_bucketName), 
                cancellationToken);
            
            if (!bucketExists)
            {
                _logger.LogWarning("Bucket {BucketName} does not exist, creating it...", _bucketName);
                await _minioClient.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_bucketName), 
                    cancellationToken);
            }

            // Upload file
            using var stream = file.OpenReadStream();
            await _minioClient.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectKey)
                    .WithStreamData(stream)
                    .WithObjectSize(file.Length)
                    .WithContentType(file.ContentType),
                cancellationToken);

            _logger.LogInformation("Successfully uploaded object {Key} to R2", objectKey);
            return objectKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to R2: {ObjectKey}", objectKey);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _minioClient.RemoveObjectAsync(
                new RemoveObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectKey),
                cancellationToken);
            _logger.LogInformation("Successfully deleted object {Key} from R2", objectKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from R2: {ObjectKey}", objectKey);
            return false;
        }
    }

    public async Task<string> GetPresignedUrlAsync(
        string objectKey,
        int expirationMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = await _minioClient.PresignedGetObjectAsync(
                new PresignedGetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectKey)
                    .WithExpiry(expirationMinutes * 60)); // Convert minutes to seconds
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL for R2: {ObjectKey}", objectKey);
            throw;
        }
    }

    public async Task<Stream> GetFileStreamAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var memoryStream = new MemoryStream();
            await _minioClient.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectKey)
                    .WithCallbackStream(stream =>
                    {
                        stream.CopyTo(memoryStream);
                    }),
                cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file stream from R2: {ObjectKey}", objectKey);
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Testing R2 connection by checking bucket {Bucket}",
                _bucketName
            );

            var exists = await _minioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_bucketName),
                cancellationToken);

            if (exists)
            {
                _logger.LogInformation("R2 connection test successful - bucket {Bucket} exists", _bucketName);
            }
            else
            {
                _logger.LogWarning("Bucket {Bucket} does not exist, but connection is working", _bucketName);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "R2 connection test failed | Message: {Message}",
                ex.Message
            );

            if (ex.Message.Contains("InvalidAccessKeyId") || ex.Message.Contains("Access Denied") || ex.Message.Contains("403"))
            {
                _logger.LogError("═══════════════════════════════════════════════════════════");
                _logger.LogError("R2 AUTHENTICATION ERROR");
                _logger.LogError("═══════════════════════════════════════════════════════════");
                _logger.LogError("The Access Key ID does not exist in Cloudflare R2 records.");
                _logger.LogError("");
                _logger.LogError("TROUBLESHOOTING STEPS:");
                _logger.LogError("1. Verify your endpoint Account ID: {AccountId}", _accountId);
                _logger.LogError("2. Go to Cloudflare Dashboard → R2 → Manage R2 API Tokens");
                _logger.LogError("3. Check which Account ID your API token was created for");
                _logger.LogError("4. If different, create a NEW API token for Account ID: {AccountId}", _accountId);
                _logger.LogError("5. Update appsettings.Development.json with the new credentials");
                _logger.LogError("6. Ensure the token has 'Object Read & Write' permissions");
                _logger.LogError("═══════════════════════════════════════════════════════════");
            }

            return false;
        }
    }
}
