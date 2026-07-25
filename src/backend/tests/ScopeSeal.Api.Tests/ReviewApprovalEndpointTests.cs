using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ScopeSeal.AgreementSnapshots.Services;
using ScopeSeal.Workspaces.Domain;

namespace ScopeSeal.Api.Tests;

[Collection("PostgresIntegration")]
public sealed class ReviewApprovalEndpointTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    private readonly PostgresWebApplicationFactory _factory = factory;

    [Fact]
    public async Task ShareInviteReviewAndApproveFlow_Succeeds()
    {
        var ownerClient = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(ownerClient, $"review-{suffix}@example.com", "Review Tenant");
        var workspacePublicId = await CreateWorkspaceAsync(ownerClient, tenantPublicId, "Review workspace");

        var createResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots",
            new { title = "Approval flow snapshot" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var snapshotPublicId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        var shareResponse = await ownerClient.PostAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}/share",
            null);
        shareResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await shareResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("status").GetString().Should().Be("Shared");

        var inviteResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}/invitations",
            new { reviewerEmail = $"reviewer-{suffix}@example.com", reviewerName = "External Reviewer" });
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var invitation = await inviteResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = invitation.GetProperty("token").GetGuid();

        var externalClient = _factory.CreateClient();
        var reviewResponse = await externalClient.GetAsync($"/api/v1/external/review/{token}");
        reviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var review = await reviewResponse.Content.ReadFromJsonAsync<JsonElement>();
        review.GetProperty("snapshot").GetProperty("status").GetString().Should().Be("Shared");

        var commentResponse = await externalClient.PostAsJsonAsync(
            $"/api/v1/external/review/{token}/comments",
            new { authorName = "External Reviewer", content = "Please clarify payment terms." });
        commentResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var readyResponse = await ownerClient.PostAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}/ready-for-approval",
            null);
        readyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await readyResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("status").GetString().Should().Be("ReadyForApproval");

        var approveResponse = await externalClient.PostAsJsonAsync(
            $"/api/v1/external/review/{token}/approve",
            new
            {
                approverName = "External Reviewer",
                approverEmail = $"reviewer-{suffix}@example.com",
                confirmationStatement = "I approve this agreement snapshot as presented."
            });
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var approval = await approveResponse.Content.ReadFromJsonAsync<JsonElement>();
        approval.GetProperty("canonicalHashSha256").GetString().Should().NotBeNullOrWhiteSpace();
        approval.GetProperty("snapshotVersionNumber").GetInt32().Should().Be(1);

        var getSnapshotResponse = await ownerClient.GetAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}");
        getSnapshotResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await getSnapshotResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("status").GetString().Should().Be("Approved");

        var approvalRecordResponse = await ownerClient.GetAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}/approval");
        approvalRecordResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApprovedSnapshotCannotBeEdited()
    {
        var ownerClient = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(ownerClient, $"immutable-{suffix}@example.com", "Immutable Tenant");
        var workspacePublicId = await CreateWorkspaceAsync(ownerClient, tenantPublicId, "Immutable workspace");

        var createResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots",
            new { title = "Immutable snapshot" });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var snapshotPublicId = created.GetProperty("publicId").GetGuid();
        var updatedAtUtc = created.GetProperty("updatedAtUtc").GetDateTime();

        await ownerClient.PostAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}/share",
            null);
        var inviteResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}/invitations",
            new { reviewerEmail = $"immutable-reviewer-{suffix}@example.com" });
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
                approverEmail = $"immutable-reviewer-{suffix}@example.com",
                confirmationStatement = "I approve this agreement snapshot as presented."
            });

        var updateResponse = await ownerClient.PutAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}",
            new UpdateAgreementSnapshotRequest(
                "Changed title",
                null,
                updatedAtUtc,
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                []));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RevokedInvitationReturnsNotFound()
    {
        var ownerClient = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(ownerClient, $"revoke-{suffix}@example.com", "Revoke Tenant");
        var workspacePublicId = await CreateWorkspaceAsync(ownerClient, tenantPublicId, "Revoke workspace");

        var createResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots",
            new { title = "Revoke test" });
        var snapshotPublicId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        await ownerClient.PostAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}/share",
            null);

        var inviteResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}/invitations",
            new { reviewerEmail = $"revoke-reviewer-{suffix}@example.com" });
        var invitation = await inviteResponse.Content.ReadFromJsonAsync<JsonElement>();
        var invitationPublicId = invitation.GetProperty("publicId").GetGuid();
        var token = invitation.GetProperty("token").GetGuid();

        var revokeResponse = await ownerClient.PostAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}/invitations/{invitationPublicId}/revoke",
            null);
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var externalClient = _factory.CreateClient();
        var reviewResponse = await externalClient.GetAsync($"/api/v1/external/review/{token}");
        reviewResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RequestChangesTransitionsSnapshotStatus()
    {
        var ownerClient = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(ownerClient, $"changes-{suffix}@example.com", "Changes Tenant");
        var workspacePublicId = await CreateWorkspaceAsync(ownerClient, tenantPublicId, "Changes workspace");

        var createResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots",
            new { title = "Changes requested test" });
        var snapshotPublicId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        await ownerClient.PostAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}/share",
            null);

        var inviteResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}/invitations",
            new { reviewerEmail = $"changes-reviewer-{suffix}@example.com" });
        var token = (await inviteResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetGuid();

        var externalClient = _factory.CreateClient();
        var requestChangesResponse = await externalClient.PostAsync(
            $"/api/v1/external/review/{token}/request-changes",
            null);
        requestChangesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await requestChangesResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("status").GetString().Should().Be("ChangesRequested");
    }

    [Fact]
    public async Task UserCannotAccessAnotherTenantsReviewInvitations()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var userAClient = CreateAuthenticatedClient();
        var userBClient = CreateAuthenticatedClient();

        var tenantA = await RegisterAndLoginAsync(userAClient, $"review-a-{suffix}@example.com", "Tenant A");
        var tenantB = await RegisterAndLoginAsync(userBClient, $"review-b-{suffix}@example.com", "Tenant B");

        var workspaceB = await CreateWorkspaceAsync(userBClient, tenantB, "Private workspace");
        var createResponse = await userBClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantB}/workspaces/{workspaceB}/snapshots",
            new { title = "Private snapshot" });
        var snapshotPublicId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        await userBClient.PostAsync(
            $"/api/v1/tenants/{tenantB}/workspaces/{workspaceB}/snapshots/{snapshotPublicId}/share",
            null);

        var crossTenantList = await userAClient.GetAsync(
            $"/api/v1/tenants/{tenantB}/workspaces/{workspaceB}/snapshots/{snapshotPublicId}/invitations");
        crossTenantList.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FreePlanBlocksSecondExternalInvitation()
    {
        var ownerClient = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(ownerClient, $"invite-limit-{suffix}@example.com", "Invite Limit Tenant");
        var workspacePublicId = await CreateWorkspaceAsync(ownerClient, tenantPublicId, "Invite workspace");

        var createResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots",
            new { title = "Invitation limit test" });
        var snapshotPublicId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        await ownerClient.PostAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}/share",
            null);

        var firstInvite = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}/invitations",
            new { reviewerEmail = $"first-{suffix}@example.com" });
        firstInvite.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondInvite = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}/invitations",
            new { reviewerEmail = $"second-{suffix}@example.com" });
        secondInvite.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
            displayName = "Review Test User",
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
