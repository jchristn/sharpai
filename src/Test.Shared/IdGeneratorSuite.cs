namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using SharpAI.Helpers;

    using Touchstone.Core;

    /// <summary>
    /// Touchstone suite for <see cref="IdGenerator"/>. The generators are deterministic in shape (a fixed
    /// prefix followed by a GUID "N" body) but random in value, so the suite covers the stable contract:
    /// the declared prefix is emitted, the body is a 32-character lowercase-hex GUID with no hyphens,
    /// distinct generators never collide by prefix, and repeated generations stay unique.
    /// </summary>
    public static class IdGeneratorSuite
    {
        #region Public-Methods

        /// <summary>
        /// Build the id-generator suite.
        /// </summary>
        /// <returns>Id-generator suite.</returns>
        public static TestSuiteDescriptor Build()
        {
            // Map each declared prefix to the generator that must produce it. Keyed by prefix (which is
            // unique per entity) to avoid tuples, per the project's coding standards.
            Dictionary<string, Func<string>> generators = new Dictionary<string, Func<string>>
            {
                { IdGenerator.RequestHistoryPrefix, IdGenerator.GenerateRequestHistoryId },
                { IdGenerator.ModelPrefix, IdGenerator.GenerateModelId },
                { IdGenerator.TenantPrefix, IdGenerator.GenerateTenantId },
                { IdGenerator.UserPrefix, IdGenerator.GenerateUserId },
                { IdGenerator.CredentialPrefix, IdGenerator.GenerateCredentialId },
                { IdGenerator.SessionPrefix, IdGenerator.GenerateSessionId },
                { IdGenerator.AuditPrefix, IdGenerator.GenerateAuditId },
                { IdGenerator.RolePrefix, IdGenerator.GenerateRoleId },
                { IdGenerator.PermissionPrefix, IdGenerator.GeneratePermissionId },
                { IdGenerator.AssignmentPrefix, IdGenerator.GenerateAssignmentId }
            };

            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>
            {
                new TestCaseDescriptor("IdGen", "RequestHistory_Shape", "Request-history id has req_ prefix and a GUID-N body",
                    ct =>
                    {
                        string id = IdGenerator.GenerateRequestHistoryId();
                        TestAssert.True(id.StartsWith("req_", StringComparison.Ordinal), "expected req_ prefix, got " + id);
                        AssertGuidBody(id, "req_");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("IdGen", "Model_Shape", "Model id has mdl_ prefix and a GUID-N body",
                    ct =>
                    {
                        string id = IdGenerator.GenerateModelId();
                        TestAssert.True(id.StartsWith("mdl_", StringComparison.Ordinal), "expected mdl_ prefix, got " + id);
                        AssertGuidBody(id, "mdl_");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("IdGen", "All_Prefixes", "Every generator emits its declared prefix",
                    ct =>
                    {
                        foreach (KeyValuePair<string, Func<string>> kvp in generators)
                        {
                            string id = kvp.Value();
                            TestAssert.True(id.StartsWith(kvp.Key, StringComparison.Ordinal),
                                "generator for prefix '" + kvp.Key + "' produced '" + id + "'");
                        }
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("IdGen", "All_Bodies_GuidN", "Every body is a 32-char lowercase-hex GUID with no hyphens",
                    ct =>
                    {
                        foreach (KeyValuePair<string, Func<string>> kvp in generators)
                            AssertGuidBody(kvp.Value(), kvp.Key);
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("IdGen", "Bodies_No_Hyphen_Or_Upper", "Bodies contain no hyphens and no uppercase (negative format checks)",
                    ct =>
                    {
                        foreach (KeyValuePair<string, Func<string>> kvp in generators)
                        {
                            string id = kvp.Value();
                            string body = id.Substring(kvp.Key.Length);
                            TestAssert.DoesNotContain(body, "-");
                            foreach (char c in body)
                                TestAssert.True(!Char.IsUpper(c), "unexpected uppercase char in body: " + id);
                        }
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("IdGen", "Prefixes_Distinct", "Declared prefixes are mutually distinct so ids are unambiguous",
                    ct =>
                    {
                        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
                        foreach (string prefix in generators.Keys)
                            TestAssert.True(seen.Add(prefix), "duplicate prefix detected: " + prefix);
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("IdGen", "Unique_Across_Many", "Repeated generations do not collide",
                    ct =>
                    {
                        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
                        for (int i = 0; i < 5000; i++)
                            TestAssert.True(ids.Add(IdGenerator.GenerateRequestHistoryId()), "collision on iteration " + i);
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("IdGen", "Sequential_Differ", "Two sequential calls to the same generator differ",
                    ct =>
                    {
                        string first = IdGenerator.GenerateSessionId();
                        string second = IdGenerator.GenerateSessionId();
                        TestAssert.True(!String.Equals(first, second, StringComparison.Ordinal),
                            "sequential session ids were identical: " + first);
                        return Task.CompletedTask;
                    })
            };

            return new TestSuiteDescriptor("IdGen", "Identifier generator", cases);
        }

        #endregion

        #region Private-Methods

        private static void AssertGuidBody(string id, string prefix)
        {
            TestAssert.True(id.Length == prefix.Length + 32,
                "expected a 32-char body after '" + prefix + "', got '" + id + "'");
            string body = id.Substring(prefix.Length);
            Guid parsed;
            TestAssert.True(Guid.TryParseExact(body, "N", out parsed),
                "body is not a GUID-N value: '" + body + "'");
        }

        #endregion
    }
}
