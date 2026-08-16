namespace SharpAI.Server.Classes.Settings
{
    using System;

    /// <summary>
    /// Telemetry settings controlling in-process OpenTelemetry export (metrics, traces, and optionally
    /// logs) via Radiant, plus an optional in-process Prometheus scrape endpoint. All fields can be
    /// overridden by <c>SHARPAI_TELEMETRY_*</c> environment variables at startup. Telemetry is a clean
    /// no-op when <see cref="Enable"/> is false or when no collector is reachable.
    /// </summary>
    public class TelemetrySettings
    {
        #region Public-Members

        /// <summary>
        /// Whether telemetry is enabled. Default true. When false, no meters or activity sources are
        /// subscribed and no exporter is started.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// Logical service name attached to all telemetry. Default "sharpai". May not be null or empty.
        /// </summary>
        public string ServiceName
        {
            get
            {
                return _ServiceName;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(ServiceName));
                _ServiceName = value;
            }
        }

        /// <summary>
        /// OTLP collector endpoint. Default "http://localhost:4317". May not be null or empty.
        /// </summary>
        public string OtlpEndpoint
        {
            get
            {
                return _OtlpEndpoint;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(OtlpEndpoint));
                _OtlpEndpoint = value;
            }
        }

        /// <summary>
        /// OTLP protocol: "grpc" (default) or "httpprotobuf". Any other value falls back to "grpc".
        /// </summary>
        public string OtlpProtocol
        {
            get
            {
                return _OtlpProtocol;
            }
            set
            {
                _OtlpProtocol = String.IsNullOrEmpty(value) ? "grpc" : value;
            }
        }

        /// <summary>
        /// Whether to serve an in-process Prometheus scrape endpoint (separate HttpListener). Default false;
        /// in the bundled Docker stack, metrics reach Prometheus through the OTLP collector instead.
        /// </summary>
        public bool PrometheusEnable { get; set; } = false;

        /// <summary>
        /// Hostname for the in-process Prometheus endpoint. Default "localhost".
        /// </summary>
        public string PrometheusHostname
        {
            get
            {
                return _PrometheusHostname;
            }
            set
            {
                _PrometheusHostname = String.IsNullOrEmpty(value) ? "localhost" : value;
            }
        }

        /// <summary>
        /// Port for the in-process Prometheus endpoint. Default 9464. Clamped to the range 0-65535.
        /// </summary>
        public int PrometheusPort
        {
            get
            {
                return _PrometheusPort;
            }
            set
            {
                _PrometheusPort = Math.Clamp(value, 0, 65535);
            }
        }

        /// <summary>
        /// Path for the in-process Prometheus endpoint. Default "/metrics".
        /// </summary>
        public string PrometheusPath
        {
            get
            {
                return _PrometheusPath;
            }
            set
            {
                _PrometheusPath = String.IsNullOrEmpty(value) ? "/metrics" : value;
            }
        }

        /// <summary>
        /// Whether metrics are exported. Default true.
        /// </summary>
        public bool EnableMetrics { get; set; } = true;

        /// <summary>
        /// Whether traces are exported. Default true.
        /// </summary>
        public bool EnableTraces { get; set; } = true;

        /// <summary>
        /// Whether logs are exported over OTLP. Default false; the bundled stack ships logs to Loki by
        /// tailing the server's log files, so OTLP log export is off unless explicitly enabled.
        /// </summary>
        public bool EnableLogs { get; set; } = false;

        #endregion

        #region Private-Members

        private string _ServiceName = "sharpai";
        private string _OtlpEndpoint = "http://localhost:4317";
        private string _OtlpProtocol = "grpc";
        private string _PrometheusHostname = "localhost";
        private int _PrometheusPort = 9464;
        private string _PrometheusPath = "/metrics";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public TelemetrySettings()
        {
        }

        #endregion
    }
}
