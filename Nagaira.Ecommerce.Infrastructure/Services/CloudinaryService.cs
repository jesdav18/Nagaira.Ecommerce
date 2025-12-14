using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Nagaira.Ecommerce.Application.Interfaces;

namespace Nagaira.Ecommerce.Infrastructure.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly string _uploadPreset;

    public CloudinaryService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"] 
            ?? throw new InvalidOperationException("Cloudinary:CloudName no está configurado");
        var apiKey = configuration["Cloudinary:ApiKey"] 
            ?? throw new InvalidOperationException("Cloudinary:ApiKey no está configurado");
        var apiSecret = configuration["Cloudinary:ApiSecret"] 
            ?? throw new InvalidOperationException("Cloudinary:ApiSecret no está configurado");
        _uploadPreset = configuration["Cloudinary:UploadPreset"];

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string? folder = null)
    {
        if (imageStream == null)
        {
            throw new ArgumentNullException(nameof(imageStream));
        }

        if (!imageStream.CanRead)
        {
            throw new InvalidOperationException("El stream no se puede leer");
        }

        var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(fileName));

        try
        {
            using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            {
                await imageStream.CopyToAsync(fileStream);
            }

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(tempFilePath),
                Folder = folder ?? "products",
                Transformation = new Transformation()
                    .Quality("auto")
                    .FetchFormat("auto")
            };

            if (!string.IsNullOrWhiteSpace(_uploadPreset))
            {
                uploadParams.UploadPreset = _uploadPreset;
            }

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
            {
                if (uploadResult.Error?.Message?.Contains("preset") == true && !string.IsNullOrWhiteSpace(_uploadPreset))
                {
                    uploadParams.UploadPreset = null;
                    uploadResult = await _cloudinary.UploadAsync(uploadParams);
                    
                    if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new Exception($"Error al subir imagen: {uploadResult.Error?.Message}");
                    }
                }
                else
                {
                    throw new Exception($"Error al subir imagen: {uploadResult.Error?.Message}");
                }
            }

            return uploadResult.SecureUrl.ToString();
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deleteParams);
        return result.Result == "ok";
    }
}

