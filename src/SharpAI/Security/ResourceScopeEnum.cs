namespace SharpAI.Security
{
    /// <summary>
    /// The breadth of a role assignment or permission grant.
    /// </summary>
    public enum ResourceScopeEnum
    {
        /// <summary>
        /// The grant applies to the entire tenant. It covers every resource of the listed types inside the
        /// tenant when the assignment inherits to children; otherwise only tenant-level operations.
        /// </summary>
        Tenant,

        /// <summary>
        /// The grant applies only to one specific resource, identified by its GUID.
        /// </summary>
        Resource
    }
}
