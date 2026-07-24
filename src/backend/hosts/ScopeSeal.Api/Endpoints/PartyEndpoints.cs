using System.Security.Claims;
using ScopeSeal.Audit.Domain;
using ScopeSeal.Audit.Services;
using ScopeSeal.Identity.Authorization;
using ScopeSeal.Tenancy.Services;
using ScopeSeal.Workspaces.Services;

namespace ScopeSeal.Api.Endpoints;

public static class PartyEndpoints
{
    public static IEndpointRouteBuilder MapPartyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenants/{tenantPublicId:guid}/parties")
            .WithTags("Parties");

        group.MapGet("/", ListPartiesAsync)
            .WithName("ListParties")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        group.MapPost("/", CreatePartyAsync)
            .WithName("CreateParty")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor)
            .Produces(StatusCodes.Status201Created);

        group.MapGet("/{partyPublicId:guid}", GetPartyAsync)
            .WithName("GetParty")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        return app;
    }

    private static async Task<IResult> ListPartiesAsync(
        Guid tenantPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IPartyService partyService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var parties = await partyService.ListPartiesAsync(tenant.TenantId, cancellationToken);
        return Results.Ok(parties);
    }

    private static async Task<IResult> GetPartyAsync(
        Guid tenantPublicId,
        Guid partyPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IPartyService partyService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var party = await partyService.GetPartyAsync(
            tenant.TenantId, partyPublicId, cancellationToken);

        return party is null ? Results.NotFound() : Results.Ok(party);
    }

    private static async Task<IResult> CreatePartyAsync(
        Guid tenantPublicId,
        CreatePartyRequest request,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IPartyService partyService,
        IAuditService auditService,
        CancellationToken cancellationToken)
    {
        var userId = TenantEndpointHelpers.GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var (party, error) = await partyService.CreatePartyAsync(
            tenant.TenantId, request, cancellationToken);

        if (error is not null)
        {
            return Results.Problem(
                title: "Unable to create party",
                detail: error,
                statusCode: StatusCodes.Status404NotFound);
        }

        await auditService.RecordAsync(
            tenant.TenantId,
            AuditEventType.PartyCreated,
            "Party",
            party!.PublicId,
            userId,
            $"Party '{party.DisplayName}' created.",
            cancellationToken);

        return Results.Created(
            $"/api/v1/tenants/{tenantPublicId}/parties/{party.PublicId}",
            party);
    }
}
