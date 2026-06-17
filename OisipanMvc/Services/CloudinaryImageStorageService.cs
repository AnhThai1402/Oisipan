using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FrontendMvc.Options;
using Microsoft.Extensions.Options;

namespace FrontendMvc.Services;

public class CloudinaryImageStorageService : IImageStorageService
{
    private readonly CloudinarySettings _settings;

    public CloudinaryImageStorageService(IOptions<CloudinarySettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<string> UploadProductImageAsync(IFormFile imageFile, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.CloudName) ||
            string.IsNullOrWhiteSpace(_settings.ApiKey) ||
            string.IsNullOrWhiteSpace(_settings.ApiSecret))
        {
            throw new InvalidOperationException("Cloudinary chua duoc cau hinh day du.");
        }

        var account = new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret);
        var cloudinary = new Cloudinary(account)
        {
            Api = { Secure = true }
        };

        await using var stream = imageFile.OpenReadStream();
        var publicId = $"{Path.GetFileNameWithoutExtension(imageFile.FileName)}-{Guid.NewGuid():N}";

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(imageFile.FileName, stream),
            Folder = _settings.Folder,
            PublicId = publicId,
            Overwrite = false
        };

        var result = await cloudinary.UploadAsync(uploadParams, cancellationToken);
        if (result.Error is not null)
        {
            throw new InvalidOperationException(result.Error.Message);
        }

        return result.SecureUrl?.ToString()
            ?? throw new InvalidOperationException("Cloudinary khong tra ve URL anh.");
    }
}
