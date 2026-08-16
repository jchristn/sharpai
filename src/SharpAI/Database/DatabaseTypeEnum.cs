namespace SharpAI.Database
{
    /// <summary>
    /// Supported database providers.
    /// </summary>
    public enum DatabaseTypeEnum
    {
        /// <summary>
        /// SQLite (file-based, the local default).
        /// </summary>
        Sqlite,

        /// <summary>
        /// MySQL / MariaDB.
        /// </summary>
        Mysql,

        /// <summary>
        /// PostgreSQL.
        /// </summary>
        Postgresql,

        /// <summary>
        /// Microsoft SQL Server.
        /// </summary>
        SqlServer
    }
}
