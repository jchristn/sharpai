namespace SharpAI.Database
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using SharpAI.Database.Mysql;
    using SharpAI.Database.Postgresql;
    using SharpAI.Database.Sqlite;
    using SharpAI.Database.SqlServer;

    using SyslogLogging;

    /// <summary>
    /// Composition root for the data layer: creates the provider-specific database driver for the
    /// configured <see cref="DatabaseSettings.Type"/>.
    /// </summary>
    public static class DatabaseDriverFactory
    {
        /// <summary>
        /// Create a driver for the configured provider.
        /// </summary>
        /// <param name="settings">Database settings. May not be null.</param>
        /// <param name="logging">Logging module. May not be null.</param>
        /// <returns>Database driver.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the database type is unknown.</exception>
        public static DatabaseDriverBase Create(DatabaseSettings settings, LoggingModule logging)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            switch (settings.Type)
            {
                case DatabaseTypeEnum.Sqlite:
                    return new SqliteDatabaseDriver(settings, logging);
                case DatabaseTypeEnum.Mysql:
                    return new MysqlDatabaseDriver(settings, logging);
                case DatabaseTypeEnum.Postgresql:
                    return new PostgresqlDatabaseDriver(settings, logging);
                case DatabaseTypeEnum.SqlServer:
                    return new SqlServerDatabaseDriver(settings, logging);
                default:
                    throw new ArgumentException("Unknown database type: " + settings.Type);
            }
        }

        /// <summary>
        /// Create and initialize (create schema, apply migrations) a driver for the configured provider.
        /// </summary>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Initialized database driver.</returns>
        public static async Task<DatabaseDriverBase> CreateAndInitializeAsync(
            DatabaseSettings settings,
            LoggingModule logging,
            CancellationToken token = default)
        {
            DatabaseDriverBase driver = Create(settings, logging);
            await driver.InitializeAsync(token).ConfigureAwait(false);
            return driver;
        }
    }
}
