namespace ScopeSeal.Identity.Authorization;

public static class ScopeSealPolicies
{
    public const string Authenticated = "Authenticated";
    public const string TenantMember = "TenantMember";
    public const string TenantAdmin = "TenantAdmin";
    public const string TenantOwner = "TenantOwner";
}

public static class ScopeSealClaimTypes
{
    public const string TenantId = "tenant_id";
    public const string TenantPublicId = "tenant_public_id";
    public const string TenantRole = "tenant_role";
}
