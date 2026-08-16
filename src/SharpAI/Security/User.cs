namespace SharpAI.Security
{
    using System;

    using SharpAI.Helpers;

    /// <summary>
    /// A tenant user. Passwords are stored only as a SHA-256 hex digest; the plaintext is never persisted.
    /// </summary>
    public class User
    {
        #region Public-Members

        /// <summary>
        /// User identifier (prefix "usr_").
        /// </summary>
        public string Guid { get; set; } = IdGenerator.GenerateUserId();

        /// <summary>
        /// Owning tenant identifier.
        /// </summary>
        public string TenantGuid { get; set; } = String.Empty;

        /// <summary>
        /// First name.
        /// </summary>
        public string FirstName { get; set; } = String.Empty;

        /// <summary>
        /// Last name.
        /// </summary>
        public string LastName { get; set; } = String.Empty;

        /// <summary>
        /// Email address (unique within a tenant).
        /// </summary>
        public string Email { get; set; } = String.Empty;

        /// <summary>
        /// SHA-256 hex digest of the user's password. Never the plaintext.
        /// </summary>
        public string PasswordSha256 { get; set; } = String.Empty;

        /// <summary>
        /// Whether the user is a global administrator.
        /// </summary>
        public bool IsAdmin { get; set; } = false;

        /// <summary>
        /// Whether the user is a tenant administrator.
        /// </summary>
        public bool IsTenantAdmin { get; set; } = false;

        /// <summary>
        /// Whether the user is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Whether the user is protected from deletion/modification via tenant APIs.
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
        public User()
        {
        }

        #endregion
    }
}
