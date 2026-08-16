namespace Test.Shared
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;

    using SharpAI.Telemetry;

    using Touchstone.Core;

    /// <summary>
    /// Touchstone suite for <see cref="SharpAITelemetry"/>. Telemetry must be a safe no-op when no listener
    /// is attached: recording, spans, and the resident-model provider must never throw and must not require
    /// a running exporter. These cases run without any telemetry host present.
    /// </summary>
    public static class TelemetrySuite
    {
        #region Public-Methods

        /// <summary>
        /// Build the telemetry suite.
        /// </summary>
        /// <returns>Telemetry suite.</returns>
        public static TestSuiteDescriptor Build()
        {
            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>
            {
                new TestCaseDescriptor("Telemetry", "Names_Stable", "Instrument source names are the stable contract",
                    ct =>
                    {
                        TestAssert.Equal("SharpAI.Inference", SharpAITelemetry.MeterName);
                        TestAssert.Equal("SharpAI.Models", SharpAITelemetry.ModelsMeterName);
                        TestAssert.Equal("SharpAI.Inference", SharpAITelemetry.ActivitySourceName);
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Telemetry", "Record_NoListener", "Recording without a listener does not throw",
                    ct =>
                    {
                        SharpAITelemetry.RecordInference("chat", "test-model", 0.25, 42, true);
                        SharpAITelemetry.RecordInference("completion", null, 0.0, 0, false);
                        SharpAITelemetry.RecordInference("embedding", "m", -1.0, -5, true);
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Telemetry", "Span_NullSafe", "Starting a span without a listener is null-safe",
                    ct =>
                    {
                        Activity? activity = SharpAITelemetry.StartInference("chat", "test-model");
                        // With no ActivityListener registered, StartActivity returns null; disposing null is not
                        // our concern, but the call itself must not throw.
                        activity?.Dispose();
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Telemetry", "Provider_AcceptsAndIgnoresNull", "Resident-model provider accepts a value and ignores null",
                    ct =>
                    {
                        SharpAITelemetry.SetResidentModelCountProvider(() => 3);
                        SharpAITelemetry.SetResidentModelCountProvider(null);
                        return Task.CompletedTask;
                    })
            };

            return new TestSuiteDescriptor("Telemetry", "Telemetry no-op safety", cases);
        }

        #endregion
    }
}
