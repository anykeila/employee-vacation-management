using VacationManagement.Application.Abstractions;

namespace VacationManagement.Infrastructure.Security;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hash) =>
        !string.IsNullOrEmpty(hash) && BCrypt.Net.BCrypt.Verify(password, hash);
}
