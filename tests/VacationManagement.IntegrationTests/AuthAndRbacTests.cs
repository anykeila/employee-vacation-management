using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
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
