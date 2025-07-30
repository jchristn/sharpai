namespace SharpAI.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents the model's configuration including architecture and tokenizer settings.
    /// </summary>
    public class ModelConfig
    {
        /// <summary>
        /// Gets or sets the list of model architectures (e.g., ["BertModel"]).
        /// </summary>
        [JsonPropertyName("architectures")]
        public List<string> Architectures { get; set; }

        /// <summary>
        /// Gets or sets the type of model architecture (e.g., "bert", "llama", "gpt2").
        /// </summary>
        [JsonPropertyName("model_type")]
        public string ModelType { get; set; }

        /// <summary>
        /// Gets or sets the tokenizer configuration with special tokens.
        /// </summary>
        [JsonPropertyName("tokenizer_config")]
        public TokenizerConfig TokenizerConfig { get; set; }

        /// <summary>
        /// Gets or sets additional chat templates for conversational models.
        /// </summary>
        [JsonPropertyName("additional_chat_templates")]
        public Dictionary<string, object> AdditionalChatTemplates { get; set; }

        /// <summary>
        /// Gets or sets the maximum position embeddings (context length).
        /// </summary>
        [JsonPropertyName("max_position_embeddings")]
        public int? MaxPositionEmbeddings { get; set; }

        /// <summary>
        /// Gets or sets the hidden size of the model.
        /// </summary>
        [JsonPropertyName("hidden_size")]
        public int? HiddenSize { get; set; }

        /// <summary>
        /// Gets or sets the number of attention heads.
        /// </summary>
        [JsonPropertyName("num_attention_heads")]
        public int? NumAttentionHeads { get; set; }

        /// <summary>
        /// Gets or sets the number of hidden layers.
        /// </summary>
        [JsonPropertyName("num_hidden_layers")]
        public int? NumHiddenLayers { get; set; }

        /// <summary>
        /// Gets or sets the vocabulary size.
        /// </summary>
        [JsonPropertyName("vocab_size")]
        public int? VocabSize { get; set; }
    }
}
