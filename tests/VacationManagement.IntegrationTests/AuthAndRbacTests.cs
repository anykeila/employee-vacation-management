using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using VacationManagement.Application.Authentication;
using Xunit;

namespace VacationManagement.IntegrationTests;

[Collection("Integration")]
public class AuthAndRbacTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthAndRbacTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAnd200()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(TestApi.Admin, TestApi.Password));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(TestApi.Admin, "wrong-password"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ExceedingRateLimit_Returns429()
    {
        // Fresh host with a tiny limit so the limiter trips deterministically.
        using var factory = _factory.WithWebHostBuilder(b =>
            b.UseSetting("RateLimiting:AuthPermitLimit", "3"));
        var client = factory.CreateClient();
        var bad = new LoginRequest(TestApi.Admin, "wrong-password");

        // Exhaust the window (each returns 401), then the next request is throttled.
        for (var i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync("/api/v1/auth/login", bad);
        }

        var throttled = await client.PostAsJsonAsync("/api/v1/auth/login", bad);

        throttled.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Employees_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/employees");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Employees_AsEmployeeRole_Returns403()
    {
        var client = await _factory.AuthenticatedClientAsync(TestApi.Marta);

        var response = await client.GetAsync("/api/v1/employees");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Employees_AsAdministrator_Returns200()
    {
        var client = await _factory.AuthenticatedClientAsync(TestApi.Admin);

        var response = await client.GetAsync("/api/v1/employees");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
