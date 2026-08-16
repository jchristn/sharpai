namespace Test.Nunit
{
    using System.Collections;
    using System.Threading;
    using System.Threading.Tasks;

    using NUnit.Framework;

    using Test.Shared;

    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    /// <summary>
    /// Exposes each SharpAI Touchstone descriptor as an individual NUnit test case.
    /// </summary>
    [TestFixture]
    public sealed class SharpAINunitTests
    {
        private static IEnumerable TestCases()
        {
            return new TouchstoneTestCaseSource(SharpAISuites.All);
        }

        /// <summary>
        /// Execute a single descriptor.
        /// </summary>
        /// <param name="testCase">Test case to execute.</param>
        /// <returns>Task.</returns>
        [Test]
        [TestCaseSource(nameof(TestCases))]
        public async Task RunCase(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
