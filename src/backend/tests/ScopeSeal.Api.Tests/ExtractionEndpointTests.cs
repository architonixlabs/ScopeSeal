using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ScopeSeal.Entitlements.Domain;
using ScopeSeal.Entitlements.Services;
using ScopeSeal.Extraction.Domain;
using ScopeSeal.Extraction.Services;
using ScopeSeal.Identity.Domain;
using ScopeSeal.Tenancy.Services;
using ScopeSeal.Workspaces.Domain;

namespace ScopeSeal.Api.Tests;

[Collection("PostgresIntegration")]
public sealed class ExtractionEndpointTests(ExtractionPostgresWebApplicationFactory factory)
    : IClassFixture<ExtractionPostgresWebApplicationFactory>
{
    private static readonly byte[] ExtractionFixturePdf =
        "%PDF-1.4\nSCOPeseal-fixture\n1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\ntrailer\n<< /Size 4 /Root 1 0 R >>\nstartxref\n149\n%%EOF"u8.ToArray();

    private readonly ExtractionPostgresWebApplicationFactory _factory = factory;

    [Fact]
    public async Task ManualOnlyModeBlocksExtractionTrigger()
    {
        await using var manualFactory = new ManualOnlyWebApplicationFactory();
        await manualFactory.InitializeAsync();
        var client = manualFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"manual-mode-{suffix}@example.com";
        var tenantPublicId = await RegisterAndLoginAsync(client, email, "Manual Mode Tenant");
        await AssignProPlanAsync(manualFactory, email);
        var workspacePublicId = await CreateWorkspaceAsync(client, tenantPublicId, "Manual mode workspace");
        var documentPublicId = await UploadDocumentAsync(client, tenantPublicId, workspacePublicId);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/documents/{documentPublicId}/extraction-runs",
            new { snapshotPublicId = (Guid?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FreePlanBlocksExtractionTrigger()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"free-extract-{suffix}@example.com", "Free Extraction Tenant");
        var workspacePublicId = await CreateWorkspaceAsync(client, tenantPublicId, "Free workspace");
        var documentPublicId = await UploadDocumentAsync(client, tenantPublicId, workspacePublicId);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/documents/{documentPublicId}/extraction-runs",
            new { snapshotPublicId = (Guid?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LocalProcessingExtractReviewAndApplyFlow_Succeeds()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"extract-{suffix}@example.com";
        var tenantPublicId = await RegisterAndLoginAsync(client, email, "Extraction Tenant");
        await AssignProPlanAsync(_factory, email);
        var workspacePublicId = await CreateWorkspaceAsync(client, tenantPublicId, "Extraction workspace");
        var documentPublicId = await UploadDocumentAsync(client, tenantPublicId, workspacePublicId);

        var snapshotResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots",
            new { title = "Extraction target snapshot" });
        snapshotResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var snapshotPublicId = (await snapshotResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        var triggerResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/documents/{documentPublicId}/extraction-runs",
            new { snapshotPublicId });
        triggerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var runPublicId = (await triggerResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        await ProcessPendingJobsAsync();

        var runResponse = await client.GetAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/extraction-runs/{runPublicId}");
        runResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var run = await runResponse.Content.ReadFromJsonAsync<JsonElement>();
        run.GetProperty("status").GetString().Should().Be("Completed");
        run.GetProperty("facts").GetArrayLength().Should().Be(3);
        run.GetProperty("facts")[0].GetProperty("reviewStatus").GetString().Should().Be("Draft");

        var facts = run.GetProperty("facts").EnumerateArray().ToList();
        var scopeFact = facts.Single(f =>
            f.GetProperty("sectionType").GetString() == ExtractedFactSectionType.ScopeItem.ToString());
        var acceptedFactPublicId = scopeFact.GetProperty("publicId").GetGuid();

        foreach (var fact in facts)
        {
            if (fact.GetProperty("publicId").GetGuid() == acceptedFactPublicId)
            {
                continue;
            }

            var reviewStatus = fact.GetProperty("sectionType").GetString() == ExtractedFactSectionType.PaymentMilestone.ToString()
                ? FactReviewStatus.Rejected
                : FactReviewStatus.Uncertain;

            await client.PostAsJsonAsync(
                $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/extraction-runs/{runPublicId}/facts/{fact.GetProperty("publicId").GetGuid()}/review",
                new { reviewStatus = reviewStatus.ToString() });
        }

        var acceptReviewResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/extraction-runs/{runPublicId}/facts/{acceptedFactPublicId}/review",
            new { reviewStatus = FactReviewStatus.Accepted.ToString() });
        acceptReviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var applyResponse = await client.PostAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/extraction-runs/{runPublicId}/apply/{snapshotPublicId}",
            null);
        applyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var applyResult = await applyResponse.Content.ReadFromJsonAsync<JsonElement>();
        applyResult.GetProperty("snapshot").GetProperty("scopeItems").GetArrayLength().Should().Be(1);
        applyResult.GetProperty("snapshot").GetProperty("scopeItems")[0].GetProperty("title").GetString()
            .Should().Be("Living room interior design");
    }

    [Fact]
    public async Task InvalidProviderOutputMarksRunFailed()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"invalid-{suffix}@example.com";
        var tenantPublicId = await RegisterAndLoginAsync(client, email, "Invalid Tenant");
        await AssignProPlanAsync(_factory, email);
        var workspacePublicId = await CreateWorkspaceAsync(client, tenantPublicId, "Invalid workspace");
        var documentPublicId = await UploadInvalidFixtureDocumentAsync(client, tenantPublicId, workspacePublicId);

        var triggerResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/documents/{documentPublicId}/extraction-runs",
            new { snapshotPublicId = (Guid?)null });
        triggerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var runPublicId = (await triggerResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        await ProcessPendingJobsAsync();

        var runResponse = await client.GetAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/extraction-runs/{runPublicId}");
        var run = await runResponse.Content.ReadFromJsonAsync<JsonElement>();
        run.GetProperty("status").GetString().Should().Be("Failed");
        run.GetProperty("errorMessage").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UserCannotAccessAnotherTenantsExtractionRun()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var ownerClient = CreateAuthenticatedClient();
        var otherClient = CreateAuthenticatedClient();

        var ownerEmail = $"owner-{suffix}@example.com";
        var ownerTenant = await RegisterAndLoginAsync(ownerClient, ownerEmail, "Owner Tenant");
        await AssignProPlanAsync(_factory, ownerEmail);
        var ownerWorkspace = await CreateWorkspaceAsync(ownerClient, ownerTenant, "Owner workspace");
        var ownerDocument = await UploadDocumentAsync(ownerClient, ownerTenant, ownerWorkspace);

        await RegisterAndLoginAsync(otherClient, $"other-{suffix}@example.com", "Other Tenant");

        var triggerResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{ownerTenant}/workspaces/{ownerWorkspace}/documents/{ownerDocument}/extraction-runs",
            new { snapshotPublicId = (Guid?)null });
        var runPublicId = (await triggerResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        var crossTenantResponse = await otherClient.GetAsync(
            $"/api/v1/tenants/{ownerTenant}/workspaces/{ownerWorkspace}/extraction-runs/{runPublicId}");
        crossTenantResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private HttpClient CreateAuthenticatedClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

    private async Task ProcessPendingJobsAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<IProcessingJobProcessor>();
        (await processor.ProcessPendingAsync()).Should().BeGreaterThan(0);
    }

    private static async Task AssignProPlanAsync(PostgresWebApplicationFactory factory, string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();
        var entitlementService = scope.ServiceProvider.GetRequiredService<IEntitlementService>();
        var userId = await GetUserIdForEmailAsync(scope, email);
        var tenant = await tenantService.GetCurrentTenantForUserAsync(userId);
        tenant.Should().NotBeNull();
        await entitlementService.AssignPlanAsync(
            tenant!.TenantId,
            PlanCode.Pro,
            EntitlementSource.Trial);
    }

    private static async Task<Guid> RegisterAndLoginAsync(HttpClient client, string email, string tenantName)
    {
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "SecurePass1!",
            displayName = "Extraction Test User",
            tenantName
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = "SecurePass1!"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        return (await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me"))
            .GetProperty("tenant")
            .GetProperty("publicId")
            .GetGuid();
    }

    private static async Task<Guid> CreateWorkspaceAsync(HttpClient client, Guid tenantPublicId, string name)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces",
            new { name, type = WorkspaceType.General.ToString() });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("publicId").GetGuid();
    }

    private static async Task<Guid> UploadDocumentAsync(HttpClient client, Guid tenantPublicId, Guid workspacePublicId) =>
        await UploadDocumentBytesAsync(client, tenantPublicId, workspacePublicId, ExtractionFixturePdf, "scope-fixture.pdf");

    private static async Task<Guid> UploadInvalidFixtureDocumentAsync(
        HttpClient client,
        Guid tenantPublicId,
        Guid workspacePublicId)
    {
        var invalidPdf = Encoding.UTF8.GetBytes("%PDF-1.4\nSCOPeseal-fixture-invalid\n%%EOF");
        return await UploadDocumentBytesAsync(client, tenantPublicId, workspacePublicId, invalidPdf, "invalid-fixture.pdf");
    }

    private static async Task<Guid> UploadDocumentBytesAsync(
        HttpClient client,
        Guid tenantPublicId,
        Guid workspacePublicId,
        byte[] content,
        string fileName)
    {
        var sessionResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/upload-sessions",
            new
            {
                originalFileName = fileName,
                declaredContentType = "application/pdf",
                expectedBytes = content.Length
            });
        sessionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sessionPublicId = (await sessionResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        using var uploadContent = new MultipartFormDataContent();
        uploadContent.Add(new ByteArrayContent(content), "file", fileName);
        (await client.PutAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/upload-sessions/{sessionPublicId}/content",
            uploadContent)).StatusCode.Should().Be(HttpStatusCode.OK);

        var completeResponse = await client.PostAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/upload-sessions/{sessionPublicId}/complete",
            null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await completeResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("document")
            .GetProperty("publicId")
            .GetGuid();
    }

    private static async Task<Guid> GetUserIdForEmailAsync(AsyncServiceScope scope, string email)
    {
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.Should().NotBeNull();
        return user!.Id;
    }

    private sealed class ManualOnlyWebApplicationFactory : PostgresWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("ScopeSeal:Ai:Mode", "ManualOnly");
        }
    }
}
