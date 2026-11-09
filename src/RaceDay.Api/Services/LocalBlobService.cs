using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace RaceDay.Api.Services
{
    /// <summary>
    /// Local-disk stand-in for blob storage: files are written under wwwroot/uploads
    /// by LocalFileService and removed from there on delete.
    /// </summary>
    public class LocalBlobService : IBlobService
    {
        private readonly ILocalFileService _files;
        private readonly IWebHostEnvironment _env;

        public LocalBlobService(ILocalFileService files, IWebHostEnvironment env)
        {
            _files = files;
            _env = env;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folderPrefix)
        {
            return await _files.SaveFileAsync(file, folderPrefix) ?? string.Empty;
        }

        public Task<bool> DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return Task.FromResult(false);

            var root = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var relative = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var path = Path.Combine(root, relative);

            if (!File.Exists(path)) return Task.FromResult(false);

            File.Delete(path);
            return Task.FromResult(true);
        }
    }
}
