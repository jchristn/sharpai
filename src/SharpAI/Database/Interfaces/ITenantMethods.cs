namespace SharpAI.Database.Interfaces
{
    using SharpAI.Models;
    using SharpAI.Security;

    /// <summary>
    /// Data-access methods for tenants. Handwritten, provider-aware SQL.
    /// </summary>
    public interface ITenantMethods
    {
        /// <summary>Create a tenant.</summary>
        /// <param name="tenant">Tenant.</param>
        /// <returns>The created tenant.</returns>
        Tenant Create(Tenant tenant);

        /// <summary>Read a tenant by identifier.</summary>
        /// <param name="guid">Tenant identifier.</param>
        /// <returns>The tenant, or null.</returns>
        Tenant Read(string guid);

        /// <summary>Read a tenant by name.</summary>
        /// <param name="name">Tenant name.</param>
        /// <returns>The tenant, or null.</returns>
        Tenant GetByName(string name);

        /// <summary>Update a tenant.</summary>
        /// <param name="tenant">Tenant.</param>
        /// <returns>The updated tenant.</returns>
        Tenant Update(Tenant tenant);

        /// <summary>Delete a tenant by identifier.</summary>
        /// <param name="guid">Tenant identifier.</param>
        void Delete(string guid);

        /// <summary>Page through tenants.</summary>
        /// <param name="query">Enumeration query.</param>
        /// <returns>Enumeration result.</returns>
        EnumerationResult<Tenant> Enumerate(EnumerationQuery query);
    }
}
