namespace SharpAI.Security
{
    using System;
    using System.Collections.Generic;

    using SharpAI.Helpers;

    /// <summary>
    /// A granular RBAC permission: it permits or denies a set of operations on a set of resource types. Both
    /// lists accept the wildcard value <c>"All"</c>. Built-in permissions carry a null <see cref="TenantGuid"/>.
    /// </summary>
    public class Permission
    {
        #region Public-Members

        /// <summary>
        /// Permission identifier (prefix "perm_").
        /// </summary>
        public string Guid { get; set; } = IdGenerator.GeneratePermissionId();

        /// <summary>
        /// Owning tenant identifier, or null for a globally-visible built-in permission.
        /// </summary>
        public string TenantGuid { get; set; } = null;

        /// <summary>
        /// Human-readable name (for example "Manage Users").
        /// </summary>
        public string Name { get; set; } = String.Empty;

        /// <summary>
        /// Resource types this permission applies to. Use <see cref="ResourceTypes.All"/> for a wildcard.
        /// Never null.
        /// </summary>
        public List<string> ResourceTypes
        {
            get
            {
                return _ResourceTypes;
            }
            set
            {
                _ResourceTypes = value ?? new List<string>();
            }
        }

        /// <summary>
        /// Operation types this permission covers, as <see cref="OperationTypeEnum"/> names. Use
        /// <c>"All"</c> for a wildcard. Never null.
        /// </summary>
        public List<string> OperationTypes
        {
            get
            {
                return _OperationTypes;
            }
            set
            {
                _OperationTypes = value ?? new List<string>();
            }
        }

        /// <summary>
        /// Whether this permission permits or explicitly denies.
        /// </summary>
        public PermissionEffectEnum Effect { get; set; } = PermissionEffectEnum.Permit;

        /// <summary>
        /// Whether the permission is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Whether the permission is protected from deletion/mutation through tenant APIs.
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

        #region Private-Members

        private List<string> _ResourceTypes = new List<string>();
        private List<string> _OperationTypes = new List<string>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public Permission()
        {
        }

        #endregion
    }
}
