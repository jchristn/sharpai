namespace SharpAI.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Query parameters for paginated enumeration of the model registry. Supports page-based pagination,
    /// ordering, creation-time filtering, and model-specific equality filters. This is the sole entry point
    /// for listing models; there is no unbounded "get all" operation.
    /// </summary>
    public class EnumerationQuery
    {
        #region Public-Members

        /// <summary>
        /// Page number (1-based). Default 1; values below 1 are clamped to 1.
        /// </summary>
        public int PageNumber
        {
            get
            {
                return _PageNumber;
            }
            set
            {
                _PageNumber = value < 1 ? 1 : value;
            }
        }

        /// <summary>
        /// Number of results per page. Default 100, minimum 1, maximum 1000.
        /// </summary>
        public int PageSize
        {
            get
            {
                return _PageSize;
            }
            set
            {
                _PageSize = Math.Clamp(value, 1, 1000);
            }
        }

        /// <summary>
        /// Sort order. Default is CreatedDescending (newest first).
        /// </summary>
        public EnumerationOrderEnum Order { get; set; } = EnumerationOrderEnum.CreatedDescending;

        /// <summary>
        /// Filter to models created strictly after this UTC timestamp.
        /// </summary>
        public DateTime? CreatedAfter { get; set; } = null;

        /// <summary>
        /// Filter to models created strictly before this UTC timestamp.
        /// </summary>
        public DateTime? CreatedBefore { get; set; } = null;

        /// <summary>
        /// Filter by exact model name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Filter by model family.
        /// </summary>
        public string Family { get; set; } = null;

        /// <summary>
        /// Filter by quantization.
        /// </summary>
        public string Quantization { get; set; } = null;

        /// <summary>
        /// Filter by model format.
        /// </summary>
        public string Format { get; set; } = null;

        /// <summary>
        /// SQL OFFSET derived from the page number and page size.
        /// </summary>
        [JsonIgnore]
        public int Offset
        {
            get { return (PageNumber - 1) * PageSize; }
        }

        #endregion

        #region Private-Members

        private int _PageNumber = 1;
        private int _PageSize = 100;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate with defaults.
        /// </summary>
        public EnumerationQuery()
        {
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Apply query-string overrides. Any non-empty query-string value replaces the corresponding value.
        /// </summary>
        /// <param name="queryGetter">Function that retrieves a query-string value by key.</param>
        public void ApplyQuerystringOverrides(Func<string, string> queryGetter)
        {
            if (queryGetter == null) return;

            string value;

            value = queryGetter("pageNumber");
            if (!String.IsNullOrEmpty(value) && Int32.TryParse(value, out int pageNumber)) PageNumber = pageNumber;

            value = queryGetter("pageSize");
            if (!String.IsNullOrEmpty(value) && Int32.TryParse(value, out int pageSize)) PageSize = pageSize;

            value = queryGetter("order");
            if (!String.IsNullOrEmpty(value) && Enum.TryParse<EnumerationOrderEnum>(value, true, out EnumerationOrderEnum order)) Order = order;

            value = queryGetter("createdAfter");
            if (!String.IsNullOrEmpty(value) && DateTime.TryParse(value, out DateTime createdAfter)) CreatedAfter = createdAfter.ToUniversalTime();

            value = queryGetter("createdBefore");
            if (!String.IsNullOrEmpty(value) && DateTime.TryParse(value, out DateTime createdBefore)) CreatedBefore = createdBefore.ToUniversalTime();

            value = queryGetter("name");
            if (!String.IsNullOrEmpty(value)) Name = value;

            value = queryGetter("family");
            if (!String.IsNullOrEmpty(value)) Family = value;

            value = queryGetter("quantization");
            if (!String.IsNullOrEmpty(value)) Quantization = value;

            value = queryGetter("format");
            if (!String.IsNullOrEmpty(value)) Format = value;
        }

        #endregion
    }
}
