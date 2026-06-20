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
        return await UploadImageAsync(imageFile, _settings.Folder, cancellationToken);
    }

    public async Task<string> UploadNewsImageAsync(IFormFile imageFile, CancellationToken cancellationToken = default)
    {
        var folder = string.IsNullOrWhiteSpace(_settings.Folder)
            ? "oisipan/news"
            : $"{_settings.Folder.TrimEnd('/')}/news";
        return await UploadImageAsync(imageFile, folder, cancellationToken);
    }

    private async Task<string> UploadImageAsync(
        IFormFile imageFile,
        string folder,
        CancellationToken cancellationToken)
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
            Folder = folder,
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
