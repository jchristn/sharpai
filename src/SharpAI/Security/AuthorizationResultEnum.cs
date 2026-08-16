namespace SharpAI.Security
{
    /// <summary>
    /// The outcome of an authorization decision.
    /// </summary>
    public enum AuthorizationResultEnum
    {
        /// <summary>Access is permitted (a matching permit with no matching deny, or an authorized bypass).</summary>
        Permitted,

        /// <summary>Access is denied because a matching deny permission was found.</summary>
        DeniedExplicit,

        /// <summary>Access is denied because no permission matched the request.</summary>
        DeniedImplicit
    }
}
