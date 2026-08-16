namespace SharpAI.Database.Interfaces
{
    using SharpAI.Models;
    using SharpAI.Security;

    /// <summary>
    /// Data-access methods for RBAC roles. Handwritten, provider-aware SQL. Built-in roles carry a null
    /// tenant identifier and are globally visible.
    /// </summary>
    public interface IRoleMethods
    {
        /// <summary>Create a role.</summary>
        /// <param name="role">Role.</param>
        /// <returns>The created role.</returns>
        UserRole Create(UserRole role);

        /// <summary>Read a role by identifier.</summary>
        /// <param name="guid">Role identifier.</param>
        /// <returns>The role, or null.</returns>
        UserRole Read(string guid);

        /// <summary>Read a built-in role by name (tenant identifier is null).</summary>
        /// <param name="name">Role name.</param>
        /// <returns>The role, or null.</returns>
        UserRole GetBuiltInByName(string name);

        /// <summary>Read a tenant-scoped role by name.</summary>
        /// <param name="tenantGuid">Tenant identifier.</param>
        /// <param name="name">Role name.</param>
        /// <returns>The role, or null.</returns>
        UserRole GetByName(string tenantGuid, string name);

        /// <summary>Update a role.</summary>
        /// <param name="role">Role.</param>
        /// <returns>The updated role.</returns>
        UserRole Update(UserRole role);

        /// <summary>Delete a role by identifier.</summary>
        /// <param name="guid">Role identifier.</param>
        void Delete(string guid);

        /// <summary>Page through roles visible to a tenant (its own plus built-ins).</summary>
        /// <param name="tenantGuid">Tenant identifier.</param>
        /// <param name="query">Enumeration query.</param>
        /// <returns>Enumeration result.</returns>
        EnumerationResult<UserRole> Enumerate(string tenantGuid, EnumerationQuery query);
    }
}
