using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using VacationManagement.Application.VacationRequests;
using Xunit;

namespace VacationManagement.IntegrationTests;

[Collection("Integration")]
public class VacationWorkflowTests
{
    private readonly CustomWebApplicationFactory _factory;

    // Real wall-clock applies here (TimeProvider.System), so every range is well in
    // the future. Each test owns a disjoint window because the no-overlap rule is
    // company-wide and the shared database persists approvals across tests.
    private static readonly DateOnly Base = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));

    public VacationWorkflowTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Employee_CreatesRequest_DirectManager_Approves()
    {
        var marta = await _factory.AuthenticatedClientAsync(TestApi.Marta);
        var created = await CreateRequestAsync(marta, Base.AddDays(10), Base.AddDays(14));
        created.Status.Should().Be("Pending");

        var joao = await _factory.AuthenticatedClientAsync(TestApi.ManagerOfMarta);
        var approve = await joao.PostAsJsonAsync($"/api/v1/vacation-requests/{created.Id}/approve",
            new DecisionRequest(null));

        approve.StatusCode.Should().Be(HttpStatusCode.OK);
        var decided = await approve.Content.ReadFromJsonAsync<VacationRequestResponse>();
        decided!.Status.Should().Be("Approved");
    }

    [Fact]
    public async Task Approving_OverlapWithAnotherEmployeesApprovedVacation_Returns409()
    {
        // Marta's request gets approved for a window owned by this test.
        var marta = await _factory.AuthenticatedClientAsync(TestApi.Marta);
        var martaReq = await CreateRequestAsync(marta, Base.AddDays(40), Base.AddDays(44));
        var joao = await _factory.AuthenticatedClientAsync(TestApi.ManagerOfMarta);
        (await joao.PostAsJsonAsync($"/api/v1/vacation-requests/{martaReq.Id}/approve",
            new DecisionRequest(null))).StatusCode.Should().Be(HttpStatusCode.OK);

        // Henrique (different employee, different manager) requests an overlapping window.
        var henrique = await _factory.AuthenticatedClientAsync(TestApi.Henrique);
        var henriqueReq = await CreateRequestAsync(henrique, Base.AddDays(42), Base.AddDays(46));
        var joana = await _factory.AuthenticatedClientAsync(TestApi.ManagerOfHenrique);

        var approve = await joana.PostAsJsonAsync($"/api/v1/vacation-requests/{henriqueReq.Id}/approve",
            new DecisionRequest(null));

        // The PostgreSQL GiST exclusion constraint is the authoritative backstop here.
        approve.StatusCode.Should().Be(HttpStatusCode.Conflict);
        approve.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Approving_AdjacentVacation_IsAllowed()
    {
        var marta = await _factory.AuthenticatedClientAsync(TestApi.Marta);
        var martaReq = await CreateRequestAsync(marta, Base.AddDays(70), Base.AddDays(74));
        var joao = await _factory.AuthenticatedClientAsync(TestApi.ManagerOfMarta);
        (await joao.PostAsJsonAsync($"/api/v1/vacation-requests/{martaReq.Id}/approve",
            new DecisionRequest(null))).StatusCode.Should().Be(HttpStatusCode.OK);

        // Henrique starts the day after Marta ends — no shared day, must be allowed.
        var henrique = await _factory.AuthenticatedClientAsync(TestApi.Henrique);
        var henriqueReq = await CreateRequestAsync(henrique, Base.AddDays(75), Base.AddDays(79));
        var joana = await _factory.AuthenticatedClientAsync(TestApi.ManagerOfHenrique);

        var approve = await joana.PostAsJsonAsync($"/api/v1/vacation-requests/{henriqueReq.Id}/approve",
            new DecisionRequest(null));

        approve.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Manager_CannotApproveRequestOutsideTheirTeam_Returns403()
    {
        var marta = await _factory.AuthenticatedClientAsync(TestApi.Marta);
        var martaReq = await CreateRequestAsync(marta, Base.AddDays(100), Base.AddDays(104));

        // Joana manages Henrique, not Marta.
        var joana = await _factory.AuthenticatedClientAsync(TestApi.ManagerOfHenrique);
        var approve = await joana.PostAsJsonAsync($"/api/v1/vacation-requests/{martaReq.Id}/approve",
            new DecisionRequest(null));

        approve.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Employee_CancelsOwnPendingRequest_TransitionsToCancelled()
    {
        var marta = await _factory.AuthenticatedClientAsync(TestApi.Marta);
        var created = await CreateRequestAsync(marta, Base.AddDays(130), Base.AddDays(134));

        var cancel = await marta.PostAsync($"/api/v1/vacation-requests/{created.Id}/cancel", null);

        cancel.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelled = await cancel.Content.ReadFromJsonAsync<VacationRequestResponse>();
        cancelled!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task Employee_CannotCancelAnotherEmployeesRequest_Returns403()
    {
        var marta = await _factory.AuthenticatedClientAsync(TestApi.Marta);
        var martaReq = await CreateRequestAsync(marta, Base.AddDays(160), Base.AddDays(164));

        var henrique = await _factory.AuthenticatedClientAsync(TestApi.Henrique);
        var cancel = await henrique.PostAsync($"/api/v1/vacation-requests/{martaReq.Id}/cancel", null);

        cancel.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<VacationRequestResponse> CreateRequestAsync(
        HttpClient client, DateOnly start, DateOnly end)
    {
        var response = await client.PostAsJsonAsync("/api/v1/vacation-requests",
            new CreateVacationRequestRequest(start, end, null));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<VacationRequestResponse>())!;
    }
}
