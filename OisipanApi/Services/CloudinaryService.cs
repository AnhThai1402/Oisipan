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
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IOptions<CloudinaryOptions> options)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.CloudName) || 
            string.IsNullOrWhiteSpace(opts.ApiKey) || 
            string.IsNullOrWhiteSpace(opts.ApiSecret))
        {
            throw new InvalidOperationException("Cloudinary configuration is missing or incomplete.");
        }

        var account = new Account(opts.CloudName, opts.ApiKey, opts.ApiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string?> UploadImageAsync(IFormFile file, string folder = "oisipan")
    {
        if (file == null || file.Length == 0)
            return null;

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
            
            if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return uploadResult.SecureUrl.ToString();
            }

            return null;
        }
        catch (Exception ex)
        {
            // Log the error
            Console.WriteLine($"Cloudinary upload error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return false;

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
