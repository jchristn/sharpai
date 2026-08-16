namespace SharpAI.Security
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Pure authentication decision logic, independent of any HTTP framework so it can be unit-tested. It
    /// establishes a <see cref="RequestContext"/> from the request path and credentials. When auth is
    /// disabled the result is the implicit system principal (open server, Ollama parity). When enabled,
    /// anonymous endpoints are always allowed, a valid admin API key yields an administrator context, and
    /// anything else is flagged to be challenged.
    /// </summary>
    public static class AuthEvaluator
    {
        #region Public-Members

        /// <summary>
        /// Path prefixes that are always served anonymously, even when authentication is enabled:
        /// the homepage, health/readiness probes, the favicon, the OpenAPI document / Swagger UI, and the
        /// token endpoint (login must be reachable without a prior credential; the token handlers enforce
        /// their own session checks for read/revoke).
        /// </summary>
        public static readonly string[] AnonymousPathPrefixes = new string[]
        {
            "/health", "/ready", "/favicon.ico", "/openapi.json", "/swagger", "/v1.0/token"
        };

        #endregion

        #region Public-Methods

        /// <summary>
        /// Determine whether a request path is served anonymously regardless of authentication mode.
        /// </summary>
        /// <param name="path">Request path (without query string).</param>
        /// <returns>True if the path is anonymous.</returns>
        public static bool IsAnonymousPath(string path)
        {
            if (String.IsNullOrEmpty(path)) return true;
            if (path == "/") return true;

            foreach (string prefix in AnonymousPathPrefixes)
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        /// <summary>
        /// Evaluate the authentication decision for a request.
        /// </summary>
        /// <param name="authEnabled">Whether authentication is enabled.</param>
        /// <param name="path">Request path (without query string).</param>
        /// <param name="apiKey">Admin API key supplied on the request (x-api-key), or null.</param>
        /// <param name="validAdminApiKeys">Configured valid admin API keys, or null/empty.</param>
        /// <returns>The established request context. Never null.</returns>
        public static RequestContext Evaluate(
            bool authEnabled,
            string path,
            string apiKey,
            IReadOnlyList<string> validAdminApiKeys)
        {
            if (!authEnabled)
            {
                return new RequestContext
                {
                    IsAuthenticated = true,
                    PrincipalType = PrincipalTypeEnum.System,
                    IsAdmin = true,
                    AuthScheme = AuthSchemeEnum.None
                };
            }

            if (IsAnonymousPath(path))
            {
                return new RequestContext
                {
                    IsAuthenticated = false,
                    IsAnonymousEndpoint = true,
                    ShouldChallenge = false,
                    PrincipalType = PrincipalTypeEnum.None
                };
            }

            if (!String.IsNullOrEmpty(apiKey) && MatchesAnyKey(apiKey, validAdminApiKeys))
            {
                return new RequestContext
                {
                    IsAuthenticated = true,
                    PrincipalType = PrincipalTypeEnum.Administrator,
                    IsAdmin = true,
                    AuthScheme = AuthSchemeEnum.AdminApiKey
                };
            }

            return new RequestContext
            {
                IsAuthenticated = false,
                ShouldChallenge = true,
                PrincipalType = PrincipalTypeEnum.None
            };
        }

        /// <summary>
        /// Compare two secret strings in constant time to avoid leaking length/content via timing.
        /// </summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <returns>True if equal.</returns>
        public static bool SecureEquals(string a, string b)
        {
            if (a == null || b == null) return false;

            byte[] left = Encoding.UTF8.GetBytes(a);
            byte[] right = Encoding.UTF8.GetBytes(b);
            if (left.Length != right.Length) return false;

            return CryptographicOperations.FixedTimeEquals(left, right);
        }

        #endregion

        #region Private-Methods

        private static bool MatchesAnyKey(string apiKey, IReadOnlyList<string> validAdminApiKeys)
        {
            if (validAdminApiKeys == null) return false;

            bool matched = false;
            foreach (string candidate in validAdminApiKeys)
            {
                if (String.IsNullOrEmpty(candidate)) continue;
                // Evaluate every key (no short-circuit) to keep the comparison time independent of position.
                if (SecureEquals(apiKey, candidate)) matched = true;
            }

            return matched;
        }

        #endregion
    }
}
