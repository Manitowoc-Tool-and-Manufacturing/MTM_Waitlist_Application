using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MTM_Waitlist_Application.Core.Interfaces.Api;
using MTM_Waitlist_Application.Core.Interfaces.Auth;
using MTM_Waitlist_Application.Core.Interfaces.Sync;
using MTM_Waitlist_Application.Core.Interfaces.Waitlist;
using MTM_Waitlist_Application.Data.Http;
using MTM_Waitlist_Application.Data.Local;
using MTM_Waitlist_Application.Data.Repositories.Waitlist;
using MTM_Waitlist_Application.Feature.Dashboard.ViewModels.Main;
using MTM_Waitlist_Application.Feature.Dashboard.Views.Main;
using MTM_Waitlist_Application.Services.Auth;
using MTM_Waitlist_Application.Services.Sync;
using MTM_Waitlist_Application.Services.Waitlist;

namespace MTM_Waitlist_Application.Tests.Unit.Shared.Success;

public class MauiProgramExtensionsTests
{
    [Fact]
    public void AddSharedServices_ShouldRegisterExpectedDependencies_WhenInvoked()
    {
        var services = new ServiceCollection();
        var extensionType = typeof(MTM_Waitlist_Application.MauiProgramExtensions).Assembly
            .GetType("MTM_Waitlist_Application.ServiceCollectionExtensions");

        extensionType.Should().NotBeNull();

        var addSharedServicesMethod = extensionType!.GetMethod("AddSharedServices");
        addSharedServicesMethod.Should().NotBeNull();

        addSharedServicesMethod!.Invoke(null, [services]);

        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(LocalDbContext) && descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IApiClient) && descriptor.ImplementationType == typeof(HttpApiClient) && descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IRepository_WaitlistEntry) && descriptor.ImplementationType == typeof(Repository_WaitlistEntry) && descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IRepository_WaitlistEntryLocal) && descriptor.ImplementationType == typeof(Repository_WaitlistEntryLocal) && descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IService_Auth) && descriptor.ImplementationType == typeof(Service_Auth) && descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IService_WaitlistEntry) && descriptor.ImplementationType == typeof(Service_WaitlistEntry) && descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ISyncService) && descriptor.ImplementationType == typeof(SyncService) && descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ViewModel_Dashboard_Main) && descriptor.Lifetime == ServiceLifetime.Transient);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(View_Dashboard_Main) && descriptor.Lifetime == ServiceLifetime.Transient);
    }
}