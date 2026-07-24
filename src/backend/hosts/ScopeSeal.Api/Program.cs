using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ScopeSeal.Api.Endpoints;
using ScopeSeal.Api.Middleware;
using ScopeSeal.AgreementSnapshots.DependencyInjection;
using ScopeSeal.Approvals.DependencyInjection;
using ScopeSeal.Audit.DependencyInjection;
using ScopeSeal.Documents.DependencyInjection;
using ScopeSeal.Entitlements.DependencyInjection;
using ScopeSeal.Identity.DependencyInjection;
using ScopeSeal.Infrastructure.DependencyInjection;
using ScopeSeal.Shared.DependencyInjection;
using ScopeSeal.Tenancy;
using ScopeSeal.Workspaces.DependencyInjection;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(new ActivitySource("ScopeSeal.Api"));

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithThreadId()
        .WriteTo.Console());

builder.Services.AddScopeSealShared(builder.Configuration);
builder.Services.AddIdentityModule();
builder.Services.AddTenancyModule();
builder.Services.AddEntitlementsModule(builder.Configuration);
builder.Services.AddWorkspacesModule();
builder.Services.AddDocumentsModule();
builder.Services.AddAgreementSnapshotsModule();
builder.Services.AddApprovalsModule();
builder.Services.AddAuditModule();

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:Default is required.");
}

builder.Services.AddScopeSealInfrastructure(connectionString, builder.Environment);

builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "ScopeSeal API";
        document.Info.Version = "v1";
        document.Info.Description =
            "ScopeSeal communication-clarity and scope-management platform API.";
        return Task.CompletedTask;
    });
});

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql", tags: ["ready"]);

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Testing")
{
    await app.Services.ApplyMigrationsAsync();
}

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
    };
});

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var problem = new ProblemDetails
        {
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = app.Environment.IsDevelopment() ? exception?.Message : null,
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier
            }
        };

        context.Response.StatusCode = problem.Status.Value;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    });
});

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantContextMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health");

app.MapSystemEndpoints();
app.MapAuthEndpoints();
app.MapTenantEndpoints();
app.MapEntitlementEndpoints();
app.MapDashboardEndpoints();
app.MapWorkspaceEndpoints();
app.MapContactEndpoints();
app.MapPartyEndpoints();
app.MapWorkspaceTemplateEndpoints();
app.MapUploadSessionEndpoints();
app.MapDocumentEndpoints();
app.MapAgreementSnapshotEndpoints();
app.MapReviewApprovalEndpoints();
app.MapExternalReviewEndpoints();

app.Run();

public partial class Program;
