using System.Security.Claims;
using ScopeSeal.Audit.Domain;
using ScopeSeal.Audit.Services;
using ScopeSeal.Identity.Authorization;
using ScopeSeal.Tenancy.Services;
using ScopeSeal.Workspaces.Services;

namespace ScopeSeal.Api.Endpoints;

public static class ContactEndpoints
{
    public static IEndpointRouteBuilder MapContactEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenants/{tenantPublicId:guid}/contacts")
            .WithTags("Contacts");

        group.MapGet("/", ListContactsAsync)
            .WithName("ListContacts")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        group.MapPost("/", CreateContactAsync)
            .WithName("CreateContact")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor)
            .Produces(StatusCodes.Status201Created);

        group.MapGet("/{contactPublicId:guid}", GetContactAsync)
            .WithName("GetContact")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        return app;
    }

    private static async Task<IResult> ListContactsAsync(
        Guid tenantPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IContactService contactService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var contacts = await contactService.ListContactsAsync(tenant.TenantId, cancellationToken);
        return Results.Ok(contacts);
    }

    private static async Task<IResult> GetContactAsync(
        Guid tenantPublicId,
        Guid contactPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IContactService contactService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var contact = await contactService.GetContactAsync(
            tenant.TenantId, contactPublicId, cancellationToken);

        return contact is null ? Results.NotFound() : Results.Ok(contact);
    }

    private static async Task<IResult> CreateContactAsync(
        Guid tenantPublicId,
        CreateContactRequest request,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IContactService contactService,
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

        var contact = await contactService.CreateContactAsync(tenant.TenantId, request, cancellationToken);

        await auditService.RecordAsync(
            tenant.TenantId,
            AuditEventType.ContactCreated,
            "Contact",
            contact.PublicId,
            userId,
            $"Contact '{contact.DisplayName}' created.",
            cancellationToken);

        return Results.Created(
            $"/api/v1/tenants/{tenantPublicId}/contacts/{contact.PublicId}",
            contact);
    }
}
