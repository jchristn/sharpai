namespace SharpAI.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents transformers library specific information.
    /// </summary>
    public class TransformersInfo
    {
        /// <summary>
        /// Gets or sets the auto model class to use for loading (e.g., "AutoModel", "AutoModelForCausalLM").
        /// </summary>
        [JsonPropertyName("auto_model")]
        public string AutoModel { get; set; }

        /// <summary>
        /// Gets or sets the pipeline tag for transformers inference (e.g., "feature-extraction", "text-generation").
        /// </summary>
        [JsonPropertyName("pipeline_tag")]
        public string PipelineTag { get; set; }

        /// <summary>
        /// Gets or sets the processor/tokenizer class to use (e.g., "AutoTokenizer").
        /// </summary>
        [JsonPropertyName("processor")]
        public string Processor { get; set; }
    }
}
