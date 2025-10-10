namespace SharpAI.Classes.Runtime
{
    using System;

    /// <summary>
    /// Provides information about the native backend that was selected during initialization.
    /// </summary>
    public static class NativeBackendInfo
    {
        #region Public-Members

        /// <summary>
        /// Gets or sets the backend that was selected during initialization.
        /// </summary>
        public static string SelectedBackend
        {
            get
            {
                return _SelectedBackend;
            }
            set
            {
                _SelectedBackend = value;
            }
        }

        #endregion

        #region Private-Members

        private static string _SelectedBackend = "unknown";

        #endregion
    }
}
