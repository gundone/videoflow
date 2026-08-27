namespace UploadService.DTOs;

public record StorageResult(string FileId, string FileName, string Url, long Size);