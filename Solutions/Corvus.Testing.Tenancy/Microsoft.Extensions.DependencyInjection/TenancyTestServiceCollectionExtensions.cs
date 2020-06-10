// <copyright file="TenancyTestServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using Corvus.Tenancy;
    using Corvus.Testing.Tenancy;

    /// <summary>
    /// Extension methods for configuring DI for tenancy testing support.
    /// </summary>
    public static class TenancyTestServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the in-memory tenant provider to the service collection.
        /// </summary>
        /// <param name="serviceCollection">The service collection to add to.</param>
        /// <returns>The service collection, for chaining.</returns>
        public static IServiceCollection AddInMemoryTenantProvider(this IServiceCollection serviceCollection)
        {
            // Add directly and via the interface - some testing code may wish to work with the provider directly because
            // it provides some helpful methods to shortcut tenant lookup.
            serviceCollection.AddSingleton<InMemoryTenantStore>();
            serviceCollection.AddSingleton<ITenantStore>(sp => sp.GetRequiredService<InMemoryTenantStore>());
            serviceCollection.AddSingleton<ITenantProvider>(sp => sp.GetRequiredService<InMemoryTenantStore>());

            return serviceCollection;
        }
    }
}