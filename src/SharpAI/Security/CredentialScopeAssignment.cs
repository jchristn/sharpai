namespace SharpAI.Security
{
    using System;
    using System.Collections.Generic;

    using SharpAI.Helpers;

    /// <summary>
    /// Binds a credential to a role (or a role-less direct grant) at a specific scope. Mirrors
    /// <see cref="UserRoleAssignment"/> but is keyed on a credential, so a credential can be scoped below its
    /// owning user (least privilege). Direct <see cref="Permissions"/>/<see cref="ResourceTypes"/> enable
    /// role-less grants.
    /// </summary>
    public class CredentialScopeAssignment
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
        /// Credential identifier.
        /// </summary>
        public string CredentialGuid { get; set; } = String.Empty;

        /// <summary>
        /// Role identifier, or null for a role-less direct grant.
        /// </summary>
        public string RoleGuid { get; set; } = null;

        /// <summary>
        /// Role name fallback, or null.
        /// </summary>
        public string RoleName { get; set; } = null;

        /// <summary>
        /// Resource scope of the grant.
        /// </summary>
        public ResourceScopeEnum ResourceScope { get; set; } = ResourceScopeEnum.Tenant;

        /// <summary>
        /// Target resource identifier when resource-scoped.
        /// </summary>
        public string ResourceGuid { get; set; } = null;

        /// <summary>
        /// Whether a tenant-scoped grant flows to child resources inside the tenant.
        /// </summary>
        public bool InheritsToChildren { get; set; } = true;

        /// <summary>
        /// Direct operation grants (<see cref="OperationTypeEnum"/> names) for a role-less assignment. Never null.
        /// </summary>
        public List<string> Permissions
        {
            get
            {
                return _Permissions;
            }
            set
            {
                _Permissions = value ?? new List<string>();
            }
        }

        /// <summary>
        /// Direct resource-type grants for a role-less assignment. Never null.
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

        #region Private-Members

        private List<string> _Permissions = new List<string>();
        private List<string> _ResourceTypes = new List<string>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public CredentialScopeAssignment()
        {
        }

        #endregion
    }
}
