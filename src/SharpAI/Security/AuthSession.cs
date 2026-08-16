namespace SharpAI.Security
{
    using System;

    using SharpAI.Helpers;

    /// <summary>
    /// A server-side, revocable authentication session referenced by a short-lived bearer token. Each
    /// session resolves to exactly one principal within one tenant.
    /// </summary>
    public class AuthSession
    {
        #region Public-Members

        /// <summary>
        /// Session identifier (prefix "ses_"). This is the value carried inside the encrypted bearer token.
        /// </summary>
        public string Guid { get; set; } = IdGenerator.GenerateSessionId();

        /// <summary>
        /// Principal (user) identifier.
        /// </summary>
        public string UserGuid { get; set; } = String.Empty;

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantGuid { get; set; } = String.Empty;

        /// <summary>
        /// Principal type.
        /// </summary>
        public PrincipalTypeEnum PrincipalType { get; set; } = PrincipalTypeEnum.User;

        /// <summary>
        /// Whether the session is active (not revoked).
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Whether the session is protected.
        /// </summary>
        public bool IsProtected { get; set; } = false;

        /// <summary>
        /// Creation time in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Expiry time in UTC.
        /// </summary>
        public DateTime ExpiresUtc { get; set; } = DateTime.UtcNow.AddMinutes(60);

        /// <summary>
        /// Last used time in UTC, if ever.
        /// </summary>
        public DateTime? LastUsedUtc { get; set; } = null;

        /// <summary>
        /// Revocation time in UTC, if revoked.
        /// </summary>
        public DateTime? RevokedUtc { get; set; } = null;

        /// <summary>
        /// Revocation reason, if revoked.
        /// </summary>
        public string RevocationReason { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AuthSession()
        {
        }

        #endregion
    }
}
