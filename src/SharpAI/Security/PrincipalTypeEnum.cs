namespace SharpAI.Security
{
    /// <summary>
    /// The type of principal that authenticated a request.
    /// </summary>
    public enum PrincipalTypeEnum
    {
        /// <summary>
        /// No authenticated principal (anonymous).
        /// </summary>
        None,

        /// <summary>
        /// The implicit system principal used when authentication is disabled (open server, Ollama parity).
        /// </summary>
        System,

        /// <summary>
        /// A platform administrator (admin API key).
        /// </summary>
        Administrator,

        /// <summary>
        /// An interactive user.
        /// </summary>
        User,

        /// <summary>
        /// A non-interactive credential (access key / signed request).
        /// </summary>
        Credential
    }
}
