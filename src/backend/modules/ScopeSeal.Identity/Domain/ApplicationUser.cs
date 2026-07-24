using Microsoft.AspNetCore.Identity;

namespace ScopeSeal.Identity.Domain;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool RequiresEmailVerification { get; set; } = true;
}
