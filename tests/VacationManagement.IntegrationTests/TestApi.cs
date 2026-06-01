using System.Net.Http.Headers;
using System.Net.Http.Json;
using VacationManagement.Application.Authentication;

namespace VacationManagement.IntegrationTests;

// Seeded credentials (all share the password below); see DbInitializer / README.
internal static class TestApi
{
    public const string Password = "Password123!";
    public const string Admin = "ana.silva@workflow.com";        // id 1, Administrator
    public const string ManagerOfMarta = "joao.pereira@workflow.com"; // id 2, manages id 3
    public const string ManagerOfHenrique = "joana.soares@workflow.com"; // id 5, manages id 4
    public const string Marta = "marta.fernandes@workflow.com";  // id 3, Employee
    public const string Henrique = "henrique.martins@workflow.com"; // id 4, Employee

    public static async Task<HttpClient> AuthenticatedClientAsync(this CustomWebApplicationFactory factory, string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, Password));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return client;
    }
}
