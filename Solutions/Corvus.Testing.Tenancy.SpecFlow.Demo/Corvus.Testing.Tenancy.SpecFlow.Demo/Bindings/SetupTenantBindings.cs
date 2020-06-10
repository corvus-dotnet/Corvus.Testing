// <copyright file="SetupTenantBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.Tenancy.SpecFlow.Demo.Bindings
{
    using System.Threading.Tasks;
    using Corvus.Azure.Cosmos.Tenancy;
    using Corvus.Leasing;
    using Corvus.Tenancy;
    using Corvus.Testing.SpecFlow;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using TechTalk.SpecFlow;

    /// <summary>
    /// Set up tenant bindings in the container.
    /// </summary>
    [Binding]
    public static class SetupTenantBindings
    {
        [BeforeFeature("@useWellKnownTenant", Order = ContainerBeforeFeatureOrder.PopulateServiceCollection)]
        public static void SetupContainer(FeatureContext featureContext)
        {
            ContainerBindings.ConfigureServices(featureContext, services =>
            {
                IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();

                IConfiguration root = configurationBuilder.Build();

                string azureServicesAuthConnectionString = root["AzureServicesAuthConnectionString"];

                services.AddSingleton(root);

                services.AddLogging();

                services.AddRootTenant();
                services.AddInMemoryTenantProvider();
                services.AddJsonSerializerSettings();
                services.AddInMemoryLeasing();
            });
        }

        [BeforeFeature("@useWellKnownTenant", Order = ContainerBeforeFeatureOrder.ServiceProviderAvailable)]
        public static async Task SetupWellKnownTenant(FeatureContext featureContext)
        {
            ITenantStore tenantStore = ContainerBindings.GetServiceProvider(featureContext).GetRequiredService<ITenantStore>();
            ILeaseProvider leaseProvider = ContainerBindings.GetServiceProvider(featureContext).GetRequiredService<ILeaseProvider>();
            TenantLease testTenantLease = await tenantStore.AcquireWellKnownTestTenant(tenantStore.Root.Id, "Test tenant", leaseProvider).ConfigureAwait(false);
            featureContext.Set(testTenantLease);
            featureContext.Set(testTenantLease.Tenant);
        }

        [AfterFeature("@useWellKnownTenant", Order = 100000)]
        public static async Task TeardownWellKnownTenant(FeatureContext featureContext)
        {
            TenantLease testTenantLease = featureContext.Get<TenantLease>();
            ITenantStore tenantStore = ContainerBindings.GetServiceProvider(featureContext).GetRequiredService<ITenantStore>();
            await featureContext.RunAndStoreExceptionsAsync(() => tenantStore.ReleaseWellKnownTestTenant(testTenantLease)).ConfigureAwait(false);
        }
    }
}
