namespace SharpAI.Server.Classes.Runtime
{
    using System;
    using System.Threading.Tasks;

    using SharpAI.Database;
    using SharpAI.Security;
    using SharpAI.Server.Classes.Settings;

    using SyslogLogging;

    using WatsonWebserver.Core;

    /// <summary>
    /// Establishes the authenticated <see cref="RequestContext"/> for each request and attaches it to the
    /// HTTP context metadata. Registered on the Watson <c>AuthenticateRequest</c> hook. When authentication
    /// is disabled this is a no-op that installs the system principal; when enabled it resolves the request
    /// against the account store (admin API key, bearer session token, or access-key / secret-key) and
    /// challenges (401) unauthenticated requests to non-anonymous endpoints, recording each denial to the
    /// security audit log.
    /// </summary>
    public class AuthenticationService
    {
        #region Private-Members

        private readonly string _Header = "[Auth] ";
        private readonly AuthSettings _Settings;
        private readonly AuthenticationEngine _Engine;
        private readonly DatabaseDriverBase _Database;
        private readonly LoggingModule _Logging;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Authentication settings.</param>
        /// <param name="engine">Authentication engine.</param>
        /// <param name="database">Database driver (for the audit log).</param>
        /// <param name="logging">Logging module.</param>
        public AuthenticationService(
            AuthSettings settings,
            AuthenticationEngine engine,
            DatabaseDriverBase database,
            LoggingModule logging)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Authenticate the request, attach a <see cref="RequestContext"/> to <c>ctx.Metadata</c>, and
        /// challenge (401) when required. Sending a 401 here stops routing.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task AuthenticateRequestAsync(HttpContextBase ctx)
        {
            string path = ExtractPath(ctx);
            string apiKey = GetHeader(ctx, "x-api-key");
            string bearer = ExtractBearer(ctx);
            string accessKey = GetHeader(ctx, "x-access-key");
            string secretKey = GetHeader(ctx, "x-secret-key");

            RequestContext context = _Engine.Authenticate(
                _Settings.Enabled, path, apiKey, _Settings.AdminApiKeys, bearer, accessKey, secretKey);
            ctx.Metadata = context;

            if (context.ShouldChallenge)
            {
                RecordDenial(ctx, path, "Authentication required.");

                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send("{\"error\":{\"type\":\"unauthorized\",\"message\":\"Authentication required.\"}}").ConfigureAwait(false);
            }
        }

        #endregion

        #region Private-Methods

        private void RecordDenial(HttpContextBase ctx, string path, string reason)
        {
            try
            {
                AuditLogEntry entry = new AuditLogEntry
                {
                    EventType = "AuthenticationFailure",
                    PrincipalType = PrincipalTypeEnum.None,
                    Method = ctx.Request.Method.ToString(),
                    Path = path,
                    IpAddress = ctx.Request.Source != null ? ctx.Request.Source.IpAddress : null,
                    AuthResult = false,
                    AuthzResult = false,
                    DenialReason = reason,
                    StatusCode = 401
                };

                _Database.Audit.Create(entry);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "unable to record authentication denial: " + e.Message);
            }
        }

        private static string ExtractPath(HttpContextBase ctx)
        {
            string raw = ctx.Request.Url != null ? ctx.Request.Url.RawWithQuery : "/";
            if (String.IsNullOrEmpty(raw)) return "/";
            int queryIndex = raw.IndexOf('?');
            return queryIndex >= 0 ? raw.Substring(0, queryIndex) : raw;
        }

        private static string ExtractBearer(HttpContextBase ctx)
        {
            string authorization = GetHeader(ctx, "authorization");
            if (!String.IsNullOrEmpty(authorization) &&
                authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authorization.Substring(7).Trim();
            }

            return GetHeader(ctx, "x-token");
        }

        private static string GetHeader(HttpContextBase ctx, string name)
        {
            System.Collections.Specialized.NameValueCollection headers = ctx.Request.Headers;
            if (headers == null) return null;

            foreach (string key in headers.AllKeys)
            {
                if (key != null && key.Equals(name, StringComparison.OrdinalIgnoreCase)) return headers[key];
            }

            return null;
        }

        #endregion
    }
}
