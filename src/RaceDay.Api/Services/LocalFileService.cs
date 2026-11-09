using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace RaceDay.Api.Services
{
    public interface ILocalFileService
    {
        Task<string?> SaveFileAsync(IFormFile? file, string folderName);
    }

    public class LocalFileService : ILocalFileService
    {
        private readonly IWebHostEnvironment _env;

        public LocalFileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string?> SaveFileAsync(IFormFile? file, string folderName)
        {
            if (file == null || file.Length == 0) return null;

            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsRoot = Path.Combine(webRootPath, "uploads", folderName);

            if (!Directory.Exists(uploadsRoot))
            {
                Directory.CreateDirectory(uploadsRoot);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsRoot, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{folderName}/{uniqueFileName}";
        }
    }
}