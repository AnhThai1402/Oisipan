using Microsoft.AspNetCore.Http;

namespace FrontendMvc.Services;

public interface IImageStorageService
{
    Task<string> UploadProductImageAsync(IFormFile imageFile, CancellationToken cancellationToken = default);
}
