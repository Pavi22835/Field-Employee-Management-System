using FEMS.Application.Common.Interfaces;

namespace FEMS.Infrastructure.Identity;

/// <summary>BCrypt-based password hashing (section 19: never stored in plain text).</summary>
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
