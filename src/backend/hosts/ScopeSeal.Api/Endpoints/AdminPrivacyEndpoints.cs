using Microsoft.Extensions.Options;
using ScopeSeal.Administration.Configuration;
using ScopeSeal.Api.Authorization;
using ScopeSeal.Privacy.Domain;
using ScopeSeal.Privacy.Services;

namespace ScopeSeal.Api.Endpoints;

public static class AdminPrivacyEndpoints
{
    public static IEndpointRouteBuilder MapAdminPrivacyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/privacy")
            .WithTags("Admin Privacy");

        group.MapGet("/queue", ListQueueAsync)
            .WithName("ListAdminPrivacyQueue");

        group.MapPatch("/queue/{queuePublicId:guid}", UpdateQueueItemAsync)
            .WithName("UpdateAdminPrivacyQueueItem");

        group.MapPost("/jobs/process-pending", ProcessPendingJobsAsync)
            .WithName("ProcessPendingPrivacyJobs");

        group.MapPost("/jobs/retention-scan", RunRetentionScanAsync)
            .WithName("RunRetentionFoundationJob");

        return app;
    }

    private static async Task<IResult> ListQueueAsync(
        HttpRequest httpRequest,
        IPrivacyService privacyService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!AdminOperatorAuth.IsAuthorized(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var items = await privacyService.ListAdminQueueAsync(cancellationToken);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> UpdateQueueItemAsync(
        Guid queuePublicId,
        UpdateAdminQueueItemRequest request,
        HttpRequest httpRequest,
        IPrivacyService privacyService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!AdminOperatorAuth.IsAuthorized(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var (item, error) = await privacyService.UpdateAdminQueueItemAsync(
            queuePublicId,
            request,
            cancellationToken);

        if (item is null)
        {
            return Results.Problem(
                title: "Queue update failed",
                detail: error,
                statusCode: StatusCodes.Status404NotFound);
        }

        return Results.Ok(item);
    }

    private static async Task<IResult> ProcessPendingJobsAsync(
        HttpRequest httpRequest,
        IPrivacyService privacyService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!AdminOperatorAuth.IsAuthorized(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var processed = await privacyService.ProcessPendingPrivacyJobsAsync(cancellationToken);
        return Results.Ok(new { processed });
    }

    private static async Task<IResult> RunRetentionScanAsync(
        HttpRequest httpRequest,
        IPrivacyService privacyService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!AdminOperatorAuth.IsAuthorized(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var recordsProcessed = await privacyService.RunRetentionFoundationJobAsync(cancellationToken);
        return Results.Ok(new { recordsProcessed });
    }
}
