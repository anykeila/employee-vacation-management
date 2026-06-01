using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VacationManagement.Application.Abstractions;
using VacationManagement.Application.Authentication;
using VacationManagement.Application.Employees;
using VacationManagement.Application.VacationRequests;
using VacationManagement.Infrastructure.Authentication;
using VacationManagement.Infrastructure.Employees;
using VacationManagement.Infrastructure.Persistence;
using VacationManagement.Infrastructure.Security;
using VacationManagement.Infrastructure.VacationRequests;

namespace VacationManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IVacationRequestService, VacationRequestService>();

        services.AddHostedService<RefreshTokenCleanupService>();

        return services;
    }
}
