namespace SharpAI.Security
{
    using System;

    using SharpAI.Helpers;

    /// <summary>
    /// Associates a role with a permission (the many-to-many role↔permission relationship). Built-in maps
    /// carry a null <see cref="TenantGuid"/>.
    /// </summary>
    public class RolePermissionMap
    {
        #region Public-Members

        /// <summary>
        /// Mapping identifier (prefix "asn_").
        /// </summary>
        public string Guid { get; set; } = IdGenerator.GenerateAssignmentId();

        /// <summary>
        /// Owning tenant identifier, or null for a built-in mapping.
        /// </summary>
        public string TenantGuid { get; set; } = null;

        /// <summary>
        /// Role identifier.
        /// </summary>
        public string RoleGuid { get; set; } = String.Empty;

        /// <summary>
        /// Permission identifier.
        /// </summary>
        public string PermissionGuid { get; set; } = String.Empty;

        /// <summary>
        /// Whether the mapping is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Whether the mapping is protected.
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
        public RolePermissionMap()
        {
        }

        #endregion
    }
}
