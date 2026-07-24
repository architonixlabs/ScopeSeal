using FluentAssertions;
using NetArchTest.Rules;
using ScopeSeal.Administration;
using Xunit;
using ScopeSeal.AgreementSnapshots;
using ScopeSeal.Approvals;
using ScopeSeal.Audit;
using ScopeSeal.Billing;
using ScopeSeal.ChangeLedger;
using ScopeSeal.Documents;
using ScopeSeal.Entitlements.DependencyInjection;
using ScopeSeal.Extraction;
using ScopeSeal.Identity.DependencyInjection;
using ScopeSeal.Notifications;
using ScopeSeal.Privacy;
using ScopeSeal.Shared.Abstractions;
using ScopeSeal.Tenancy;
using ScopeSeal.Workspaces;

namespace ScopeSeal.Architecture.Tests;

public sealed class ModuleBoundaryTests
{
    private static readonly (Type MarkerType, string Name)[] Modules =
    [
        (typeof(IdentityModule), "Identity"),
        (typeof(TenancyModule), "Tenancy"),
        (typeof(WorkspacesModule), "Workspaces"),
        (typeof(DocumentsModule), "Documents"),
        (typeof(AgreementSnapshotsModule), "AgreementSnapshots"),
        (typeof(ApprovalsModule), "Approvals"),
        (typeof(ChangeLedgerModule), "ChangeLedger"),
        (typeof(BillingModule), "Billing"),
        (typeof(EntitlementsModule), "Entitlements"),
        (typeof(PrivacyModule), "Privacy"),
        (typeof(AdministrationModule), "Administration"),
        (typeof(AuditModule), "Audit"),
        (typeof(NotificationsModule), "Notifications"),
        (typeof(ExtractionModule), "Extraction")
    ];

    [Fact]
    public void DomainModules_ShouldInheritModuleMarker()
    {
        foreach (var (markerType, name) in Modules)
        {
            markerType.IsAssignableTo(typeof(ModuleMarker)).Should().BeTrue($"{name} marker invalid");
        }
    }

    [Fact]
    public void DomainModules_ShouldNotReferenceApiHost()
    {
        foreach (var (markerType, name) in Modules)
        {
            var result = Types.InAssembly(markerType.Assembly)
                .ShouldNot()
                .HaveDependencyOn("ScopeSeal.Api")
                .GetResult();

            result.IsSuccessful.Should().BeTrue($"{name} must not depend on API host");
        }
    }
}
