namespace Test.Shared
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using SharpAI.Security;

    using Touchstone.Core;

    /// <summary>
    /// Touchstone suite for <see cref="AuthEvaluator"/>. Covers the disabled (open-server, Ollama-parity)
    /// default, anonymous endpoint handling, admin-API-key authentication, the challenge path, and the
    /// constant-time secret comparison.
    /// </summary>
    public static class AuthEvaluatorSuite
    {
        #region Public-Methods

        /// <summary>
        /// Build the auth evaluator suite.
        /// </summary>
        /// <returns>Auth evaluator suite.</returns>
        public static TestSuiteDescriptor Build()
        {
            List<string> keys = new List<string> { "secret_adminkey_123456" };

            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>
            {
                new TestCaseDescriptor("Auth", "Disabled_SystemPrincipal", "Disabled auth yields the system principal",
                    ct =>
                    {
                        RequestContext c = AuthEvaluator.Evaluate(false, "/api/chat", null, keys);
                        TestAssert.True(c.IsAuthenticated, "system principal is authenticated");
                        TestAssert.True(c.IsAdmin, "system principal is admin");
                        TestAssert.Equal(PrincipalTypeEnum.System, c.PrincipalType);
                        TestAssert.True(!c.ShouldChallenge, "disabled auth never challenges");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Auth", "Enabled_AnonymousPaths", "Anonymous paths are allowed when enabled",
                    ct =>
                    {
                        foreach (string p in new string[] { "/", "/health", "/ready", "/openapi.json", "/swagger", "/swagger/index.html", "/favicon.ico" })
                        {
                            RequestContext c = AuthEvaluator.Evaluate(true, p, null, keys);
                            TestAssert.True(c.IsAnonymousEndpoint, "path should be anonymous: " + p);
                            TestAssert.True(!c.ShouldChallenge, "anonymous path should not challenge: " + p);
                        }
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Auth", "Enabled_NoKey_Challenges", "Protected path with no key challenges",
                    ct =>
                    {
                        RequestContext c = AuthEvaluator.Evaluate(true, "/api/chat", null, keys);
                        TestAssert.True(!c.IsAuthenticated, "should not be authenticated");
                        TestAssert.True(c.ShouldChallenge, "should challenge");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Auth", "Enabled_ValidKey_Admin", "Valid admin key yields an administrator",
                    ct =>
                    {
                        RequestContext c = AuthEvaluator.Evaluate(true, "/api/chat", "secret_adminkey_123456", keys);
                        TestAssert.True(c.IsAuthenticated && c.IsAdmin, "should be an authenticated admin");
                        TestAssert.Equal(PrincipalTypeEnum.Administrator, c.PrincipalType);
                        TestAssert.Equal(AuthSchemeEnum.AdminApiKey, c.AuthScheme);
                        TestAssert.True(!c.ShouldChallenge, "valid key should not challenge");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Auth", "Enabled_WrongKey_Challenges", "Wrong admin key challenges",
                    ct =>
                    {
                        RequestContext c = AuthEvaluator.Evaluate(true, "/api/chat", "wrong-key", keys);
                        TestAssert.True(!c.IsAuthenticated && c.ShouldChallenge, "wrong key should challenge");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Auth", "SecureEquals", "Constant-time comparison behaves correctly",
                    ct =>
                    {
                        TestAssert.True(AuthEvaluator.SecureEquals("abc123", "abc123"), "equal strings match");
                        TestAssert.True(!AuthEvaluator.SecureEquals("abc123", "abc124"), "different strings differ");
                        TestAssert.True(!AuthEvaluator.SecureEquals("abc", "abcd"), "different lengths differ");
                        TestAssert.True(!AuthEvaluator.SecureEquals(null, "abc"), "null differs");
                        return Task.CompletedTask;
                    })
            };

            return new TestSuiteDescriptor("Auth", "Authentication evaluator", cases);
        }

        #endregion
    }
}
