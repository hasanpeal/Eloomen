using Minio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using server.Interfaces;
using System.Text.RegularExpressions;

namespace server.Services;

public class S3Service : IS3Service
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private readonly string _accountId;
    private readonly ILogger<S3Service> _logger;

    public S3Service(IConfiguration configuration, ILogger<S3Service> logger)
    {
        _logger = logger;

        _bucketName = configuration["S3:BucketName"]
            ?? throw new InvalidOperationException("S3:BucketName is not configured");

        var endpoint = configuration["S3:Endpoint"]
            ?? throw new InvalidOperationException("S3:Endpoint is not configured");

        var accessKeyId = configuration["S3:AccessKeyId"]
            ?? throw new InvalidOperationException("S3:AccessKeyId is not configured");

        var secretAccessKey = configuration["S3:SecretAccessKey"]
            ?? throw new InvalidOperationException("S3:SecretAccessKey is not configured");

        endpoint = endpoint.Trim().TrimEnd('/');
        accessKeyId = accessKeyId.Trim();
        secretAccessKey = secretAccessKey.Trim();

        _logger.LogInformation(
            "Initializing S3 service with MinIO"
        );

        // Don't log access key details in production

        // Extract Account ID from endpoint (for diagnostics only) - supports both S3 and R2 endpoints
        var match = Regex.Match(endpoint, @"https://([a-f0-9]+)\.r2\.cloudflarestorage\.com");
        if (!match.Success)
        {
            // Try S3 endpoint pattern
            match = Regex.Match(endpoint, @"https?://([^.]+)\.s3\.[^.]+\.amazonaws\.com");
        }
        _accountId = match.Success ? match.Groups[1].Value : "unknown";

        // Initialize MinIO client for S3-compatible storage
        var endpointUri = new Uri(endpoint);
        _minioClient = new MinioClient()
            .WithEndpoint(endpointUri.Host, endpointUri.Port == 443 ? 443 : (endpointUri.Port == -1 ? 443 : endpointUri.Port))
            .WithCredentials(accessKeyId, secretAccessKey)
            .WithSSL(endpointUri.Scheme == "https")
            .Build();

        _logger.LogInformation("S3 MinIO client initialized successfully");
    }

    public async Task<string> UploadFileAsync(
        IFormFile file,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure bucket exists
            var bucketExists = await _minioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_bucketName), 
                cancellationToken);
            
            if (!bucketExists)
            {
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

            return objectKey;
        }
        catch (Exception ex)
        {
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
            return true;
        }
        catch (Exception ex)
        {
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
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _minioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_bucketName),
                cancellationToken);

            if (exists)
            {
                _logger.LogInformation("S3 connection test successful - bucket exists");
            }
            else
            {
                _logger.LogWarning("Bucket does not exist, but connection is working");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "S3 connection test failed"
            );
            return false;
        }
    }
}

