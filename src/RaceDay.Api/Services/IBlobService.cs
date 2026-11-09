namespace RaceDay.Api.Services
{
    public interface IBlobService
    {
        Task<string> UploadFileAsync(IFormFile file, string folderPrefix);
        Task<bool> DeleteFileAsync(string fileUrl);
    }
}