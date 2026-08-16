namespace SharpAI.Database
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A versioned, ordered set of DDL statements applied to bring the schema to a given version. Migrations
    /// are tracked in the <c>schema_migrations</c> table and are safe to run repeatedly (idempotent DDL).
    /// </summary>
    public class SchemaMigration
    {
        #region Public-Members

        /// <summary>
        /// Monotonic migration version. Minimum 1.
        /// </summary>
        public int Version
        {
            get
            {
                return _Version;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(Version), "Version must be at least 1.");
                _Version = value;
            }
        }

        /// <summary>
        /// Human-readable description of the migration.
        /// </summary>
        public string Description { get; set; } = String.Empty;

        /// <summary>
        /// Ordered DDL statements to apply. Never null.
        /// </summary>
        public List<string> Statements
        {
            get
            {
                return _Statements;
            }
            set
            {
                _Statements = value ?? new List<string>();
            }
        }

        #endregion

        #region Private-Members

        private int _Version = 1;
        private List<string> _Statements = new List<string>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="version">Migration version (minimum 1).</param>
        /// <param name="description">Description.</param>
        /// <param name="statements">Ordered DDL statements.</param>
        public SchemaMigration(int version, string description, List<string> statements)
        {
            Version = version;
            Description = description ?? String.Empty;
            Statements = statements;
        }

        #endregion
    }
}
