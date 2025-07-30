namespace SharpAI.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// GGUF info.
    /// </summary>
    public class GgufInfo
    {
        /// <summary>
        /// Gets or sets the total size of the model in bytes.
        /// </summary>
        [JsonPropertyName("total")]
        public long Total { get; set; }

        /// <summary>
        /// Gets or sets the model architecture (e.g., "bert", "llama", "gpt2").
        /// </summary>
        [JsonPropertyName("architecture")]
        public string Architecture { get; set; }

        /// <summary>
        /// Gets or sets the maximum context length in tokens that the model can process.
        /// </summary>
        [JsonPropertyName("context_length")]
        public int ContextLength { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model is causal (autoregressive) or not.
        /// </summary>
        [JsonPropertyName("causal")]
        public bool Causal { get; set; }

        /// <summary>
        /// Gets or sets the beginning-of-sequence token (e.g., "[CLS]" for BERT).
        /// </summary>
        [JsonPropertyName("bos_token")]
        public string BosToken { get; set; }

        /// <summary>
        /// Gets or sets the end-of-sequence token (e.g., "[SEP]" for BERT).
        /// </summary>
        [JsonPropertyName("eos_token")]
        public string EosToken { get; set; }
    }
}
