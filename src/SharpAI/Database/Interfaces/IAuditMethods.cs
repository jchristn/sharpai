namespace SharpAI.Database.Interfaces
{
    using System;

    using SharpAI.Models;
    using SharpAI.Security;

    /// <summary>
    /// Data-access methods for the security audit log. Handwritten, provider-aware SQL.
    /// </summary>
    public interface IAuditMethods
    {
        /// <summary>Insert an audit entry.</summary>
        /// <param name="entry">Entry.</param>
        void Create(AuditLogEntry entry);

        /// <summary>Read an audit entry by identifier.</summary>
        /// <param name="guid">Entry identifier.</param>
        /// <returns>The entry, or null.</returns>
        AuditLogEntry Read(string guid);

        /// <summary>Page through audit entries, optionally scoped to a tenant.</summary>
        /// <param name="tenantGuid">Tenant identifier, or null for all tenants.</param>
        /// <param name="query">Enumeration query.</param>
        /// <returns>Enumeration result.</returns>
        EnumerationResult<AuditLogEntry> Enumerate(string tenantGuid, EnumerationQuery query);

        /// <summary>Prune audit entries created before the given UTC cutoff.</summary>
        /// <param name="olderThanUtc">Cutoff.</param>
        /// <returns>Number of rows deleted.</returns>
        int Prune(DateTime olderThanUtc);
    }
}
