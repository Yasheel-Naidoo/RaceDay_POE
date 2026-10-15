using Microsoft.AspNetCore.Identity;

namespace RaceDay.Api.Services
{
    public interface IPasswordService
    {
        string HashPassword(string password);
        bool VerifyPassword(string hashedPassword, string providedPassword);
    }

    public class PasswordService : IPasswordService
    {
        private readonly PasswordHasher<object> _hasher = new();

        public string HashPassword(string password) => _hasher.HashPassword(new object(), password);

        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            var res = _hasher.VerifyHashedPassword(new object(), hashedPassword, providedPassword);
            return res == PasswordVerificationResult.Success || res == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}