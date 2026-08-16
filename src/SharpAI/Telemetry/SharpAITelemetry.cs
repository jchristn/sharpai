namespace SharpAI.Telemetry
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;

    /// <summary>
    /// Central telemetry surface for the SharpAI core library. Instruments are emitted through the .NET
    /// BCL (<see cref="Meter"/> and <see cref="ActivitySource"/>) so the core carries no dependency on any
    /// telemetry host or exporter. A host process (for example the server, via Radiant) subscribes to the
    /// meter and activity source by name; when nothing subscribes, recording is a near-zero-cost no-op.
    ///
    /// Metric names follow OpenTelemetry conventions; after Prometheus suffixing they render as
    /// <c>sharpai_inference_requests_total</c>, <c>sharpai_inference_tokens_generated_total</c>,
    /// <c>sharpai_inference_latency_seconds</c>, and <c>sharpai_models_resident</c>.
    /// </summary>
    public static class SharpAITelemetry
    {
        #region Public-Members

        /// <summary>
        /// Name of the inference meter and activity source. Subscribe to this name to receive inference
        /// metrics and spans. This is a stable public contract.
        /// </summary>
        public const string MeterName = "SharpAI.Inference";

        /// <summary>
        /// Name of the models meter, which emits gauges about loaded models. Stable public contract.
        /// </summary>
        public const string ModelsMeterName = "SharpAI.Models";

        /// <summary>
        /// Name of the inference activity (trace) source. Stable public contract.
        /// </summary>
        public const string ActivitySourceName = "SharpAI.Inference";

        /// <summary>
        /// Activity source used to create inference spans. Never null.
        /// </summary>
        public static ActivitySource Source
        {
            get { return _Source; }
        }

        #endregion

        #region Private-Members

        private static readonly Meter _Meter = new Meter(MeterName);
        private static readonly Meter _ModelsMeter = new Meter(ModelsMeterName);
        private static readonly ActivitySource _Source = new ActivitySource(ActivitySourceName);

        private static readonly Counter<long> _Requests =
            _Meter.CreateCounter<long>("sharpai.inference.requests", "{request}", "Inference requests processed.");
        private static readonly Counter<long> _Tokens =
            _Meter.CreateCounter<long>("sharpai.inference.tokens_generated", "{token}", "Tokens generated during inference.");
        private static readonly Histogram<double> _Latency =
            _Meter.CreateHistogram<double>("sharpai.inference.latency", "s", "End-to-end inference latency in seconds.");

        private static Func<int> _ResidentModelProvider = () => 0;

        #endregion

        #region Constructors-and-Factories

        static SharpAITelemetry()
        {
            _ModelsMeter.CreateObservableGauge<long>(
                "sharpai.models.resident",
                ObserveResidentModels,
                "{model}",
                "Number of models currently resident in memory.");
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Register a callback that reports the number of models currently resident in memory. The most
        /// recent non-null provider wins. Passing null is ignored.
        /// </summary>
        /// <param name="provider">Callback returning the resident model count.</param>
        public static void SetResidentModelCountProvider(Func<int> provider)
        {
            if (provider != null) _ResidentModelProvider = provider;
        }

        /// <summary>
        /// Start an inference span. The returned activity may be null when no trace listener is active;
        /// callers must null-check or use it inside a <c>using</c> that tolerates null.
        /// </summary>
        /// <param name="operation">Operation name, for example "chat" or "completion".</param>
        /// <param name="model">Model identifier, or null.</param>
        /// <returns>Started activity, or null when nothing is sampling.</returns>
        public static Activity StartInference(string operation, string model)
        {
            Activity activity = _Source.StartActivity("inference." + (operation ?? "unknown"), ActivityKind.Internal);
            if (activity != null)
            {
                activity.SetTag("sharpai.operation", operation ?? "unknown");
                activity.SetTag("sharpai.model", model ?? "unknown");
            }
            return activity;
        }

        /// <summary>
        /// Record the outcome of an inference call: the request count, its latency, and the number of
        /// tokens generated. Only low-cardinality tags (operation, model, outcome) are attached to metrics.
        /// </summary>
        /// <param name="operation">Operation type, for example "chat", "completion", or "embedding".</param>
        /// <param name="model">Model identifier, or null.</param>
        /// <param name="seconds">Elapsed time in seconds. Negative values are clamped to zero.</param>
        /// <param name="tokensGenerated">Number of tokens generated. Values below zero are treated as zero.</param>
        /// <param name="success">True when the call completed successfully.</param>
        public static void RecordInference(string operation, string model, double seconds, long tokensGenerated, bool success)
        {
            if (seconds < 0) seconds = 0;

            TagList tags = new TagList
            {
                { "operation", operation ?? "unknown" },
                { "model", model ?? "unknown" },
                { "outcome", success ? "ok" : "error" }
            };

            _Requests.Add(1, tags);
            _Latency.Record(seconds, tags);

            if (tokensGenerated > 0)
            {
                TagList tokenTags = new TagList
                {
                    { "operation", operation ?? "unknown" },
                    { "model", model ?? "unknown" }
                };
                _Tokens.Add(tokensGenerated, tokenTags);
            }
        }

        #endregion

        #region Private-Methods

        private static long ObserveResidentModels()
        {
            try
            {
                return _ResidentModelProvider();
            }
            catch
            {
                return 0;
            }
        }

        #endregion
    }
}
