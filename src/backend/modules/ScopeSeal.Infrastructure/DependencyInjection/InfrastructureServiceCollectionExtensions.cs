using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ScopeSeal.AgreementSnapshots.Services;
using ScopeSeal.Audit.Services;
using ScopeSeal.Documents.Services;
using ScopeSeal.Entitlements.Services;
using ScopeSeal.Identity.Domain;
using ScopeSeal.Identity.Services;
using ScopeSeal.Infrastructure.Persistence;
using ScopeSeal.Infrastructure.Security;
using ScopeSeal.Infrastructure.Services;
using ScopeSeal.Infrastructure.Storage;
using ScopeSeal.Shared.Configuration;
using ScopeSeal.Tenancy.Services;
using ScopeSeal.Workspaces.Services;

namespace ScopeSeal.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    private static readonly SemaphoreSlim MigrationLock = new(1, 1);

    public static IServiceCollection AddScopeSealInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IHostEnvironment? environment = null)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "__Host-ScopeSeal";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = environment is not null &&
                    (environment.IsDevelopment() || environment.EnvironmentName == "Testing")
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<IOptions<ScopeSealOptions>>((cookieOptions, scopeSealOptions) =>
            {
                cookieOptions.ExpireTimeSpan = TimeSpan.FromHours(scopeSealOptions.Value.Auth.CookieExpirationHours);
            });

        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IUserAuthenticationService, AuthenticationService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IEntitlementService, EntitlementService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IPartyService, PartyService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IWorkspaceTemplateService, WorkspaceTemplateService>();
        services.AddScoped<IUploadSessionService, UploadSessionService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IAgreementSnapshotService, AgreementSnapshotService>();
        services.AddSingleton<IContentTypeValidator, ContentTypeValidator>();
        services.AddSingleton<IMalwareScanner, DevelopmentMalwareScanner>();

        if (environment?.EnvironmentName == "Testing")
        {
            services.AddSingleton<IBlobStorageService, InMemoryBlobStorageService>();
        }
        else
        {
            services.AddSingleton<IBlobStorageService, AzuriteBlobStorageService>();
        }

        services.AddScoped<PlanCatalogSeeder>();
        services.AddScoped<WorkspaceTemplateSeeder>();

        return services;
    }

    public static async Task ApplyMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await MigrationLock.WaitAsync(cancellationToken);
        try
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync(cancellationToken);

            var seeder = scope.ServiceProvider.GetRequiredService<PlanCatalogSeeder>();
            await seeder.SeedAsync(cancellationToken);

            var templateSeeder = scope.ServiceProvider.GetRequiredService<WorkspaceTemplateSeeder>();
            await templateSeeder.SeedAsync(cancellationToken);
        }
        finally
        {
            MigrationLock.Release();
        }
    }
}
