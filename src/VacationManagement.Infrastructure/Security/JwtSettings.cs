namespace VacationManagement.Infrastructure.Security;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;

    // Lifetime of the long-lived opaque refresh token (the JWT above stays short-lived).
    public int RefreshTokenDays { get; set; } = 7;
}
