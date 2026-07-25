namespace ScopeSeal.Tenancy.Domain;

public enum TenantRole
{
    Owner = 0,
    Admin = 1,
    Editor = 2,
    Reviewer = 3,
    ReadOnly = 4
}
