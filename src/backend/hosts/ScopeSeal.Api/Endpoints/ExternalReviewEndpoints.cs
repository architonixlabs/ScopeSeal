using ScopeSeal.Approvals.Services;

namespace ScopeSeal.Api.Endpoints;

public static class ExternalReviewEndpoints
{
    public static IEndpointRouteBuilder MapExternalReviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/external/review/{token:guid}")
            .WithTags("External Review")
            .AllowAnonymous();

        group.MapGet("/", GetSnapshotForReviewAsync)
            .WithName("GetSnapshotForExternalReview");

        group.MapPost("/comments", AddCommentAsync)
            .WithName("AddExternalReviewComment");

        group.MapPost("/change-suggestions", AddChangeSuggestionAsync)
            .WithName("AddExternalChangeSuggestion");

        group.MapPost("/request-changes", RequestChangesAsync)
            .WithName("RequestExternalChanges");

        group.MapPost("/approve", ApproveSnapshotAsync)
            .WithName("ApproveSnapshotExternally");

        return app;
    }

    private static async Task<IResult> GetSnapshotForReviewAsync(
        Guid token,
        IReviewApprovalService reviewApprovalService,
        CancellationToken cancellationToken)
    {
        var review = await reviewApprovalService.GetSnapshotForReviewAsync(token, cancellationToken);
        return review is null ? Results.NotFound() : Results.Ok(review);
    }

    private static async Task<IResult> AddCommentAsync(
        Guid token,
        AddReviewCommentRequest request,
        IReviewApprovalService reviewApprovalService,
        CancellationToken cancellationToken)
    {
        var (comment, error) = await reviewApprovalService.AddCommentAsync(token, request, cancellationToken);

        if (comment is null && error is null)
        {
            return Results.NotFound();
        }

        if (error is not null)
        {
            return Results.Problem(
                title: "Comment denied",
                detail: error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Created($"/api/v1/external/review/{token}/comments/{comment!.PublicId}", comment);
    }

    private static async Task<IResult> AddChangeSuggestionAsync(
        Guid token,
        AddChangeSuggestionRequest request,
        IReviewApprovalService reviewApprovalService,
        CancellationToken cancellationToken)
    {
        var (suggestion, error) = await reviewApprovalService.AddChangeSuggestionAsync(token, request, cancellationToken);

        if (suggestion is null && error is null)
        {
            return Results.NotFound();
        }

        if (error is not null)
        {
            return Results.Problem(
                title: "Change suggestion denied",
                detail: error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Created(
            $"/api/v1/external/review/{token}/change-suggestions/{suggestion!.PublicId}",
            suggestion);
    }

    private static async Task<IResult> RequestChangesAsync(
        Guid token,
        IReviewApprovalService reviewApprovalService,
        CancellationToken cancellationToken)
    {
        var (snapshot, error) = await reviewApprovalService.RequestChangesAsync(token, cancellationToken);

        if (snapshot is null && error is null)
        {
            return Results.NotFound();
        }

        if (error is not null)
        {
            return Results.Problem(
                title: "Request changes denied",
                detail: error,
                statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Ok(snapshot);
    }

    private static async Task<IResult> ApproveSnapshotAsync(
        Guid token,
        ApproveSnapshotRequest request,
        IReviewApprovalService reviewApprovalService,
        CancellationToken cancellationToken)
    {
        var (approval, error) = await reviewApprovalService.ApproveSnapshotAsync(token, request, cancellationToken);

        if (approval is null && error is null)
        {
            return Results.NotFound();
        }

        if (error is not null)
        {
            return Results.Problem(
                title: "Approval denied",
                detail: error,
                statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Ok(approval);
    }
}
