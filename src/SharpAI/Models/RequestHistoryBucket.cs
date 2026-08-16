namespace SharpAI.Models
{
    using System;

    /// <summary>
    /// A single time bucket in a request-history summary. The server emits a bucket for every interval in
    /// the requested range, including empty ones, so the dashboard can render a continuous chart.
    /// </summary>
    public class RequestHistoryBucket
    {
        #region Public-Members

        /// <summary>
        /// UTC start of the bucket (inclusive).
        /// </summary>
        public DateTime BucketStartUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC end of the bucket (exclusive).
        /// </summary>
        public DateTime BucketEndUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Number of successful (status &lt; 400) requests in the bucket.
        /// </summary>
        public int SuccessCount { get; set; } = 0;

        /// <summary>
        /// Number of failed (status &gt;= 400) requests in the bucket.
        /// </summary>
        public int FailureCount { get; set; } = 0;

        /// <summary>
        /// Average request duration in milliseconds within the bucket.
        /// </summary>
        public double AverageDurationMs { get; set; } = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistoryBucket()
        {
        }

        #endregion
    }
}
