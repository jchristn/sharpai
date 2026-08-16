namespace SharpAI.Security
{
    using System;

    /// <summary>
    /// A single expanded, scoped grant that applies to a principal: the atomic unit the authorization
    /// evaluator matches against a request. Returned by effective-permission inspection.
    /// </summary>
    public class EffectivePermission
    {
        #region Public-Members

        /// <summary>
        /// Resource type this grant applies to (or <see cref="ResourceTypes.All"/>).
        /// </summary>
        public string ResourceType { get; set; } = ResourceTypes.All;

        /// <summary>
        /// Operation this grant covers.
        /// </summary>
        public OperationTypeEnum Operation { get; set; } = OperationTypeEnum.All;

        /// <summary>
        /// Whether this grant permits or denies.
        /// </summary>
        public PermissionEffectEnum Effect { get; set; } = PermissionEffectEnum.Permit;

        /// <summary>
        /// Scope of the grant.
        /// </summary>
        public ResourceScopeEnum ResourceScope { get; set; } = ResourceScopeEnum.Tenant;

        /// <summary>
        /// Target resource identifier when resource-scoped, otherwise null.
        /// </summary>
        public string ResourceGuid { get; set; } = null;

        /// <summary>
        /// Whether a tenant-scoped grant flows to child resources.
        /// </summary>
        public bool InheritsToChildren { get; set; } = true;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public EffectivePermission()
        {
        }

        #endregion
    }
}
