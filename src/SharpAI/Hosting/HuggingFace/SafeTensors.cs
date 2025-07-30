namespace SharpAI.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents SafeTensors format information including parameter counts.
    /// </summary>
    public class SafeTensors
    {
        /// <summary>
        /// Gets or sets the parameter counts broken down by data type.
        /// </summary>
        [JsonPropertyName("parameters")]
        public SafeTensorParameters Parameters { get; set; }

        /// <summary>
        /// Gets or sets the total number of parameters across all data types.
        /// </summary>
        [JsonPropertyName("total")]
        public long Total { get; set; }
    }
}
