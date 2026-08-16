namespace SharpAI.Helpers
{
    using System;

    /// <summary>
    /// Generates prefixed application identifiers. Prefixes make identifiers self-describing in logs and
    /// APIs (for example <c>req_</c> for a request-history entry, <c>mdl_</c> for a model).
    /// </summary>
    public static class IdGenerator
    {
        #region Public-Members

        /// <summary>
        /// Prefix for request-history entry identifiers.
        /// </summary>
        public const string RequestHistoryPrefix = "req_";

        /// <summary>
        /// Prefix for model identifiers.
        /// </summary>
        public const string ModelPrefix = "mdl_";

        /// <summary>
        /// Prefix for tenant identifiers.
        /// </summary>
        public const string TenantPrefix = "ten_";

        /// <summary>
        /// Prefix for user identifiers.
        /// </summary>
        public const string UserPrefix = "usr_";

        /// <summary>
        /// Prefix for credential identifiers.
        /// </summary>
        public const string CredentialPrefix = "crd_";

        /// <summary>
        /// Prefix for session identifiers.
        /// </summary>
        public const string SessionPrefix = "ses_";

        /// <summary>
        /// Prefix for audit log entry identifiers.
        /// </summary>
        public const string AuditPrefix = "aud_";

        /// <summary>
        /// Prefix for role identifiers.
        /// </summary>
        public const string RolePrefix = "role_";

        /// <summary>
        /// Prefix for permission identifiers.
        /// </summary>
        public const string PermissionPrefix = "perm_";

        /// <summary>
        /// Prefix for mapping/assignment identifiers.
        /// </summary>
        public const string AssignmentPrefix = "asn_";

        #endregion

        #region Public-Methods

        /// <summary>
        /// Generate a tenant identifier.
        /// </summary>
        /// <returns>Identifier.</returns>
        public static string GenerateTenantId()
        {
            return TenantPrefix + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Generate a user identifier.
        /// </summary>
        /// <returns>Identifier.</returns>
        public static string GenerateUserId()
        {
            return UserPrefix + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Generate a credential identifier.
        /// </summary>
        /// <returns>Identifier.</returns>
        public static string GenerateCredentialId()
        {
            return CredentialPrefix + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Generate a session identifier.
        /// </summary>
        /// <returns>Identifier.</returns>
        public static string GenerateSessionId()
        {
            return SessionPrefix + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Generate an audit log entry identifier.
        /// </summary>
        /// <returns>Identifier.</returns>
        public static string GenerateAuditId()
        {
            return AuditPrefix + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Generate a request-history entry identifier.
        /// </summary>
        /// <returns>Identifier.</returns>
        public static string GenerateRequestHistoryId()
        {
            return RequestHistoryPrefix + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Generate a model identifier.
        /// </summary>
        /// <returns>Identifier.</returns>
        public static string GenerateModelId()
        {
            return ModelPrefix + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Generate a role identifier.
        /// </summary>
        /// <returns>Identifier.</returns>
        public static string GenerateRoleId()
        {
            return RolePrefix + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Generate a permission identifier.
        /// </summary>
        /// <returns>Identifier.</returns>
        public static string GeneratePermissionId()
        {
            return PermissionPrefix + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Generate a mapping/assignment identifier.
        /// </summary>
        /// <returns>Identifier.</returns>
        public static string GenerateAssignmentId()
        {
            return AssignmentPrefix + Guid.NewGuid().ToString("N");
        }

        #endregion
    }
}
