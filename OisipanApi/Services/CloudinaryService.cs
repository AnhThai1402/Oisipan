using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace Oishipan.Services;

public class CloudinaryOptions
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
}

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary? _cloudinary;
    private readonly CloudinaryOptions _options;
    private readonly IWebHostEnvironment _environment;

    public CloudinaryService(IOptions<CloudinaryOptions> options, IWebHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;

        if (!string.IsNullOrWhiteSpace(_options.CloudName) && _options.CloudName != "YOUR_CLOUD_NAME" &&
            !string.IsNullOrWhiteSpace(_options.ApiKey) && _options.ApiKey != "YOUR_API_KEY" &&
            !string.IsNullOrWhiteSpace(_options.ApiSecret) && _options.ApiSecret != "YOUR_API_SECRET")
        {
            var account = new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret);
            _cloudinary = new Cloudinary(account);
        }
    }

    public async Task<string?> UploadImageAsync(IFormFile file, string folder = "oisipan")
    {
        if (file == null || file.Length == 0)
            return null;

        if (_cloudinary == null)
        {
            return await SaveImageLocally(file, folder);
        }

        try
        {
            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                UseFilename = true,
                UniqueFilename = false,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            // Log status for diagnostics
            Console.WriteLine($"Cloudinary upload status: {uploadResult.StatusCode}");
            if (uploadResult.Error != null)
            {
                Console.WriteLine($"Cloudinary upload error: {uploadResult.Error.Message}");
            }

            if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return uploadResult.SecureUrl.ToString();
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cloudinary upload error: {ex.Message}");
            return null;
        }
    }

    private async Task<string> SaveImageLocally(IFormFile file, string folder)
    {
        var relativeFolder = string.IsNullOrWhiteSpace(folder) ? "uploads" : folder.Trim('/').Replace('\\', '/');
        var physicalFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), relativeFolder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(physicalFolder);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(physicalFolder, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/{relativeFolder}/{fileName}";
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return false;

        if (_cloudinary == null)
        {
            // Dummy implementation for local fallback: assume success
            // In a complete implementation, this would delete the local file
            return true;
        }

        try
        {
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);
            
            return result.StatusCode == System.Net.HttpStatusCode.OK || 
                   result.Result == "ok" || 
                   result.Error?.Message == null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cloudinary delete error: {ex.Message}");
            return false;
        }
    }
}
