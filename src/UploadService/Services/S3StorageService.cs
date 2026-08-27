using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using UploadService.DTOs;

namespace UploadService.Services;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly IOptions<S3Options> _options;

    public S3StorageService(IAmazonS3 s3, IOptions<S3Options> options)
    {
        _s3 = s3;
        _options = options;
    }


    public async Task<StorageResult> UploadAsync(string filename, Stream content, string contentType, CancellationToken ct = default)
    {
        var fileId = Ulid.NewUlid().ToString();
        var key = $"{fileId}/{filename}";

        var bucket = await GetBucketAsync(0U, ct);

        var response = await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            
        }, ct);

        return new StorageResult(fileId, filename,
            $"{_options.Value.PublicUrl}/{_options.Value.BucketName}/{key}", content.Length);
    }

    public async Task<PresignedUploadResult> GeneratePresignedUploadUrlAsync(string filename, string contentType, CancellationToken ct = default)
    {
        var fileId = Ulid.NewUlid().ToString();
        var key = $"{fileId}/{filename}";
        var bucket = await GetBucketAsync(0U, ct);
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.Value.UploadTimeoutMinutes);
        var response = await _s3.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = bucket.BucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = expiresAt,
            Protocol = Protocol.HTTP
        });

        return new PresignedUploadResult(fileId, response, $"{_options.Value.PublicUrl}/{_options.Value.BucketName}/{key}", expiresAt);
    }

    private async Task<S3Bucket> GetBucketAsync(uint depth = 0, CancellationToken ct = default)
    {
        if (depth > 3)
        {
            throw new InvalidOperationException("Failed to get s3 bucket");
        }

        var bucketsResponse = await _s3.ListBucketsAsync(ct);
        if (bucketsResponse.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException("Failed to list buckets");
        }

        var bucket = bucketsResponse.Buckets?.FirstOrDefault(b => b.BucketName == _options.Value.BucketName);
        if (bucket is null)
        {
            await _s3.PutBucketAsync(_options.Value.BucketName, ct);
            return await GetBucketAsync(++depth, ct);
        }

        return bucket;
    }
}