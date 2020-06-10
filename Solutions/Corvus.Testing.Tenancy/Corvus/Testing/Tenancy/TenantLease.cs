// <copyright file="TenantLease.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.Tenancy
{
    using System;
    using System.Threading;
    using Corvus.Leasing;
    using Corvus.Tenancy;

    /// <summary>
    /// A tenant and the associated lease.
    /// </summary>
    /// <remarks>
    /// This is created when you call <see cref="WellKnownTenantStoreExtensions.AcquireWellKnownTestTenant(Corvus.Tenancy.ITenantStore, string, string, Leasing.ILeaseProvider)"/>. It will be
    /// disposed when it is passed to <see cref="WellKnownTenantStoreExtensions.ReleaseWellKnownTestTenant(Corvus.Tenancy.ITenantStore, TenantLease)"/>.
    /// </remarks>
    public class TenantLease
    {
        /// <summary>
        /// A tenant and its associated lease.
        /// </summary>
        /// <param name="tenant">The tenant that has been leased.</param>
        /// <param name="lease">The lease for the tenant, that must be released when the tenant is no longer in use.</param>
        /// <param name="cts">The cancellation token source for releasing the lease.</param>
        internal TenantLease(ITenant tenant, Lease lease, CancellationTokenSource cts)
        {
            this.Lease = lease ?? throw new ArgumentNullException(nameof(lease));
            this.Tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
            this.CancellationTokenSource = cts;
        }

        /// <summary>
        /// Gets the leased tenant.
        /// </summary>
        public ITenant Tenant { get; }

        /// <summary>
        /// Gets the lease for the tenant which must be released when the tenant is no longer required.
        /// </summary>
        public Lease Lease { get; }

        /// <summary>
        /// Gets the cancellation token source to use to release the renewing lease.
        /// </summary>
        internal CancellationTokenSource CancellationTokenSource { get; }
    }
}
