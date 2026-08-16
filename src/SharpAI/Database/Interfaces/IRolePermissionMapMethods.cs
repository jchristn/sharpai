namespace SharpAI.Database.Interfaces
{
    using System.Collections.Generic;

    using SharpAI.Security;

    /// <summary>
    /// Data-access methods for role↔permission mappings. Handwritten, provider-aware SQL.
    /// </summary>
    public interface IRolePermissionMapMethods
    {
        /// <summary>Create a mapping.</summary>
        /// <param name="map">Mapping.</param>
        /// <returns>The created mapping.</returns>
        RolePermissionMap Create(RolePermissionMap map);

        /// <summary>Get the mappings for a role.</summary>
        /// <param name="roleGuid">Role identifier.</param>
        /// <returns>Mappings; never null.</returns>
        List<RolePermissionMap> GetByRole(string roleGuid);

        /// <summary>Delete a mapping by identifier.</summary>
        /// <param name="guid">Mapping identifier.</param>
        void Delete(string guid);
    }
}
