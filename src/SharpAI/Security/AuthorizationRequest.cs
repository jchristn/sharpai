namespace SharpAI.Security
{
    using System;

    /// <summary>
    /// The immutable input to an authorization decision: the resolved principal plus the resource and
    /// operation being attempted. For credential principals, <see cref="OwnerUserGuid"/> enables the
    /// owner-ceiling intersection.
    /// </summary>
    public class AuthorizationRequest
    {
        #region Public-Members

        /// <summary>
        /// Tenant identifier the request is scoped to.
        /// </summary>
        public string TenantGuid { get; set; } = String.Empty;

        /// <summary>
        /// Principal type.
        /// </summary>
        public PrincipalTypeEnum PrincipalType { get; set; } = PrincipalTypeEnum.None;

        /// <summary>
        /// Principal identifier (user or credential GUID).
        /// </summary>
        public string PrincipalGuid { get; set; } = String.Empty;

        /// <summary>
        /// Whether the principal has the global-administrator bypass.
        /// </summary>
        public bool IsAdmin { get; set; } = false;

        /// <summary>
        /// Whether the principal has the tenant-administrator bypass (within its own tenant).
        /// </summary>
        public bool IsTenantAdmin { get; set; } = false;

        /// <summary>
        /// The resource type the operation targets (see <see cref="ResourceTypes"/>).
        /// </summary>
        public string ResourceType { get; set; } = ResourceTypes.All;

        /// <summary>
        /// The operation being attempted.
        /// </summary>
        public OperationTypeEnum Operation { get; set; } = OperationTypeEnum.Read;

        /// <summary>
        /// The specific resource identifier being targeted, or null for a tenant-level operation.
        /// </summary>
        public string ResourceGuid { get; set; } = null;

        /// <summary>
        /// For credential principals, the owning user's identifier (used for the owner-ceiling intersection).
        /// </summary>
        public string OwnerUserGuid { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AuthorizationRequest()
        {
        }

        #endregion
    }
}
