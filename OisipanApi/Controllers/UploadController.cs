using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Oishipan.Services;

namespace Oishipan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IWebHostEnvironment _environment;
    private readonly CloudinaryOptions _cloudinaryOptions;

    public UploadController(
        ICloudinaryService cloudinaryService,
        IWebHostEnvironment environment,
        IOptions<CloudinaryOptions> cloudinaryOptions)
    {
        _cloudinaryService = cloudinaryService;
        _environment = environment;
        _cloudinaryOptions = cloudinaryOptions.Value;
    }

    [HttpPost("image")]
    public async Task<ActionResult<UploadResponse>> UploadImage(IFormFile file, [FromQuery] string? folder = "oisipan")
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Không có tệp nào được tải lên." });
        }

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(fileExtension))
        {
            return BadRequest(new { message = "Định dạng tệp không được hỗ trợ. Vui lòng tải lên ảnh (JPG, PNG, GIF, WebP)." });
        }

        // Validate file size (max 5MB)
        const long maxFileSize = 5 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return BadRequest(new { message = "Kích thước tệp quá lớn. Tối đa 5MB." });
        }

        try
        {
            string? imageUrl = null;
            if (!string.IsNullOrWhiteSpace(_cloudinaryOptions.CloudName) &&
                !string.IsNullOrWhiteSpace(_cloudinaryOptions.ApiKey) &&
                !string.IsNullOrWhiteSpace(_cloudinaryOptions.ApiSecret))
            {
                imageUrl = await _cloudinaryService.UploadImageAsync(file, folder ?? "oisipan");
            }

            if (string.IsNullOrEmpty(imageUrl))
            {
                // Fallback to local storage when Cloudinary is not configured or upload fails.
                var uploadDir = Path.Combine(_environment.WebRootPath ?? string.Empty, "uploads");
                Directory.CreateDirectory(uploadDir);

                var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                var fileExt = Path.GetExtension(file.FileName);
                var safeName = string.Concat(fileName.Where(c => !Path.GetInvalidFileNameChars().Contains(c))).Trim();
                if (string.IsNullOrWhiteSpace(safeName)) safeName = "image";
                var uniqueFileName = $"{safeName}_{Guid.NewGuid():N}{fileExt}";
                var filePath = Path.Combine(uploadDir, uniqueFileName);

                await using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                imageUrl = $"{Request.Scheme}://{Request.Host}/uploads/{uniqueFileName}";
            }

            return Ok(new UploadResponse
            {
                Url = imageUrl,
                Message = "Ảnh đã được tải lên thành công."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi tải ảnh: {ex.Message}" });
        }
    }
}

public class UploadResponse
{
    public string Url { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
