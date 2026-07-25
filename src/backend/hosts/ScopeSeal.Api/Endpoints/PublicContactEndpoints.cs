using ScopeSeal.Infrastructure.Services.Email;

namespace ScopeSeal.Api.Endpoints;

public static class PublicContactEndpoints
{
    public static IEndpointRouteBuilder MapPublicContactEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/public/contact", SubmitContactAsync)
            .WithTags("Public")
            .WithName("SubmitPublicContact")
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> SubmitContactAsync(
        PublicContactRequest request,
        IOutboundEmailService outboundEmail,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["message"] = ["Message is required."]
            });
        }

        var body =
            $"Contact form submission\n\n" +
            $"Name: {request.Name?.Trim() ?? "(not provided)"}\n" +
            $"Email: {request.Email?.Trim() ?? "(not provided)"}\n\n" +
            request.Message.Trim();

        var accepted = await outboundEmail.SendAsync(
            request.Name,
            request.Email,
            request.Subject?.Trim() ?? "ScopeSeal contact form",
            body,
            cancellationToken);

        return accepted
            ? Results.Accepted(value: new { status = "queued" })
            : Results.Problem(
                title: "Could not send message",
                statusCode: StatusCodes.Status502BadGateway);
    }

    private sealed record PublicContactRequest(
        string? Name,
        string? Email,
        string? Subject,
        string Message);
}
