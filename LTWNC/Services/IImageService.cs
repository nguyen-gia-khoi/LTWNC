using Microsoft.AspNetCore.Http;

namespace LTWNC.Services
{
    public interface IImageService
    {
        Task<List<string>> UploadImagesAsync(List<IFormFile> files, string folder);
    }
}
