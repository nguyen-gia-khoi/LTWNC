using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using LTWNC.Services;

namespace LTWNC.Services
{
    public class ImageService : IImageService
    {
        private readonly Cloudinary _cloudinary;

        public ImageService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public async Task<List<string>> UploadImagesAsync(List<IFormFile> files, string folder)
        {
            var imageUrls = new List<string>();

            foreach (var file in files)
            {
                if (file.Length <= 0) continue;

                await using var stream = file.OpenReadStream();

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder
                };

                var result = await _cloudinary.UploadAsync(uploadParams);

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    imageUrls.Add(result.SecureUrl.AbsoluteUri);
                }
                else
                {
                    throw new Exception($"Failed to upload {file.FileName}: {result.Error?.Message}");
                }
            }

            return imageUrls;
        }
    }
}
