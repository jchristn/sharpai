namespace SharpAI.Models
{
    using System;
    using System.Collections.Generic;

    using SharpAI.Helpers;

    /// <summary>
    /// A captured HTTP request and its response, used by the dashboard's activity and request-history
    /// views and for operational diagnostics. Scalar fields are typed columns; the header maps are
    /// genuinely schemaless and stored as JSON.
    /// </summary>
    public class RequestHistoryEntry
    {
        #region Public-Members

        /// <summary>
        /// Entry identifier (prefix "req_"). Never null or empty.
        /// </summary>
        public string Id
        {
            get
            {
                return _Id;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Id));
                _Id = value;
            }
        }

        /// <summary>
        /// Tenant identifier, or null for unauthenticated requests.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// User identifier, or null for unauthenticated requests.
        /// </summary>
        public string UserId { get; set; } = null;

        /// <summary>
        /// Principal display name, if resolved.
        /// </summary>
        public string PrincipalName { get; set; } = null;

        /// <summary>
        /// HTTP method (GET, POST, etc.).
        /// </summary>
        public string Method { get; set; } = String.Empty;

        /// <summary>
        /// Route template that matched, or the raw path when no template.
        /// </summary>
        public string Path { get; set; } = String.Empty;

        /// <summary>
        /// Full request URL including query string.
        /// </summary>
        public string Url { get; set; } = String.Empty;

        /// <summary>
        /// Response HTTP status code.
        /// </summary>
        public int StatusCode { get; set; } = 0;

        /// <summary>
        /// Request duration in milliseconds.
        /// </summary>
        public double DurationMs { get; set; } = 0;

        /// <summary>
        /// Client source IP as observed by the server.
        /// </summary>
        public string SourceIp { get; set; } = null;

        /// <summary>
        /// Request headers with secrets redacted. Never null.
        /// </summary>
        public Dictionary<string, string> RequestHeaders
        {
            get
            {
                return _RequestHeaders;
            }
            set
            {
                _RequestHeaders = value ?? new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Request body (may be truncated).
        /// </summary>
        public string RequestBody { get; set; } = null;

        /// <summary>
        /// Original request body length in bytes, before truncation.
        /// </summary>
        public long RequestBodyBytes { get; set; } = 0;

        /// <summary>
        /// Whether the request body was truncated.
        /// </summary>
        public bool RequestBodyTruncated { get; set; } = false;

        /// <summary>
        /// Response headers. Never null.
        /// </summary>
        public Dictionary<string, string> ResponseHeaders
        {
            get
            {
                return _ResponseHeaders;
            }
            set
            {
                _ResponseHeaders = value ?? new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Response body (may be truncated).
        /// </summary>
        public string ResponseBody { get; set; } = null;

        /// <summary>
        /// Original response body length in bytes, before truncation.
        /// </summary>
        public long ResponseBodyBytes { get; set; } = 0;

        /// <summary>
        /// Whether the response body was truncated.
        /// </summary>
        public bool ResponseBodyTruncated { get; set; } = false;

        /// <summary>
        /// UTC time the request began.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC time the response was sent, if known.
        /// </summary>
        public DateTime? CompletedUtc { get; set; } = null;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.GenerateRequestHistoryId();
        private Dictionary<string, string> _RequestHeaders = new Dictionary<string, string>();
        private Dictionary<string, string> _ResponseHeaders = new Dictionary<string, string>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistoryEntry()
        {
        }

        #endregion
    }
}
