using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FrontendMvc.Options;
using Microsoft.Extensions.Options;

namespace FrontendMvc.Services;

public class CloudinaryImageStorageService : IImageStorageService
{
    private readonly CloudinarySettings _settings;
    private readonly IWebHostEnvironment _environment;

    public CloudinaryImageStorageService(
        IOptions<CloudinarySettings> settings,
        IWebHostEnvironment environment)
    {
        _settings = settings.Value;
        _environment = environment;
    }

    public async Task<string> UploadProductImageAsync(IFormFile imageFile, CancellationToken cancellationToken = default)
    {
        var folder = string.IsNullOrWhiteSpace(_settings.Folder)
            ? "oisipan/products"
            : $"{_settings.Folder.TrimEnd('/')}/products";
        return await UploadImageAsync(imageFile, folder, cancellationToken);
    }

    public async Task<string> UploadAvatarImageAsync(IFormFile imageFile, CancellationToken cancellationToken = default)
    {
        var folder = string.IsNullOrWhiteSpace(_settings.Folder)
            ? "oisipan/avatars"
            : $"{_settings.Folder.TrimEnd('/')}/avatars";
        return await UploadImageAsync(imageFile, folder, cancellationToken);
    }

    public async Task<string> UploadNewsImageAsync(IFormFile imageFile, CancellationToken cancellationToken = default)
    {
        var folder = string.IsNullOrWhiteSpace(_settings.Folder)
            ? "oisipan/news"
            : $"{_settings.Folder.TrimEnd('/')}/news";
        return await UploadImageAsync(imageFile, folder, cancellationToken);
    }

    public async Task<string> UploadCategoryImageAsync(IFormFile imageFile, CancellationToken cancellationToken = default)
    {
        var folder = string.IsNullOrWhiteSpace(_settings.Folder)
            ? "oisipan/categories"
            : $"{_settings.Folder.TrimEnd('/')}/categories";
        return await UploadImageAsync(imageFile, folder, cancellationToken);
    }

    private async Task<string> UploadImageAsync(
        IFormFile imageFile,
        string folder,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.CloudName) || _settings.CloudName == "YOUR_CLOUD_NAME" ||
            string.IsNullOrWhiteSpace(_settings.ApiKey) || _settings.ApiKey == "YOUR_API_KEY" ||
            string.IsNullOrWhiteSpace(_settings.ApiSecret) || _settings.ApiSecret == "YOUR_API_SECRET")
        {
            return await SaveImageLocally(imageFile, folder, cancellationToken);
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

    private async Task<string> SaveImageLocally(
        IFormFile imageFile,
        string folder,
        CancellationToken cancellationToken)
    {
        var relativeFolder = string.IsNullOrWhiteSpace(folder)
            ? "uploads"
            : folder.Trim('/').Replace('\\', '/');
        var physicalFolder = Path.Combine(
            _environment.WebRootPath,
            relativeFolder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(physicalFolder);

        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(physicalFolder, fileName);

        await using var output = new FileStream(filePath, FileMode.CreateNew);
        await imageFile.CopyToAsync(output, cancellationToken);

        return $"/{relativeFolder}/{fileName}";
    }
}
