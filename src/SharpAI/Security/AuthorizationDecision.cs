namespace SharpAI.Security
{
    using System;

    /// <summary>
    /// The outcome of an authorization decision, suitable for both enforcement and the denial response body
    /// (reason + required permission) described by the authentication requirements.
    /// </summary>
    public class AuthorizationDecision
    {
        #region Public-Members

        /// <summary>
        /// The decision result.
        /// </summary>
        public AuthorizationResultEnum Result { get; set; } = AuthorizationResultEnum.DeniedImplicit;

        /// <summary>
        /// Whether access is permitted.
        /// </summary>
        public bool IsPermitted
        {
            get { return Result == AuthorizationResultEnum.Permitted; }
        }

        /// <summary>
        /// Short human-readable explanation of the decision.
        /// </summary>
        public string Reason { get; set; } = String.Empty;

        /// <summary>
        /// The resource type that was evaluated.
        /// </summary>
        public string ResourceType { get; set; } = null;

        /// <summary>
        /// The operation that was evaluated.
        /// </summary>
        public OperationTypeEnum Operation { get; set; } = OperationTypeEnum.Read;

        /// <summary>
        /// When authorization was skipped via a bypass rule, the reason for the bypass; otherwise null.
        /// </summary>
        public string BypassReason { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AuthorizationDecision()
        {
        }

        /// <summary>
        /// Create a permitted decision.
        /// </summary>
        /// <param name="reason">Explanation.</param>
        /// <param name="bypassReason">Bypass reason, or null.</param>
        /// <returns>Decision.</returns>
        public static AuthorizationDecision Permit(string reason, string bypassReason = null)
        {
            return new AuthorizationDecision
            {
                Result = AuthorizationResultEnum.Permitted,
                Reason = reason,
                BypassReason = bypassReason
            };
        }

        /// <summary>
        /// Create a denied decision.
        /// </summary>
        /// <param name="result">Explicit or implicit denial.</param>
        /// <param name="reason">Explanation.</param>
        /// <returns>Decision.</returns>
        public static AuthorizationDecision Deny(AuthorizationResultEnum result, string reason)
        {
            return new AuthorizationDecision
            {
                Result = result,
                Reason = reason
            };
        }

        #endregion
    }
}
