namespace SharpAI.Security
{
    /// <summary>
    /// The authentication scheme that established a request's principal.
    /// </summary>
    public enum AuthSchemeEnum
    {
        /// <summary>
        /// No scheme (anonymous, or authentication disabled).
        /// </summary>
        None,

        /// <summary>
        /// Administrator API key (x-api-key).
        /// </summary>
        AdminApiKey,

        /// <summary>
        /// Bearer session token (Authorization: Bearer / x-token).
        /// </summary>
        BearerToken,

        /// <summary>
        /// Header-based user login (x-email / x-password).
        /// </summary>
        PasswordHeaders,

        /// <summary>
        /// Access-key / secret-key direct headers.
        /// </summary>
        AccessKeySecret,

        /// <summary>
        /// AWS-style signed request.
        /// </summary>
        AccessKeySignature
    }
}
