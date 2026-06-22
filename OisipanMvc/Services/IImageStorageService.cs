using Microsoft.AspNetCore.Http;

namespace FrontendMvc.Services;

public interface IImageStorageService
{
    Task<string> UploadProductImageAsync(IFormFile imageFile, CancellationToken cancellationToken = default);
    Task<string> UploadNewsImageAsync(IFormFile imageFile, CancellationToken cancellationToken = default);
    Task<string> UploadCategoryImageAsync(IFormFile imageFile, CancellationToken cancellationToken = default);
}
