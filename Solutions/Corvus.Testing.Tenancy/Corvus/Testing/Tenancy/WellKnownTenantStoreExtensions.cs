// <copyright file="WellKnownTenantStoreExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.Tenancy
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Corvus.Leasing;
    using Corvus.Leasing.Exceptions;
    using Corvus.Tenancy;

    /// <summary>
    /// Utilities for creating and configuring a well-known tenant for test purposes.
    /// </summary>
    public static class WellKnownTenantStoreExtensions
    {
        /// <summary>
        /// These are the well known tenant IDs for test tenants.
        /// </summary>
        /// <remarks>
        /// We have initially created ten, which should be plenty.
        /// </remarks>
        public static readonly Guid[] WellKnownTestTenantGuids =
            new[]
            {
                new Guid("69786C61-624F-4E86-BB35-75C20282D314"),
                new Guid("EABE50E2-D93F-4DAE-AA25-95A4CA129FB6"),
                new Guid("DBAAB4AA-E162-424A-97D9-11F27BDF6C65"),
                new Guid("961BD2FD-19C3-453F-AED1-15EC11099C7A"),
                new Guid("7EFD7A0A-7E95-4F2A-B0A2-F4CF1A4F30D5"),
                new Guid("8D5EFC94-3C5A-4B7F-A0FE-9F86D01E3609"),
                new Guid("90A32482-32F4-4E98-94BE-21C2B620B95D"),
                new Guid("CB832B4D-8206-4BD3-A4C9-CCA870A1DD4C"),
                new Guid("E00E4D41-162B-42EE-84D6-696AB121F82C"),
                new Guid("3CA2EA06-872E-419C-A824-FE1D1641264C"),
            };

        /// <summary>
        /// Gets a tenant for testing, and acquires a lease on it for the duration of the test.
        /// </summary>
        /// <param name="store">The store with which to create or get the tenant.</param>
        /// <param name="parentTenantId">The ID of the parent tenant for this well-known tenant.</param>
        /// <param name="name">The name of the new tenant.</param>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <returns>An instance of a well known test tenant and the lease.</returns>
        /// <exception cref="TimeoutException">The operation timed out while attempting to acquire a tenant lease.</exception>
        /// <exception cref="OperationCanceledException">The operation was cancelled by the caller.</exception>
        /// <remarks>
        /// <para>You must call <see cref="ReleaseWellKnownTestTenant(ITenantStore,TenantLease)"/> when you have finished using the tenant in order to release the lease.</para>
        /// <para>The lease auto-renews while your process is still running, and will be released after 3 seconds of your process terminating.</para>
        /// </remarks>
        public static Task<TenantLease> AcquireWellKnownTestTenant(this ITenantStore store, string parentTenantId, string name, ILeaseProvider leaseProvider)
        {
            return AcquireWellKnownTestTenant(store, parentTenantId, name, leaseProvider, TimeSpan.FromSeconds(10), CancellationToken.None);
        }

        /// <summary>
        /// Gets a tenant for testing, and acquires a lease on it for the duration of the test.
        /// </summary>
        /// <param name="store">The store with which to create or get the tenant.</param>
        /// <param name="parentTenantId">The ID of the parent tenant for this well-known tenant.</param>
        /// <param name="name">The name of the new tenant.</param>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="timeout">The maximum time to spend attempting to acquire a tenant lease before abandoning the operation.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>An instance of a well known test tenant and the lease.</returns>
        /// <exception cref="TimeoutException">The operation timed out while attempting to acquire a tenant lease.</exception>
        /// <exception cref="OperationCanceledException">The operation was cancelled by the caller.</exception>
        /// <remarks>
        /// <para>You must call <see cref="ReleaseWellKnownTestTenant(ITenantStore,TenantLease)"/> when you have finished using the tenant in order to release the lease.</para>
        /// </remarks>
        public static async Task<TenantLease> AcquireWellKnownTestTenant(this ITenantStore store, string parentTenantId, string name, ILeaseProvider leaseProvider, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (leaseProvider is null)
            {
                throw new ArgumentNullException(nameof(leaseProvider));
            }

            DateTimeOffset start = DateTimeOffset.UtcNow;

            int index = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (DateTimeOffset.UtcNow - start > timeout)
                {
                    throw new TimeoutException();
                }

                Lease? lease = null;
                var cts = new CancellationTokenSource();

                try
                {
                    lease = await leaseProvider.AcquireAutorenewingLeaseAsync(cts.Token, $"tenantlease-{parentTenantId}-{WellKnownTestTenantGuids[index]}", TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                    ITenant tenant = await store.CreateWellKnownChildTenantAsync(parentTenantId, WellKnownTestTenantGuids[index], name).ConfigureAwait(false);
                    return new TenantLease(tenant, lease, cts);
                }
                catch (LeaseAcquisitionUnsuccessfulException)
                {
                    index++;
                    if (index >= WellKnownTestTenantGuids.Length)
                    {
                        index = 0;
                    }
                }
                catch
                {
                    // If we have a failure in creating the tenant, then we clean up the lease
                    if (lease?.HasLease == true)
                    {
                        await ReleaseLeaseAsync(lease, cts).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Release a tenant lease and clean up the tenant.
        /// </summary>
        /// <param name="store">The tenant store that provided the tenant.</param>
        /// <param name="tenantLease">The leased tenant.</param>
        /// <returns>A <see cref="Task"/> which completes when the lease is released.</returns>
        public static async Task ReleaseWellKnownTestTenant(this ITenantStore store, TenantLease tenantLease)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (tenantLease is null)
            {
                throw new ArgumentNullException(nameof(tenantLease));
            }

            try
            {
                await store.DeleteTenantAsync(tenantLease.Tenant.Id).ConfigureAwait(false);
            }
            finally
            {
                await ReleaseLeaseAsync(tenantLease.Lease, tenantLease.CancellationTokenSource).ConfigureAwait(false);
            }
        }

        private static async Task ReleaseLeaseAsync(Lease lease, CancellationTokenSource cts)
        {
            cts.Cancel();
            cts.Dispose();
            await lease.ReleaseAsync().ConfigureAwait(false);
        }
    }
}
