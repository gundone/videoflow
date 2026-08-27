namespace UploadService.DTOs;

public record PresignedUploadResult(string FileId, string UploadUrl, string PublicUrl, DateTime ExpiresAt);