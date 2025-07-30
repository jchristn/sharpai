namespace SharpAI.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a file in the model repository.
    /// </summary>
    public class Sibling
    {
        /// <summary>
        /// Gets or sets the relative filename within the repository (e.g., "config.json", "model.safetensors").
        /// </summary>
        [JsonPropertyName("rfilename")]
        public string RFilename { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        [JsonPropertyName("size")]
        public long? Size { get; set; }

        /// <summary>
        /// Gets or sets the blob ID for the file.
        /// </summary>
        [JsonPropertyName("blobId")]
        public string BlobId { get; set; }

        /// <summary>
        /// Gets or sets the LFS (Large File Storage) information.
        /// </summary>
        [JsonPropertyName("lfs")]
        public LfsInfo Lfs { get; set; }
    }
}
