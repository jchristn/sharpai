namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Test.Shared;

    using Touchstone.Core;
    using Touchstone.XunitAdapter;

    using Xunit;

    /// <summary>
    /// Runs every SharpAI Touchstone descriptor sequentially as a single xUnit fact.
    /// </summary>
    public sealed class SharpAIFactTests : TouchstoneFactBase
    {
        /// <inheritdoc />
        protected override IReadOnlyList<TestSuiteDescriptor> Suites
        {
            get { return SharpAISuites.All; }
        }

        /// <summary>
        /// Execute all suites; fails if any descriptor throws.
        /// </summary>
        /// <returns>Task.</returns>
        [Fact]
        public async Task RunAll()
        {
            // xUnit analyzer xUnit1030: test methods must not ConfigureAwait(false).
            await RunAllAsync();
        }
    }
}
