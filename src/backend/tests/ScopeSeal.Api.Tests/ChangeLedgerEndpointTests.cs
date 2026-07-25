using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ScopeSeal.AgreementSnapshots.Services;
using ScopeSeal.ChangeLedger.Domain;
using ScopeSeal.ChangeLedger.Services;
using ScopeSeal.Workspaces.Domain;

namespace ScopeSeal.Api.Tests;

[Collection("PostgresIntegration")]
public sealed class ChangeLedgerEndpointTests(ChangeLedgerPostgresWebApplicationFactory factory)
    : IClassFixture<ChangeLedgerPostgresWebApplicationFactory>
{
    private readonly ChangeLedgerPostgresWebApplicationFactory _factory = factory;

    [Fact]
    public async Task ChangeRequestAcceptAndReapproveFlow_Succeeds()
    {
        var ownerClient = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(ownerClient, $"change-{suffix}@example.com", "Change Tenant");
        var workspacePublicId = await CreateWorkspaceAsync(ownerClient, tenantPublicId, "Change workspace");

        var approvedSnapshotPublicId = await CreateApprovedSnapshotAsync(
            ownerClient, tenantPublicId, workspacePublicId, suffix, "Original scope");

        var createChangeResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/change-requests",
            new
            {
                sourceSnapshotPublicId = approvedSnapshotPublicId,
                title = "Add extra room",
                reason = "Client requested additional bedroom design.",
                impacts = new object[]
                {
                    new
                    {
                        impactType = ChangeImpactType.Scope.ToString(),
                        description = "One additional bedroom in scope"
                    },
                    new
                    {
                        impactType = ChangeImpactType.Financial.ToString(),
                        description = "Additional design fee",
                        amountMinorUnits = 2500000L,
                        currencyCode = "INR"
                    }
                }
            });
        createChangeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var changeRequest = await createChangeResponse.Content.ReadFromJsonAsync<JsonElement>();
        var changeRequestPublicId = changeRequest.GetProperty("publicId").GetGuid();
        changeRequest.GetProperty("status").GetString().Should().Be("Proposed");

        var discussResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/change-requests/{changeRequestPublicId}/transition",
            new { newStatus = ChangeRequestStatus.UnderDiscussion.ToString() });
        discussResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var acceptResponse = await ownerClient.PostAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/change-requests/{changeRequestPublicId}/accept",
            null);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var acceptResult = await acceptResponse.Content.ReadFromJsonAsync<JsonElement>();
        var draftSnapshotPublicId = acceptResult
            .GetProperty("draftSnapshot")
            .GetProperty("publicId")
            .GetGuid();
        acceptResult.GetProperty("changeRequest").GetProperty("status").GetString().Should().Be("Accepted");
        acceptResult.GetProperty("draftSnapshot").GetProperty("versionNumber").GetInt32().Should().Be(2);
        acceptResult.GetProperty("draftSnapshot").GetProperty("status").GetString().Should().Be("Draft");

        var originalSnapshotResponse = await ownerClient.GetAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{approvedSnapshotPublicId}");
        originalSnapshotResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await originalSnapshotResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("status").GetString().Should().Be("Approved");

        var draftSnapshot = await ownerClient.GetFromJsonAsync<JsonElement>(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{draftSnapshotPublicId}");
        var updatedAtUtc = draftSnapshot.GetProperty("updatedAtUtc").GetDateTime();

        await ownerClient.PutAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{draftSnapshotPublicId}",
            new UpdateAgreementSnapshotRequest(
                "Updated scope with extra room",
                "Includes additional bedroom design.",
                updatedAtUtc,
                [new SectionItemInput(null, 0, "Extra bedroom design", "Full design package")],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                []));

        await ownerClient.PostAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{draftSnapshotPublicId}/share",
            null);
        var inviteResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{draftSnapshotPublicId}/invitations",
            new { reviewerEmail = $"change-reviewer-{suffix}@example.com" });
        var token = (await inviteResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetGuid();

        await ownerClient.PostAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{draftSnapshotPublicId}/ready-for-approval",
            null);

        var externalClient = _factory.CreateClient();
        await externalClient.PostAsJsonAsync(
            $"/api/v1/external/review/{token}/approve",
            new
            {
                approverName = "Reviewer",
                approverEmail = $"change-reviewer-{suffix}@example.com",
                confirmationStatement = "I approve the updated agreement snapshot."
            });

        (await ownerClient.GetFromJsonAsync<JsonElement>(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{approvedSnapshotPublicId}"))
            .GetProperty("status").GetString().Should().Be("Superseded");

        (await ownerClient.GetFromJsonAsync<JsonElement>(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{draftSnapshotPublicId}"))
            .GetProperty("status").GetString().Should().Be("Approved");

        var implementedChangeRequest = await ownerClient.GetFromJsonAsync<JsonElement>(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/change-requests/{changeRequestPublicId}");
        implementedChangeRequest.GetProperty("status").GetString().Should().Be("Implemented");
        implementedChangeRequest.GetProperty("resultSnapshotPublicId").GetGuid().Should().Be(draftSnapshotPublicId);
    }

    [Fact]
    public async Task SnapshotDiffReturnsChanges()
    {
        var ownerClient = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(ownerClient, $"diff-{suffix}@example.com", "Diff Tenant");
        var workspacePublicId = await CreateWorkspaceAsync(ownerClient, tenantPublicId, "Diff workspace");

        var approvedSnapshotPublicId = await CreateApprovedSnapshotAsync(
            ownerClient, tenantPublicId, workspacePublicId, suffix, "Diff baseline");

        var createChangeResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/change-requests",
            new
            {
                sourceSnapshotPublicId = approvedSnapshotPublicId,
                title = "Scope tweak",
                reason = "Minor adjustment."
            });
        var changeRequestPublicId = (await createChangeResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/change-requests/{changeRequestPublicId}/transition",
            new { newStatus = ChangeRequestStatus.UnderDiscussion.ToString() });

        var acceptResponse = await ownerClient.PostAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/change-requests/{changeRequestPublicId}/accept",
            null);
        var draftSnapshotPublicId = (await acceptResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("draftSnapshot").GetProperty("publicId").GetGuid();

        var draftSnapshot = await ownerClient.GetFromJsonAsync<JsonElement>(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{draftSnapshotPublicId}");
        var updatedAtUtc = draftSnapshot.GetProperty("updatedAtUtc").GetDateTime();

        await ownerClient.PutAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{draftSnapshotPublicId}",
            new UpdateAgreementSnapshotRequest(
                "Diff baseline revised",
                null,
                updatedAtUtc,
                [new SectionItemInput(null, 0, "New scope item", null)],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                []));

        var diffResponse = await ownerClient.GetAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{approvedSnapshotPublicId}/diff/{draftSnapshotPublicId}");
        diffResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var diff = await diffResponse.Content.ReadFromJsonAsync<JsonElement>();
        diff.GetProperty("fromVersionNumber").GetInt32().Should().Be(1);
        diff.GetProperty("toVersionNumber").GetInt32().Should().Be(2);
        diff.GetProperty("changes").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ApprovedSnapshotCannotBeMutatedByChangeRequest()
    {
        var ownerClient = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(ownerClient, $"immutable2-{suffix}@example.com", "Immutable2 Tenant");
        var workspacePublicId = await CreateWorkspaceAsync(ownerClient, tenantPublicId, "Immutable2 workspace");

        var approvedSnapshotPublicId = await CreateApprovedSnapshotAsync(
            ownerClient, tenantPublicId, workspacePublicId, suffix, "Immutable approved");

        var before = await ownerClient.GetFromJsonAsync<JsonElement>(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{approvedSnapshotPublicId}");
        var hashBefore = before.GetProperty("updatedAtUtc").GetDateTime();

        var createChangeResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/change-requests",
            new
            {
                sourceSnapshotPublicId = approvedSnapshotPublicId,
                title = "Attempt mutation",
                reason = "Should clone, not mutate."
            });
        var changeRequestPublicId = (await createChangeResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/change-requests/{changeRequestPublicId}/transition",
            new { newStatus = ChangeRequestStatus.UnderDiscussion.ToString() });

        await ownerClient.PostAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/change-requests/{changeRequestPublicId}/accept",
            null);

        var after = await ownerClient.GetFromJsonAsync<JsonElement>(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{approvedSnapshotPublicId}");
        after.GetProperty("status").GetString().Should().Be("Approved");
        after.GetProperty("updatedAtUtc").GetDateTime().Should().Be(hashBefore);
    }

    [Fact]
    public async Task UserCannotAccessAnotherTenantsChangeRequests()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var userAClient = CreateAuthenticatedClient();
        var userBClient = CreateAuthenticatedClient();

        var tenantA = await RegisterAndLoginAsync(userAClient, $"change-a-{suffix}@example.com", "Tenant A");
        var tenantB = await RegisterAndLoginAsync(userBClient, $"change-b-{suffix}@example.com", "Tenant B");

        var workspaceA = await CreateWorkspaceAsync(userAClient, tenantA, "Workspace A");
        var workspaceB = await CreateWorkspaceAsync(userBClient, tenantB, "Workspace B");

        var snapshotA = await CreateApprovedSnapshotAsync(userAClient, tenantA, workspaceA, suffix + "a", "Tenant A snapshot");

        var createResponse = await userAClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantA}/workspaces/{workspaceA}/change-requests",
            new
            {
                sourceSnapshotPublicId = snapshotA,
                title = "Tenant A change",
                reason = "Private change."
            });
        var changeRequestPublicId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        var crossTenantResponse = await userBClient.GetAsync(
            $"/api/v1/tenants/{tenantB}/workspaces/{workspaceB}/change-requests/{changeRequestPublicId}");
        crossTenantResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InvalidStateTransitionReturnsConflict()
    {
        var ownerClient = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(ownerClient, $"state-{suffix}@example.com", "State Tenant");
        var workspacePublicId = await CreateWorkspaceAsync(ownerClient, tenantPublicId, "State workspace");

        var approvedSnapshotPublicId = await CreateApprovedSnapshotAsync(
            ownerClient, tenantPublicId, workspacePublicId, suffix, "State test");

        var createChangeResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/change-requests",
            new
            {
                sourceSnapshotPublicId = approvedSnapshotPublicId,
                title = "State test",
                reason = "Testing transitions."
            });
        var changeRequestPublicId = (await createChangeResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        var rejectFromProposed = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/change-requests/{changeRequestPublicId}/transition",
            new { newStatus = ChangeRequestStatus.Rejected.ToString() });
        rejectFromProposed.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task<Guid> CreateApprovedSnapshotAsync(
        HttpClient ownerClient,
        Guid tenantPublicId,
        Guid workspacePublicId,
        string suffix,
        string title)
    {
        var createResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots",
            new { title });
        var snapshotPublicId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        await ownerClient.PostAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}/share",
            null);

        var inviteResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}/invitations",
            new { reviewerEmail = $"reviewer-{suffix}@example.com" });
        var token = (await inviteResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetGuid();

        await ownerClient.PostAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}/ready-for-approval",
            null);

        var externalClient = _factory.CreateClient();
        await externalClient.PostAsJsonAsync(
            $"/api/v1/external/review/{token}/approve",
            new
            {
                approverName = "Reviewer",
                approverEmail = $"reviewer-{suffix}@example.com",
                confirmationStatement = "I approve this agreement snapshot as presented."
            });

        return snapshotPublicId;
    }

    private HttpClient CreateAuthenticatedClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

    private static async Task<Guid> RegisterAndLoginAsync(HttpClient client, string email, string tenantName)
    {
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "SecurePass1!",
            displayName = "Change Ledger Test User",
            tenantName,
            confirmedAge18OrAbove = true
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = "SecurePass1!"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var me = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        return me.GetProperty("tenant").GetProperty("publicId").GetGuid();
    }

    private static async Task<Guid> CreateWorkspaceAsync(HttpClient client, Guid tenantPublicId, string name)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces",
            new { name, type = WorkspaceType.General.ToString() });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var workspace = await response.Content.ReadFromJsonAsync<JsonElement>();
        return workspace.GetProperty("publicId").GetGuid();
    }
}
