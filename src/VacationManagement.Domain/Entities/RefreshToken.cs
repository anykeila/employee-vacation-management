namespace VacationManagement.Domain.Entities;

// The opaque token handed to the client is never stored; only its SHA-256 hash is,
// so a database leak cannot be replayed against /auth/refresh.
public class RefreshToken
{
    public int Id { get; set; }
    public required string TokenHash { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;
}
