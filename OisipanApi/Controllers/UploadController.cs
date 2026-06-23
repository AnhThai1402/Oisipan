using Microsoft.AspNetCore.Mvc;
using Oishipan.Services;

namespace Oishipan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly ICloudinaryService _cloudinaryService;

    public UploadController(ICloudinaryService cloudinaryService)
    {
        _cloudinaryService = cloudinaryService;
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
            var imageUrl = await _cloudinaryService.UploadImageAsync(file, folder ?? "oisipan");
            
            if (string.IsNullOrEmpty(imageUrl))
            {
                return StatusCode(500, new { message = "Không thể tải ảnh lên. Vui lòng thử lại." });
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
