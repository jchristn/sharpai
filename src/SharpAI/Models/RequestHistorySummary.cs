namespace SharpAI.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Time-bucketed summary of request history for chart rendering.
    /// </summary>
    public class RequestHistorySummary
    {
        #region Public-Members

        /// <summary>
        /// Total number of requests in the range.
        /// </summary>
        public long TotalCount { get; set; } = 0;

        /// <summary>
        /// Total successful (status &lt; 400) requests.
        /// </summary>
        public long TotalSuccess { get; set; } = 0;

        /// <summary>
        /// Total failed (status &gt;= 400) requests.
        /// </summary>
        public long TotalFailure { get; set; } = 0;

        /// <summary>
        /// Average request duration in milliseconds across the range.
        /// </summary>
        public double AverageDurationMs { get; set; } = 0;

        /// <summary>
        /// Buckets covering the range, including empty ones. Never null.
        /// </summary>
        public List<RequestHistoryBucket> Buckets
        {
            get
            {
                return _Buckets;
            }
            set
            {
                _Buckets = value ?? new List<RequestHistoryBucket>();
            }
        }

        #endregion

        #region Private-Members

        private List<RequestHistoryBucket> _Buckets = new List<RequestHistoryBucket>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistorySummary()
        {
        }

        #endregion
    }
}
