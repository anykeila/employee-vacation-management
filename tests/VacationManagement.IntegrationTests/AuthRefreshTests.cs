using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using VacationManagement.Application.Authentication;
using Xunit;

namespace VacationManagement.IntegrationTests;

[Collection("Integration")]
public class AuthRefreshTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthRefreshTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_IssuesAccessAndRefreshTokens()
    {
        var login = await LoginAsync();

        login.AccessToken.Should().NotBeNullOrWhiteSpace();
        login.RefreshToken.Should().NotBeNullOrWhiteSpace();
        login.RefreshTokenExpiresAtUtc.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Refresh_WithValidToken_RotatesToNewTokenPair()
    {
        var login = await LoginAsync();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshRequest(login.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await response.Content.ReadFromJsonAsync<LoginResponse>();
        refreshed!.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshed.RefreshToken.Should().NotBe(login.RefreshToken); // rotated
    }

    [Fact]
    public async Task Refresh_ReusingAlreadyRotatedToken_Returns401()
    {
        var login = await LoginAsync();
        var client = _factory.CreateClient();

        // First use rotates (and revokes) the original token.
        (await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(login.RefreshToken)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // Presenting the already-rotated token again must be rejected.
        var reuse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(login.RefreshToken));

        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshRequest("not-a-real-token"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<LoginResponse> LoginAsync()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(TestApi.Admin, TestApi.Password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }
}
