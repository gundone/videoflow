using Microsoft.AspNetCore.Mvc;
using UploadService.DTOs;
using UploadService.Services;

namespace UploadService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IStorageService _storageService;
    private readonly IEnumerable<string> _allowedFileTypes = [".mp4", ".avi", ".mkv", ".mov", ".webm"];
    private string AllowedTypesString => $"'{string.Join("', '", _allowedFileTypes)}'";

    public UploadController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpPost]
    [RequestSizeLimit(524288000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Content is null or empty");
        }

        if (CheckFileExtension(file.FileName, out var badRequest))
        {
            return badRequest;
        }

        await using var stream = file.OpenReadStream();
        var result = await _storageService.UploadAsync(file.FileName, stream, file.ContentType, ct);
        return Ok(new
        {
            fileId = result.FileId,
            fileName = result.FileName,
            url = result.Url,
            size = result.Size
        });
    }

    [HttpPost("request")]
    public async Task<IActionResult> RequestUpload([FromBody] UploadRequest request, CancellationToken ct = default)
    {
        if (CheckFileExtension(request.FileName, out var notSupportedFileType))
        {
            return notSupportedFileType;
        }

        var result = await _storageService.GeneratePresignedUploadUrlAsync(request.FileName, request.ContentType, ct);
        return Ok(result);
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> RequestConfirm([FromBody] ConfirmRequest request)
    {
        return Ok();
    }

    private bool CheckFileExtension(string filename, out IActionResult notSupportedFileType)
    {
        var ext = Path.GetExtension(filename).ToLowerInvariant();
        notSupportedFileType = BadRequest($"Filetype {ext} is not supported. Supported types are: {AllowedTypesString}");
        return !_allowedFileTypes.Contains(ext);
    }
}