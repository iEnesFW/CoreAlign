using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    private const int WORK_FACTOR = 12;

    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WORK_FACTOR);
    }

    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
