using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ScopeSeal.Identity.Authorization;
using ScopeSeal.Identity.Services;
using ScopeSeal.Tenancy.Services;

namespace ScopeSeal.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Authentication");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithSummary("Register a new user and default tenant.")
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Sign in with email and password.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .RequireAuthorization(ScopeSealPolicies.Authenticated)
            .WithSummary("Sign out the current session.")
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/me", GetCurrentUserAsync)
            .WithName("GetCurrentUser")
            .RequireAuthorization(ScopeSealPolicies.TenantMember)
            .WithSummary("Returns the authenticated user and tenant context.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequestDto request,
        IRegistrationService registrationService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.DisplayName) ||
            string.IsNullOrWhiteSpace(request.TenantName))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["email"] = ["Email is required."],
                ["password"] = ["Password is required."],
                ["displayName"] = ["Display name is required."],
                ["tenantName"] = ["Tenant name is required."]
            });
        }

        if (!request.ConfirmedAge18OrAbove)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["confirmedAge18OrAbove"] = ["You must confirm that you are aged 18 or above to register."]
            });
        }

        var result = await registrationService.RegisterAsync(
            new RegisterRequest(
                request.Email,
                request.Password,
                request.DisplayName,
                request.TenantName,
                request.ConfirmedAge18OrAbove),
            cancellationToken);

        if (!result.Succeeded)
        {
            var errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description });
            var status = result.Errors.Any(e => e.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;
            return Results.ValidationProblem(errors, statusCode: status);
        }

        return Results.Created("/api/v1/auth/me", new { message = "Registration successful." });
    }

    private static async Task<IResult> LoginAsync(
        LoginRequestDto request,
        IUserAuthenticationService authenticationService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["email"] = ["Email is required."],
                ["password"] = ["Password is required."]
            });
        }

        var result = await authenticationService.SignInAsync(
            new LoginRequest(request.Email, request.Password),
            httpContext,
            cancellationToken);

        if (!result.Succeeded)
        {
            return Results.Problem(
                title: result.EmailNotConfirmed ? "Email verification required" : "Sign-in failed",
                detail: result.Error,
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> LogoutAsync(
        IUserAuthenticationService authenticationService,
        HttpContext httpContext)
    {
        await authenticationService.SignOutAsync(httpContext);
        return Results.NoContent();
    }

    private static async Task<IResult> GetCurrentUserAsync(
        ClaimsPrincipal user,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var tenant = await tenantService.GetCurrentTenantForUserAsync(userId, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new
        {
            userId,
            email = user.FindFirstValue(ClaimTypes.Email),
            displayName = user.FindFirstValue(ClaimTypes.Name),
            tenant = new
            {
                tenant.PublicId,
                tenant.Name,
                tenant.Role,
                tenant.CreatedAtUtc
            }
        });
    }

    private sealed record RegisterRequestDto(
        string Email,
        string Password,
        string DisplayName,
        string TenantName,
        bool ConfirmedAge18OrAbove);

    private sealed record LoginRequestDto(string Email, string Password);
}
