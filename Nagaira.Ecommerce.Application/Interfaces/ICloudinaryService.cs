namespace Nagaira.Ecommerce.Application.Interfaces;

public interface ICloudinaryService
{
    Task<string> UploadImageAsync(Stream imageStream, string fileName, string? folder = null);
    Task<bool> DeleteImageAsync(string publicId);
}

