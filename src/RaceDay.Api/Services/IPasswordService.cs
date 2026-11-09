namespace RaceDay.Api.Services
{
    public interface IPasswordService
    {
        string HashPassword(string password);
        bool VerifyPassword(string providedPassword, string hashedPassword);
    }
}