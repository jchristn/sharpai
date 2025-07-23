namespace SharpAI.Server.Classes.Settings
{
    using System;
    using System.Collections.Generic;
    using DatabaseWrapper.Core;
    using WatsonWebserver.Core;

    /// <summary>
    /// Settings.
    /// </summary>
    public class Settings
    {
        #region Public-Members

        /// <summary>
        /// Timestamp from creation, in UTC time.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Software version.
        /// </summary>
        public string SoftwareVersion
        {
            get
            {
                return _SoftwareVersion;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(SoftwareVersion));
                _SoftwareVersion = value;
            }
        }

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LoggingSettings Logging
        {
            get
            {
                return _Logging;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Logging));
                _Logging = value;
            }
        }

        /// <summary>
        /// Storage settings.
        /// </summary>
        public StorageSettings Storage
        {
            get
            {
                return _Storage;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Storage));
                _Storage = value;
            }
        }

        /// <summary>
        /// Database settings.
        /// </summary>
        public DatabaseSettings Database
        {
            get
            {
                return _Database;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Database));
                _Database = value;
            }
        }

        /// <summary>
        /// HuggingFace settings.
        /// </summary>
        public HuggingFaceSettings HuggingFace
        {
            get
            {
                return _HuggingFace;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(HuggingFace));
                _HuggingFace = value;
            }
        }

        /// <summary>
        /// REST settings.
        /// </summary>
        public WebserverSettings Rest
        {
            get
            {
                return _Rest;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Rest));
                _Rest = value;
            }
        }

        #endregion

        #region Private-Members

        private string _SoftwareVersion = "unknown";
        private LoggingSettings _Logging = new LoggingSettings();
        private StorageSettings _Storage = new StorageSettings();
        private DatabaseSettings _Database = new DatabaseSettings(Constants.DatabaseFile);
        private HuggingFaceSettings _HuggingFace = new HuggingFaceSettings();
        private WebserverSettings _Rest = new WebserverSettings();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public Settings()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}