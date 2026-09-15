namespace Oishipan.Services;

public interface ICloudinaryService
{
    /// <summary>
    /// Upload an image file to Cloudinary
    /// </summary>
    /// <param name="file">The image file to upload</param>
    /// <param name="folder">The folder in Cloudinary to store the image</param>
    /// <returns>The URL of the uploaded image</returns>
    Task<string?> UploadImageAsync(IFormFile file, string folder = "oisipan");

    /// <summary>
    /// Delete an image from Cloudinary
    /// </summary>
    /// <param name="publicId">The public ID of the image to delete</param>
    Task<bool> DeleteImageAsync(string publicId);
}
