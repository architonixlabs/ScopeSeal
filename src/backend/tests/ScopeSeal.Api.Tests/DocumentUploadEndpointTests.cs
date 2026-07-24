using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ScopeSeal.Workspaces.Domain;

namespace ScopeSeal.Api.Tests;

public sealed class DocumentUploadEndpointTests(PostgresWebApplicationFactory factory) : IClassFixture<PostgresWebApplicationFactory>
{
    private readonly PostgresWebApplicationFactory _factory = factory;

    private static readonly byte[] MinimalPdf =
        "%PDF-1.4\n1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\ntrailer\n<< /Size 4 /Root 1 0 R >>\nstartxref\n149\n%%EOF"u8.ToArray();

    [Fact]
    public async Task UploadCompleteAndDownloadPdf_Succeeds()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"doc-{suffix}@example.com", "Document Tenant");
        var workspacePublicId = await CreateWorkspaceAsync(client, tenantPublicId, "Upload workspace");

        var sessionResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/upload-sessions",
            new
            {
                originalFileName = "scope.pdf",
                declaredContentType = "application/pdf",
                expectedBytes = MinimalPdf.Length
            });

        sessionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var session = await sessionResponse.Content.ReadFromJsonAsync<JsonElement>();
        var sessionPublicId = session.GetProperty("publicId").GetGuid();
        session.GetProperty("serverFileName").GetString().Should().NotContain("scope.pdf");

        using var uploadContent = new MultipartFormDataContent();
        uploadContent.Add(new ByteArrayContent(MinimalPdf), "file", "scope.pdf");

        var uploadResponse = await client.PutAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/upload-sessions/{sessionPublicId}/content",
            uploadContent);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var completeResponse = await client.PostAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/upload-sessions/{sessionPublicId}/complete",
            null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var complete = await completeResponse.Content.ReadFromJsonAsync<JsonElement>();
        var documentPublicId = complete.GetProperty("document").GetProperty("publicId").GetGuid();
        complete.GetProperty("document").GetProperty("preview").GetProperty("isPreviewSafe").GetBoolean().Should().BeTrue();

        var listResponse = await client.GetAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/documents");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        list.GetArrayLength().Should().Be(1);

        var tokenResponse = await client.PostAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/documents/{documentPublicId}/download-token",
            null);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = tokenPayload.GetProperty("token").GetGuid();

        var downloadResponse = await client.GetAsync(
            $"/api/v1/tenants/{tenantPublicId}/documents/download?token={token}");
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var downloaded = await downloadResponse.Content.ReadAsByteArrayAsync();
        downloaded.Should().BeEquivalentTo(MinimalPdf);
    }

    [Fact]
    public async Task UserCannotAccessAnotherTenantsUploadSession()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var userAClient = CreateAuthenticatedClient();
        var userBClient = CreateAuthenticatedClient();

        var tenantA = await RegisterAndLoginAsync(userAClient, $"doc-a-{suffix}@example.com", "Tenant A");
        var tenantB = await RegisterAndLoginAsync(userBClient, $"doc-b-{suffix}@example.com", "Tenant B");
        var workspaceB = await CreateWorkspaceAsync(userBClient, tenantB, "Tenant B workspace");

        var sessionResponse = await userBClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantB}/workspaces/{workspaceB}/upload-sessions",
            new
            {
                originalFileName = "private.pdf",
                declaredContentType = "application/pdf",
                expectedBytes = MinimalPdf.Length
            });
        sessionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sessionPublicId = (await sessionResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        var crossTenantGet = await userAClient.GetAsync(
            $"/api/v1/tenants/{tenantB}/workspaces/{workspaceB}/upload-sessions/{sessionPublicId}");
        crossTenantGet.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var uploadContent = new MultipartFormDataContent();
        uploadContent.Add(new ByteArrayContent(MinimalPdf), "file", "private.pdf");

        var crossTenantUpload = await userAClient.PutAsync(
            $"/api/v1/tenants/{tenantB}/workspaces/{workspaceB}/upload-sessions/{sessionPublicId}/content",
            uploadContent);
        crossTenantUpload.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var ownList = await userAClient.GetAsync(
            $"/api/v1/tenants/{tenantA}/workspaces/{workspaceB}/documents");
        ownList.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RejectsBlockedContentType()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"ctype-{suffix}@example.com", "Content Type Tenant");
        var workspacePublicId = await CreateWorkspaceAsync(client, tenantPublicId, "Content workspace");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/upload-sessions",
            new
            {
                originalFileName = "payload.exe",
                declaredContentType = "application/pdf",
                expectedBytes = 1024
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RejectsContentTypeSpoofing()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"spoof-{suffix}@example.com", "Spoof Tenant");
        var workspacePublicId = await CreateWorkspaceAsync(client, tenantPublicId, "Spoof workspace");

        var sessionResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/upload-sessions",
            new
            {
                originalFileName = "image.png",
                declaredContentType = "image/png",
                expectedBytes = 128
            });
        sessionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sessionPublicId = (await sessionResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        var fakePng = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 };
        using var uploadContent = new MultipartFormDataContent();
        uploadContent.Add(new ByteArrayContent(fakePng), "file", "image.png");

        var uploadResponse = await client.PutAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/upload-sessions/{sessionPublicId}/content",
            uploadContent);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RejectsEicarTestSignature()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"eicar-{suffix}@example.com", "Eicar Tenant");
        var workspacePublicId = await CreateWorkspaceAsync(client, tenantPublicId, "Eicar workspace");

        var eicarBytes = Encoding.UTF8.GetBytes("EICAR-STANDARD-ANTIVIRUS-TEST-FILE");
        var sessionResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/upload-sessions",
            new
            {
                originalFileName = "notes.txt",
                declaredContentType = "text/plain",
                expectedBytes = eicarBytes.Length
            });
        sessionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sessionPublicId = (await sessionResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        using var uploadContent = new MultipartFormDataContent();
        uploadContent.Add(new ByteArrayContent(eicarBytes), "file", "notes.txt");

        var uploadResponse = await client.PutAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/upload-sessions/{sessionPublicId}/content",
            uploadContent);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var completeResponse = await client.PostAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/upload-sessions/{sessionPublicId}/complete",
            null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RejectsOversizedDeclaredUpload()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"size-{suffix}@example.com", "Size Tenant");
        var workspacePublicId = await CreateWorkspaceAsync(client, tenantPublicId, "Size workspace");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/upload-sessions",
            new
            {
                originalFileName = "large.pdf",
                declaredContentType = "application/pdf",
                expectedBytes = 26_000_000
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
            displayName = "Document Test User",
            tenantName
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
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("publicId").GetGuid();
    }
}
