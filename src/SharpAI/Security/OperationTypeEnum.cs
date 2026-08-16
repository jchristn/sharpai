namespace SharpAI.Security
{
    /// <summary>
    /// The operation a request performs against a resource, used for authorization. <see cref="Write"/> is a
    /// mutating shorthand that expands to <see cref="Create"/>, <see cref="Update"/>, and <see cref="Delete"/>.
    /// <see cref="All"/> is a wildcard that matches any operation.
    /// </summary>
    public enum OperationTypeEnum
    {
        /// <summary>Wildcard — matches any operation.</summary>
        All,

        /// <summary>Create a new resource.</summary>
        Create,

        /// <summary>Retrieve or view a resource.</summary>
        Read,

        /// <summary>Mutating shorthand that expands to Create, Update, and Delete.</summary>
        Write,

        /// <summary>Modify an existing resource.</summary>
        Update,

        /// <summary>Remove a resource.</summary>
        Delete,

        /// <summary>Run or trigger an operation.</summary>
        Execute,

        /// <summary>Manage security, tenancy, configuration, or other privileged control-plane surfaces.</summary>
        Admin
    }
}
