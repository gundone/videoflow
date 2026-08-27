using UploadService.DTOs;

namespace UploadService.Services;

public interface IStorageService
{
    Task<StorageResult> UploadAsync(string filename, Stream content, string contentType, CancellationToken ct = default);

    Task<PresignedUploadResult> GeneratePresignedUploadUrlAsync(string fileName, string contentType, CancellationToken ct = default);
}
