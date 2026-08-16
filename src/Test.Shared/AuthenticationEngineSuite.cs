namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using SharpAI.Database;
    using SharpAI.Database.Sqlite;
    using SharpAI.Security;

    using SyslogLogging;

    using Touchstone.Core;

    /// <summary>
    /// Contract suite for <see cref="AuthenticationEngine"/> — the framework-free heart of authentication.
    /// It exercises interactive login (email + password) that mints a revocable session token, bearer-token
    /// resolution, access-key / secret-key resolution, revocation, expiry, and the full scheme-ordering of
    /// <see cref="AuthenticationEngine.Authenticate"/>. It runs against embedded SQLite so it needs no
    /// external server, and seeds its own tenant/user/credential fixtures.
    /// </summary>
    public static class AuthenticationEngineSuite
    {
        #region Private-Members

        private static readonly object _Lock = new object();
        private static SqliteDatabaseDriver _Driver = null!;
        private static string _DbPath = null!;
        private static AuthenticationEngine _Engine = null!;
        private static SessionTokenService _Tokens = null!;

        private static string _TenantGuid = null!;
        private static string _UserGuid = null!;
        private const string _Password = "correct-horse-battery-staple";
        private const string _AccessKey = "access_engine_fixture";
        private const string _SecretKey = "secret_engine_fixture_value";

        #endregion

        #region Public-Methods

        /// <summary>
        /// Build the authentication engine suite.
        /// </summary>
        /// <returns>Suite descriptor.</returns>
        public static TestSuiteDescriptor Build()
        {
            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>
            {
                new TestCaseDescriptor("Auth", "Login_Success", "Valid login mints a resolvable bearer token",
                    ct =>
                    {
                        EnsureFixtures();
                        AuthSession session;
                        string token = _Engine.Login(_TenantGuid, "engine@sharpai.local", _Password, 0, out session);
                        TestAssert.True(token != null && session != null, "login should succeed and return a token");
                        TestAssert.Equal(_UserGuid, session.UserGuid);

                        AuthSession resolved = _Engine.ReadSession(token);
                        TestAssert.True(resolved != null && resolved.Guid == session.Guid, "token should resolve to the session");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Auth", "Login_WrongPassword", "Wrong password fails without a session",
                    ct =>
                    {
                        EnsureFixtures();
                        AuthSession session;
                        string token = _Engine.Login(_TenantGuid, "engine@sharpai.local", "wrong", 0, out session);
                        TestAssert.True(token == null && session == null, "wrong password should not mint a token");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Auth", "Login_UnknownUser", "Unknown email fails cleanly",
                    ct =>
                    {
                        EnsureFixtures();
                        AuthSession session;
                        string token = _Engine.Login(_TenantGuid, "nobody@sharpai.local", _Password, 0, out session);
                        TestAssert.True(token == null && session == null, "unknown user should not mint a token");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Auth", "Login_DefaultTenant", "Login resolves the default tenant when guid omitted",
                    ct =>
                    {
                        EnsureFixtures();
                        AuthSession session;
                        string token = _Engine.Login(null, "engine@sharpai.local", _Password, 0, out session);
                        TestAssert.True(token != null, "login should resolve the 'default' tenant when tenant guid is omitted");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Auth", "Authenticate_Bearer", "Bearer scheme yields an authenticated user context",
                    ct =>
                    {
                        EnsureFixtures();
                        AuthSession session;
                        string token = _Engine.Login(_TenantGuid, "engine@sharpai.local", _Password, 0, out session);

                        RequestContext context = _Engine.Authenticate(true, "/api/chat", null, null, token, null, null);
                        TestAssert.True(context.IsAuthenticated && !context.ShouldChallenge, "valid bearer should authenticate");
                        TestAssert.True(context.AuthScheme == AuthSchemeEnum.BearerToken, "scheme should be BearerToken");
                        TestAssert.Equal(_UserGuid, context.PrincipalGuid);
                        TestAssert.True(context.IsTenantAdmin, "fixture user is a tenant admin");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Auth", "Authenticate_AccessKey", "Access-key/secret scheme yields a credential context",
                    ct =>
                    {
                        EnsureFixtures();
                        RequestContext context = _Engine.Authenticate(true, "/api/chat", null, null, null, _AccessKey, _SecretKey);
                        TestAssert.True(context.IsAuthenticated && !context.ShouldChallenge, "valid access key should authenticate");
                        TestAssert.True(context.AuthScheme == AuthSchemeEnum.AccessKeySecret, "scheme should be AccessKeySecret");
                        TestAssert.True(context.PrincipalType == PrincipalTypeEnum.Credential, "principal type should be Credential");
                        TestAssert.Equal(_UserGuid, context.OwnerUserGuid);
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Auth", "Authenticate_BadSecret", "Wrong secret is challenged",
                    ct =>
                    {
                        EnsureFixtures();
                        RequestContext context = _Engine.Authenticate(true, "/api/chat", null, null, null, _AccessKey, "secret_wrong");
                        TestAssert.True(context.ShouldChallenge, "wrong secret should be challenged");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Auth", "Authenticate_NoCredential", "No credential on a protected path is challenged",
                    ct =>
                    {
                        EnsureFixtures();
                        RequestContext context = _Engine.Authenticate(true, "/api/chat", null, null, null, null, null);
                        TestAssert.True(context.ShouldChallenge, "missing credentials should be challenged");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Auth", "Authenticate_Disabled_System", "Disabled auth yields the system principal",
                    ct =>
                    {
                        EnsureFixtures();
                        RequestContext context = _Engine.Authenticate(false, "/api/chat", null, null, null, null, null);
                        TestAssert.True(context.IsAuthenticated && context.IsAdmin && !context.ShouldChallenge, "disabled auth is the system principal");
                        TestAssert.True(context.PrincipalType == PrincipalTypeEnum.System, "principal type should be System");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Auth", "Authenticate_Anonymous", "Token endpoint is anonymous even with auth on",
                    ct =>
                    {
                        EnsureFixtures();
                        RequestContext context = _Engine.Authenticate(true, "/v1.0/token", null, null, null, null, null);
                        TestAssert.True(context.IsAnonymousEndpoint && !context.ShouldChallenge, "the token endpoint must be reachable anonymously");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Auth", "Revoke_Invalidates", "A revoked session no longer resolves or authenticates",
                    ct =>
                    {
                        EnsureFixtures();
                        AuthSession session;
                        string token = _Engine.Login(_TenantGuid, "engine@sharpai.local", _Password, 0, out session);

                        _Engine.RevokeSession(session.Guid, "test-revoke");
                        TestAssert.True(_Engine.ReadSession(token) == null, "revoked session should not resolve");

                        RequestContext context = _Engine.Authenticate(true, "/api/chat", null, null, token, null, null);
                        TestAssert.True(context.ShouldChallenge, "revoked bearer should be challenged");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Auth", "Expired_Rejected", "An expired session is rejected",
                    ct =>
                    {
                        EnsureFixtures();
                        AuthSession expired = new AuthSession
                        {
                            UserGuid = _UserGuid,
                            TenantGuid = _TenantGuid,
                            CreatedUtc = DateTime.UtcNow.AddMinutes(-120),
                            ExpiresUtc = DateTime.UtcNow.AddMinutes(-60)
                        };
                        Db().Sessions.Create(expired);
                        string token = _Tokens.Encrypt(expired.Guid);
                        TestAssert.True(_Engine.ReadSession(token) == null, "expired session should not resolve");
                        return Task.CompletedTask;
                    })
            };

            return new TestSuiteDescriptor("Auth", "Authentication engine (SQLite contract)", cases, BeforeSuiteAsync, AfterSuiteAsync);
        }

        #endregion

        #region Private-Methods

        private static ValueTask BeforeSuiteAsync(CancellationToken token)
        {
            EnsureFixtures();
            return new ValueTask();
        }

        private static ValueTask AfterSuiteAsync(CancellationToken token)
        {
            // Intentionally leave the shared driver in place; the same descriptor is consumed across runners.
            return new ValueTask();
        }

        private static SqliteDatabaseDriver Db()
        {
            lock (_Lock)
            {
                if (_Driver == null)
                {
                    _DbPath = Path.Combine(Path.GetTempPath(), "sharpai-authengine-" + Guid.NewGuid().ToString("N") + ".db");
                    _Driver = new SqliteDatabaseDriver(new DatabaseSettings(_DbPath), new LoggingModule());
                    _Driver.InitializeAsync().GetAwaiter().GetResult();
                    _Tokens = new SessionTokenService("engine-suite-signing-key");
                    _Engine = new AuthenticationEngine(_Driver, _Tokens);
                }
                return _Driver;
            }
        }

        private static void EnsureFixtures()
        {
            lock (_Lock)
            {
                Db();
                if (_TenantGuid != null) return;

                Tenant tenant = Db().Tenants.Create(new Tenant { Name = "default", IsProtected = true });
                _TenantGuid = tenant.Guid;

                User user = Db().Users.Create(new User
                {
                    TenantGuid = _TenantGuid,
                    FirstName = "Engine",
                    LastName = "Fixture",
                    Email = "engine@sharpai.local",
                    PasswordSha256 = PasswordHasher.Hash(_Password),
                    IsTenantAdmin = true
                });
                _UserGuid = user.Guid;

                Db().Credentials.Create(new Credential
                {
                    UserGuid = _UserGuid,
                    TenantGuid = _TenantGuid,
                    Name = "engine-fixture",
                    AccessKey = _AccessKey,
                    SecretSha256 = PasswordHasher.Hash(_SecretKey)
                });
            }
        }

        #endregion
    }
}
