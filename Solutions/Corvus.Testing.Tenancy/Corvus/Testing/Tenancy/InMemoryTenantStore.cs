// <copyright file="InMemoryTenantStore.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.Tenancy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Corvus.Extensions.Json;
    using Corvus.Json;
    using Corvus.Tenancy;
    using Corvus.Tenancy.Exceptions;
    using Newtonsoft.Json;

    /// <summary>
    /// In-memory implementation of ITenantProvider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tenants are stored in internal List{T} and Dictionary{T, T}; as such, this implementation should not be considered
    /// thread-safe.
    /// </para>
    /// <para>
    /// In order to emulate the behaviour of tenant providers that store their data in an external store (or the
    /// <c>ClientTenantProvider</c>, which internally calls the tenancy REST API), data is stored internally in serialized
    /// form. When <see cref="ITenant"/> instances are requested (e.g. via <see cref="GetTenantAsync(string, string)"/>, the
    /// appropriate data is retrieved and deserialized to an <see cref="ITenant"/> before being returned. This means that
    /// multiple calls to a method such as <see cref="GetTenantAsync(string, string)"/> will return different object instances.
    /// This is done to avoid any potential issues with tests passing when using the <see cref="InMemoryTenantStore"/> but
    /// failing when switching to a real implementation. For example, a possible cause of this would be a spec testing an
    /// operation that updates a tenant and later verifying that those changes have been made. If we simply stored the tenant
    /// in memory, it would be possible for the code under test to omit calling
    /// <see cref="UpdateTenantAsync(string, string?, IEnumerable{KeyValuePair{string, object}}?, IEnumerable{string}?)"/>, but for the test which checks that the updates have been made to still
    /// succeed. By ensuring that a unique <see cref="ITenant"/> is returned each time, we avoid this and similar problems.
    /// </para>
    /// </remarks>
    public class InMemoryTenantStore : ITenantStore
    {
        private readonly IJsonSerializerSettingsProvider jsonSerializerSettingsProvider;
        private readonly List<StoredTenant> allTenants = new List<StoredTenant>();
        private readonly Dictionary<string, List<string>> tenantsByParent = new Dictionary<string, List<string>>();
        private readonly IPropertyBagFactory propertyBagFactory;

        /// <summary>
        /// Creates a new instance of the <see cref="InMemoryTenantStore"/> class.
        /// </summary>
        /// <param name="rootTenant">The root tenant.</param>
        /// <param name="jsonSerializerSettingsProvider">The serialization settings provider.</param>
        /// <param name="propertyBagFactory">Provides the ability to create and modify property bags.</param>
        public InMemoryTenantStore(
            RootTenant rootTenant,
            IJsonSerializerSettingsProvider jsonSerializerSettingsProvider,
            IPropertyBagFactory propertyBagFactory)
        {
            this.Root = rootTenant;
            this.jsonSerializerSettingsProvider = jsonSerializerSettingsProvider;
            this.propertyBagFactory = propertyBagFactory;
        }

        /// <inheritdoc/>
        public RootTenant Root { get; }

        /// <inheritdoc/>
        public Task<ITenant> CreateChildTenantAsync(string parentTenantId, string name)
        {
            return this.CreateWellKnownChildTenantAsync(parentTenantId, Guid.NewGuid(), name);
        }

        /// <inheritdoc/>
        public async Task<ITenant> CreateWellKnownChildTenantAsync(string parentTenantId, Guid wellKnownChildTenantGuid, string name)
        {
            ITenant parent = await this.GetTenantAsync(parentTenantId).ConfigureAwait(false);
            var newTenant = new Tenant(
                parent.Id.CreateChildId(wellKnownChildTenantGuid),
                name,
                this.propertyBagFactory.Create(PropertyBagValues.Empty));

            List<string> childrenList = this.GetChildren(parent.Id);
            childrenList.Add(newTenant.Id);
            this.allTenants.Add(new StoredTenant(newTenant, this.jsonSerializerSettingsProvider.Instance));

            return newTenant;
        }

        /// <inheritdoc/>
        public Task DeleteTenantAsync(string tenantId)
        {
            if (this.tenantsByParent.TryGetValue(tenantId, out List<string>? children) && children.Count > 0)
            {
                throw new InvalidOperationException("Cannot delete a tenant with children.");
            }

            StoredTenant storedTenant = this.allTenants.Single(x => x.Id == tenantId);
            this.allTenants.Remove(storedTenant);

            string parentTenantId = storedTenant.Tenant.GetRequiredParentId();

            List<string> siblings = this.tenantsByParent[parentTenantId];
            siblings.Remove(tenantId);

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<TenantCollectionResult> GetChildrenAsync(string tenantId, int limit = 20, string? continuationToken = null)
        {
            ITenant parent = await this.GetTenantAsync(tenantId).ConfigureAwait(false);

            List<string> children = this.GetChildren(parent.Id);

            int skip = 0;

            if (!string.IsNullOrEmpty(continuationToken))
            {
                skip = int.Parse(continuationToken);
            }

            IEnumerable<string> tenants = children.Skip(skip).Take(limit);

            continuationToken = tenants.Count() == limit ? (skip + limit).ToString() : null;

            return new TenantCollectionResult(tenants, continuationToken);
        }

        /// <inheritdoc/>
        public Task<ITenant> GetTenantAsync(string tenantId, string? eTag = null)
        {
            ITenant? tenant = tenantId == this.Root.Id
                ? this.Root
                : this.allTenants.Find(x => x.Id == tenantId)?.Tenant;

            if (tenant == null)
            {
                throw new TenantNotFoundException();
            }

            return Task.FromResult(tenant);
        }

        /// <inheritdoc/>
        public Task<ITenant> UpdateTenantAsync(
            string tenantId,
            string? name = null,
            IEnumerable<KeyValuePair<string, object>>? propertiesToSetOrAdd = null,
            IEnumerable<string>? propertiesToRemove = null)
        {
            StoredTenant? currentStoredTenant = this.allTenants.Find(x => x.Id == tenantId);

            if (currentStoredTenant == null)
            {
                throw new TenantNotFoundException($"Cannot update tenant with Id '{tenantId}' because it has not previously been saved.");
            }

            bool propertiesChanged = propertiesToSetOrAdd != null || propertiesToRemove == null;
            IPropertyBag properties = propertiesChanged
                    ? this.propertyBagFactory.CreateModified(
                        currentStoredTenant.Tenant.Properties,
                        propertiesToSetOrAdd,
                        propertiesToRemove)
                    : currentStoredTenant.Tenant.Properties;
            currentStoredTenant.Tenant = new Tenant(
                tenantId,
                name ?? currentStoredTenant.Tenant.Name,
                properties);

            return Task.FromResult(currentStoredTenant.Tenant);
        }

        /// <summary>
        /// Retrieves a tenant by name. This is intended to assist with assertions during testing. Note that if multiple
        /// tenants with the same name exist, the first matching tenant will be returned.
        /// </summary>
        /// <param name="name">The tenant name to search for.</param>
        /// <returns>The first matching tenant, or null if none was found.</returns>
        public ITenant? GetTenantByName(string name)
        {
            return this.allTenants.Find(x => x.Name == name)?.Tenant;
        }

        /// <summary>
        /// Gets all children for a tenant. Intended to assist with assertions during testing.
        /// </summary>
        /// <param name="parentId">The Id of the tenant to retrieve children for.</param>
        /// <returns>The list of children, or an empty list if there are none.</returns>
        /// <remarks>
        /// This method makes no attempt to validate the supplied parent Id.
        /// </remarks>
        public List<string> GetChildren(string parentId)
        {
            if (!this.tenantsByParent.TryGetValue(parentId, out List<string>? children))
            {
                children = new List<string>();
                this.tenantsByParent.Add(parentId, children);
            }

            return children;
        }

        /// <summary>
        /// Helper class to represent a tenant being held in memory. Tenant data is held in serialized JSON form. The reasons
        /// behind storing tenants in this form are explained in the <c>remarks</c> section of the documentation for the
        /// <see cref="InMemoryTenantStore"/> class.
        /// </summary>
        private class StoredTenant
        {
            private readonly JsonSerializerSettings settings;
            private string tenant = string.Empty;

            public StoredTenant(ITenant tenant, JsonSerializerSettings settings)
            {
                this.settings = settings;
                this.Id = tenant.Id;
                this.Name = tenant.Name;
                this.Tenant = tenant;
            }

            public string Id { get; }

            public string Name { get; }

            public ITenant Tenant
            {
                get => JsonConvert.DeserializeObject<Tenant>(this.tenant, this.settings);
                set => this.tenant = JsonConvert.SerializeObject(value, this.settings);
            }
        }
    }
}