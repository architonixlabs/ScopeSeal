namespace ScopeSeal.Api.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/system").WithTags("System");

        group.MapGet("/status", () => Results.Ok(new
        {
            service = "ScopeSeal.Api",
            status = "ok",
            utc = DateTime.UtcNow
        }))
        .WithName("GetSystemStatus")
        .WithSummary("Returns basic API availability information.");

        return app;
    }
}
