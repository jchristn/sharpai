namespace SharpAI.Security
{
    using System;

    using SharpAI.Helpers;

    /// <summary>
    /// A named container of permissions within a tenant (RBAC). Built-in roles are seeded with a null
    /// <see cref="TenantGuid"/> so they are globally visible but not editable through tenant REST APIs;
    /// tenant-defined custom roles carry their tenant's identifier.
    /// </summary>
    public class UserRole
    {
        #region Public-Members

        /// <summary>
        /// Role identifier (prefix "role_").
        /// </summary>
        public string Guid { get; set; } = IdGenerator.GenerateRoleId();

        /// <summary>
        /// Owning tenant identifier, or null for a globally-visible built-in role.
        /// </summary>
        public string TenantGuid { get; set; } = null;

        /// <summary>
        /// Human-readable role name (for example "TenantAdmin", "Viewer").
        /// </summary>
        public string Name { get; set; } = String.Empty;

        /// <summary>
        /// Whether the role is seeded and maintained by the platform.
        /// </summary>
        public bool IsBuiltIn { get; set; } = false;

        /// <summary>
        /// Whether the role is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Whether the role is protected from deletion/mutation through tenant APIs.
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
        public UserRole()
        {
        }

        #endregion
    }
}
