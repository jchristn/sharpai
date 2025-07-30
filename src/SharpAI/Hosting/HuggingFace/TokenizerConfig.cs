namespace SharpAI.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents the tokenizer configuration with special tokens.
    /// </summary>
    public class TokenizerConfig
    {
        /// <summary>
        /// Gets or sets the unknown token (typically "[UNK]").
        /// </summary>
        [JsonPropertyName("unk_token")]
        public string UnkToken { get; set; }

        /// <summary>
        /// Gets or sets the separator token (typically "[SEP]").
        /// </summary>
        [JsonPropertyName("sep_token")]
        public string SepToken { get; set; }

        /// <summary>
        /// Gets or sets the padding token (typically "[PAD]").
        /// </summary>
        [JsonPropertyName("pad_token")]
        public string PadToken { get; set; }

        /// <summary>
        /// Gets or sets the classification token (typically "[CLS]").
        /// </summary>
        [JsonPropertyName("cls_token")]
        public string ClsToken { get; set; }

        /// <summary>
        /// Gets or sets the mask token (typically "[MASK]").
        /// </summary>
        [JsonPropertyName("mask_token")]
        public string MaskToken { get; set; }

        /// <summary>
        /// Gets or sets the beginning of sequence token.
        /// </summary>
        [JsonPropertyName("bos_token")]
        public string BosToken { get; set; }

        /// <summary>
        /// Gets or sets the end of sequence token.
        /// </summary>
        [JsonPropertyName("eos_token")]
        public string EosToken { get; set; }

        /// <summary>
        /// Gets or sets the chat template for conversational models.
        /// </summary>
        [JsonPropertyName("chat_template")]
        public string ChatTemplate { get; set; }
    }
}
