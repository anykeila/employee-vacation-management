using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using VacationManagement.Application.Abstractions;
using VacationManagement.Domain.Enums;

namespace VacationManagement.Api.Infrastructure;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public int? Id =>
        int.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;

    public string? Email => Principal?.FindFirstValue(JwtRegisteredClaimNames.Email);

    public Role? Role =>
        Enum.TryParse<Role>(Principal?.FindFirstValue("role"), out var role) ? role : null;
}
