namespace SharpAI.Database
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Provider-neutral database connection settings. For SQLite only <see cref="Filename"/> is required;
    /// for server databases set <see cref="Hostname"/>, <see cref="Port"/>, <see cref="DatabaseName"/>,
    /// <see cref="Username"/>, and <see cref="Password"/>.
    /// </summary>
    public class DatabaseSettings
    {
        #region Public-Members

        /// <summary>
        /// Database provider type. Default <see cref="DatabaseTypeEnum.Sqlite"/>.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DatabaseTypeEnum Type { get; set; } = DatabaseTypeEnum.Sqlite;

        /// <summary>
        /// SQLite database file path. Used only when <see cref="Type"/> is Sqlite. Default "./sharpai.db".
        /// </summary>
        public string Filename
        {
            get
            {
                return _Filename;
            }
            set
            {
                _Filename = String.IsNullOrEmpty(value) ? "./sharpai.db" : value;
            }
        }

        /// <summary>
        /// Server hostname or IP for server databases. Prefer 127.0.0.1 over localhost for loopback.
        /// </summary>
        public string Hostname { get; set; } = "127.0.0.1";

        /// <summary>
        /// Server port. 0 uses the provider default (MySQL 3306, PostgreSQL 5432, SQL Server 1433).
        /// </summary>
        public int Port
        {
            get
            {
                return _Port;
            }
            set
            {
                _Port = Math.Clamp(value, 0, 65535);
            }
        }

        /// <summary>
        /// Database (catalog) name for server databases.
        /// </summary>
        public string DatabaseName { get; set; } = "sharpai";

        /// <summary>
        /// Username for server databases.
        /// </summary>
        public string Username { get; set; } = null;

        /// <summary>
        /// Password for server databases.
        /// </summary>
        public string Password { get; set; } = null;

        #endregion

        #region Private-Members

        private string _Filename = "./sharpai.db";
        private int _Port = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate with defaults (SQLite).
        /// </summary>
        public DatabaseSettings()
        {
        }

        /// <summary>
        /// Instantiate a SQLite configuration with the specified file.
        /// </summary>
        /// <param name="filename">SQLite database file path.</param>
        public DatabaseSettings(string filename)
        {
            Type = DatabaseTypeEnum.Sqlite;
            Filename = filename;
        }

        #endregion
    }
}
