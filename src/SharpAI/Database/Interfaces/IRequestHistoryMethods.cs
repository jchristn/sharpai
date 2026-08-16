namespace SharpAI.Database.Interfaces
{
    using System;

    using SharpAI.Models;

    /// <summary>
    /// Domain-specific data-access methods for request history. Uses handwritten, provider-aware SQL.
    /// There is no unbounded "get all" — listing is via <see cref="Enumerate"/>.
    /// </summary>
    public interface IRequestHistoryMethods
    {
        /// <summary>
        /// Insert a captured request record.
        /// </summary>
        /// <param name="entry">Entry to insert.</param>
        void Create(RequestHistoryEntry entry);

        /// <summary>
        /// Read a single entry by identifier, including headers and bodies.
        /// </summary>
        /// <param name="id">Entry identifier.</param>
        /// <returns>The entry, or null when not found.</returns>
        RequestHistoryEntry Read(string id);

        /// <summary>
        /// Page through entries matching a filter. Returned entries omit request/response bodies (use
        /// <see cref="Read"/> for the full record).
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <returns>Enumeration result.</returns>
        EnumerationResult<RequestHistoryEntry> Enumerate(RequestHistoryQuery query);

        /// <summary>
        /// Produce a time-bucketed summary over a range, emitting a bucket for every interval including
        /// empty ones.
        /// </summary>
        /// <param name="query">Query defining the range, bucket width, and filters.</param>
        /// <returns>Summary.</returns>
        RequestHistorySummary Summarize(RequestHistoryQuery query);

        /// <summary>
        /// Delete a single entry by identifier.
        /// </summary>
        /// <param name="id">Entry identifier.</param>
        /// <returns>True if a record was deleted.</returns>
        bool Delete(string id);

        /// <summary>
        /// Delete all entries matching a filter.
        /// </summary>
        /// <param name="query">Filter.</param>
        /// <returns>Number of rows deleted.</returns>
        int DeleteMany(RequestHistoryQuery query);

        /// <summary>
        /// Prune entries created before the given UTC cutoff.
        /// </summary>
        /// <param name="olderThanUtc">Cutoff; entries created before this are removed.</param>
        /// <returns>Number of rows deleted.</returns>
        int Prune(DateTime olderThanUtc);
    }
}
