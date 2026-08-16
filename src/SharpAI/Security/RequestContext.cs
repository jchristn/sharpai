namespace SharpAI.Security
{
    /// <summary>
    /// Authenticated request context established by the authentication layer and attached to the HTTP
    /// context metadata. Route handlers read this to make authorization decisions. When authentication is
    /// disabled, this represents the implicit system principal (fully authorized).
    /// </summary>
    public class RequestContext
    {
        #region Public-Members

        /// <summary>
        /// Whether the request is authenticated.
        /// </summary>
        public bool IsAuthenticated { get; set; } = false;

        /// <summary>
        /// Whether the request targets an anonymous (unauthenticated) endpoint that is always allowed.
        /// </summary>
        public bool IsAnonymousEndpoint { get; set; } = false;

        /// <summary>
        /// Whether the authentication layer should challenge the request (respond 401). True when auth is
        /// enabled, the endpoint is not anonymous, and no valid principal was established.
        /// </summary>
        public bool ShouldChallenge { get; set; } = false;

        /// <summary>
        /// Principal type.
        /// </summary>
        public PrincipalTypeEnum PrincipalType { get; set; } = PrincipalTypeEnum.None;

        /// <summary>
        /// Principal identifier, if any.
        /// </summary>
        public string PrincipalGuid { get; set; } = null;

        /// <summary>
        /// Session identifier, when authenticated via a bearer session token.
        /// </summary>
        public string SessionGuid { get; set; } = null;

        /// <summary>
        /// Tenant identifier, if any.
        /// </summary>
        public string TenantGuid { get; set; } = null;

        /// <summary>
        /// For a credential principal, the owning user's identifier (used for the RBAC owner-ceiling).
        /// </summary>
        public string OwnerUserGuid { get; set; } = null;

        /// <summary>
        /// Whether the principal is a global administrator.
        /// </summary>
        public bool IsAdmin { get; set; } = false;

        /// <summary>
        /// Whether the principal is a tenant administrator.
        /// </summary>
        public bool IsTenantAdmin { get; set; } = false;

        /// <summary>
        /// The authentication scheme used.
        /// </summary>
        public AuthSchemeEnum AuthScheme { get; set; } = AuthSchemeEnum.None;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestContext()
        {
        }

        #endregion
    }
}
