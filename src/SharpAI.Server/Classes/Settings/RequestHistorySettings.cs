namespace SharpAI.Server.Classes.Settings
{
    using System;

    /// <summary>
    /// Request-history capture settings. Capture is enabled by default; bodies are truncated to the
    /// configured byte limits and rows older than the retention window are pruned on a schedule.
    /// </summary>
    public class RequestHistorySettings
    {
        #region Public-Members

        /// <summary>
        /// Whether request capture is enabled. Default true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Maximum request body bytes captured before truncation. Default 65536, range 0-1048576.
        /// </summary>
        public int MaxRequestBodyBytes
        {
            get
            {
                return _MaxRequestBodyBytes;
            }
            set
            {
                _MaxRequestBodyBytes = Math.Clamp(value, 0, 1024 * 1024);
            }
        }

        /// <summary>
        /// Maximum response body bytes captured before truncation. Default 65536, range 0-1048576.
        /// </summary>
        public int MaxResponseBodyBytes
        {
            get
            {
                return _MaxResponseBodyBytes;
            }
            set
            {
                _MaxResponseBodyBytes = Math.Clamp(value, 0, 1024 * 1024);
            }
        }

        /// <summary>
        /// Retention in days before a captured row is eligible for pruning. Default 30, range 1-3650.
        /// </summary>
        public int RetentionDays
        {
            get
            {
                return _RetentionDays;
            }
            set
            {
                _RetentionDays = Math.Clamp(value, 1, 3650);
            }
        }

        #endregion

        #region Private-Members

        private int _MaxRequestBodyBytes = 65536;
        private int _MaxResponseBodyBytes = 65536;
        private int _RetentionDays = 30;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistorySettings()
        {
        }

        #endregion
    }
}
