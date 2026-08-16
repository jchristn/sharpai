namespace SharpAI.Security
{
    using System;

    using SharpAI.Helpers;

    /// <summary>
    /// A durable security audit event. Authorization denials and privileged control-plane operations are
    /// recorded here.
    /// </summary>
    public class AuditLogEntry
    {
        #region Public-Members

        /// <summary>
        /// Audit entry identifier (prefix "aud_").
        /// </summary>
        public string Guid { get; set; } = IdGenerator.GenerateAuditId();

        /// <summary>
        /// Tenant identifier, if resolved.
        /// </summary>
        public string TenantGuid { get; set; } = null;

        /// <summary>
        /// Event type (for example "AuthenticationFailure", "AuthorizationDenied", "SessionCreated").
        /// </summary>
        public string EventType { get; set; } = String.Empty;

        /// <summary>
        /// Principal type.
        /// </summary>
        public PrincipalTypeEnum PrincipalType { get; set; } = PrincipalTypeEnum.None;

        /// <summary>
        /// Principal identifier, if resolved.
        /// </summary>
        public string PrincipalGuid { get; set; } = null;

        /// <summary>
        /// HTTP method.
        /// </summary>
        public string Method { get; set; } = null;

        /// <summary>
        /// Request path.
        /// </summary>
        public string Path { get; set; } = null;

        /// <summary>
        /// Client IP address.
        /// </summary>
        public string IpAddress { get; set; } = null;

        /// <summary>
        /// Whether authentication succeeded.
        /// </summary>
        public bool AuthResult { get; set; } = false;

        /// <summary>
        /// Whether authorization succeeded.
        /// </summary>
        public bool AuthzResult { get; set; } = false;

        /// <summary>
        /// Reason for a denial, if any.
        /// </summary>
        public string DenialReason { get; set; } = null;

        /// <summary>
        /// Response status code.
        /// </summary>
        public int StatusCode { get; set; } = 0;

        /// <summary>
        /// Event time in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AuditLogEntry()
        {
        }

        #endregion
    }
}
