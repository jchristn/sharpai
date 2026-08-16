namespace SharpAI.Security
{
    using System;

    using SharpAI.Helpers;

    /// <summary>
    /// Binds a user to a role at a specific scope. A role may be referenced by GUID or by name (the name
    /// fallback keeps assignments resilient when built-in role records are recreated with new GUIDs).
    /// Tenant-scoped assignments may optionally inherit to child resources.
    /// </summary>
    public class UserRoleAssignment
    {
        #region Public-Members

        /// <summary>
        /// Assignment identifier (prefix "asn_").
        /// </summary>
        public string Guid { get; set; } = IdGenerator.GenerateAssignmentId();

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantGuid { get; set; } = String.Empty;

        /// <summary>
        /// User identifier.
        /// </summary>
        public string UserGuid { get; set; } = String.Empty;

        /// <summary>
        /// Role identifier, or null when the assignment references a role by name.
        /// </summary>
        public string RoleGuid { get; set; } = null;

        /// <summary>
        /// Role name, used as a fallback when <see cref="RoleGuid"/> is null or unresolved.
        /// </summary>
        public string RoleName { get; set; } = null;

        /// <summary>
        /// Resource scope of the grant.
        /// </summary>
        public ResourceScopeEnum ResourceScope { get; set; } = ResourceScopeEnum.Tenant;

        /// <summary>
        /// Target resource identifier when <see cref="ResourceScope"/> is <see cref="ResourceScopeEnum.Resource"/>.
        /// </summary>
        public string ResourceGuid { get; set; } = null;

        /// <summary>
        /// Whether a tenant-scoped grant flows to child resources inside the tenant.
        /// </summary>
        public bool InheritsToChildren { get; set; } = true;

        /// <summary>
        /// Whether the assignment is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Whether the assignment is protected.
        /// </summary>
        public bool IsProtected { get; set; } = false;

        /// <summary>
        /// Creation time in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last update time in UTC.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public UserRoleAssignment()
        {
        }

        #endregion
    }
}
