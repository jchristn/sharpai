namespace SharpAI.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents Large File Storage information for a file.
    /// </summary>
    public class LfsInfo
    {
        /// <summary>
        /// Gets or sets the SHA256 hash of the file.
        /// </summary>
        [JsonPropertyName("sha256")]
        public string Sha256 { get; set; }

        /// <summary>
        /// Gets or sets the size of the file in LFS.
        /// </summary>
        [JsonPropertyName("size")]
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the pointer size.
        /// </summary>
        [JsonPropertyName("pointer_size")]
        public int PointerSize { get; set; }
    }
}
