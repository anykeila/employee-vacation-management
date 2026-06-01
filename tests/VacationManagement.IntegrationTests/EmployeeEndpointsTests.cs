using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using VacationManagement.Application.Employees;
using VacationManagement.Domain.Enums;
using Xunit;

namespace VacationManagement.IntegrationTests;

[Collection("Integration")]
public class EmployeeEndpointsTests
{
    private readonly CustomWebApplicationFactory _factory;

    public EmployeeEndpointsTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Administrator_CanCreateEmployee_AndThenReadItBack()
    {
        var client = await _factory.AuthenticatedClientAsync(TestApi.Admin);

        var create = await client.PostAsJsonAsync("/api/v1/employees", new CreateEmployeeRequest(
            "Carlos Tester", "carlos.tester@workflow.com", TestApi.Password, Role.Employee, 2));

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<EmployeeResponse>();
        created!.Id.Should().BeGreaterThan(0);
        created.ManagerName.Should().Be("João Pereira");

        var read = await client.GetAsync($"/api/v1/employees/{created.Id}");
        read.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await read.Content.ReadFromJsonAsync<EmployeeResponse>();
        fetched!.Email.Should().Be("carlos.tester@workflow.com");
    }

    [Fact]
    public async Task Manager_CannotCreateEmployee_Returns403()
    {
        var client = await _factory.AuthenticatedClientAsync(TestApi.ManagerOfMarta);

        var response = await client.PostAsJsonAsync("/api/v1/employees", new CreateEmployeeRequest(
            "Nope", "nope@workflow.com", TestApi.Password, Role.Employee, null));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
