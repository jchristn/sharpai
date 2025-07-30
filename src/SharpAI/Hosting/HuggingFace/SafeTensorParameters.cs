namespace SharpAI.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents the breakdown of model parameters by data type.
    /// </summary>
    public class SafeTensorParameters
    {
        /// <summary>
        /// Gets or sets the number of 64-bit integer parameters (typically for embeddings or indices).
        /// </summary>
        [JsonPropertyName("I64")]
        public long? I64 { get; set; }

        /// <summary>
        /// Gets or sets the number of 32-bit floating-point parameters (the main model weights).
        /// </summary>
        [JsonPropertyName("F32")]
        public long? F32 { get; set; }

        /// <summary>
        /// Gets or sets the number of 16-bit floating-point parameters.
        /// </summary>
        [JsonPropertyName("F16")]
        public long? F16 { get; set; }

        /// <summary>
        /// Gets or sets the number of brain float 16-bit parameters.
        /// </summary>
        [JsonPropertyName("BF16")]
        public long? BF16 { get; set; }

        /// <summary>
        /// Gets or sets the number of 8-bit integer parameters.
        /// </summary>
        [JsonPropertyName("I8")]
        public long? I8 { get; set; }

        /// <summary>
        /// Gets or sets the number of unsigned 8-bit integer parameters.
        /// </summary>
        [JsonPropertyName("U8")]
        public long? U8 { get; set; }
    }
}
