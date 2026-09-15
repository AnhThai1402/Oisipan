using Microsoft.AspNetCore.Http;

namespace FrontendMvc.Services;

public interface IImageStorageService
{
    Task<string> UploadProductImageAsync(IFormFile imageFile, CancellationToken cancellationToken = default);
    Task<string> UploadBannerImageAsync(IFormFile imageFile, CancellationToken cancellationToken = default);
    Task<string> UploadCategoryImageAsync(IFormFile imageFile, CancellationToken cancellationToken = default);
    Task<string> UploadAvatarImageAsync(IFormFile imageFile, CancellationToken cancellationToken = default);
}
