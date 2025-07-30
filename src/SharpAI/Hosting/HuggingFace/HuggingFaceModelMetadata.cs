namespace SharpAI.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// HuggingFace model metadata.
    /// </summary>
    public class HuggingFaceModelMetadata
    {
        #region Public-Members

        /// <summary>
        /// Gets or sets the internal database ID of the model.
        /// </summary>
        [JsonPropertyName("_id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the full model ID in the format "organization/model-name".
        /// </summary>
        [JsonPropertyName("id")]
        public string ModelId { get; set; }

        /// <summary>
        /// Gets or sets the model identifier (same as id field).
        /// </summary>
        [JsonPropertyName("modelId")]
        public string ModelIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the author or organization that created the model.
        /// </summary>
        [JsonPropertyName("author")]
        public string Author { get; set; }

        /// <summary>
        /// Gets or sets the Git SHA hash of the current model version.
        /// </summary>
        [JsonPropertyName("sha")]
        public string Sha { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of when the model was created.
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of when the model was last modified.
        /// </summary>
        [JsonPropertyName("lastModified")]
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model is private.
        /// </summary>
        [JsonPropertyName("private")]
        public bool Private { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model requires authentication to access.
        /// </summary>
        [JsonPropertyName("gated")]
        public bool? Gated { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model is disabled.
        /// </summary>
        [JsonPropertyName("disabled")]
        public bool? Disabled { get; set; }

        /// <summary>
        /// Gets or sets the pipeline tag indicating the model's primary task (e.g., "sentence-similarity", "text-generation").
        /// </summary>
        [JsonPropertyName("pipeline_tag")]
        public string PipelineTag { get; set; }

        /// <summary>
        /// Gets or sets the list of tags associated with the model, including frameworks, tasks, languages, and datasets.
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }

        /// <summary>
        /// Gets or sets the total number of downloads for the model.
        /// </summary>
        [JsonPropertyName("downloads")]
        public long? Downloads { get; set; }

        /// <summary>
        /// Gets or sets the total downloads across all time.
        /// </summary>
        [JsonPropertyName("downloadsAllTime")]
        public long? DownloadsAllTime { get; set; }

        /// <summary>
        /// Gets or sets the number of likes/stars the model has received.
        /// </summary>
        [JsonPropertyName("likes")]
        public int? Likes { get; set; }

        /// <summary>
        /// Gets or sets the trending score of the model.
        /// </summary>
        [JsonPropertyName("trendingScore")]
        public double? TrendingScore { get; set; }

        /// <summary>
        /// Gets or sets the primary library used for the model (e.g., "sentence-transformers", "transformers").
        /// </summary>
        [JsonPropertyName("library_name")]
        public string LibraryName { get; set; }

        /// <summary>
        /// Gets or sets the mask token used by the model's tokenizer (e.g., "[MASK]" for BERT models).
        /// </summary>
        [JsonPropertyName("mask_token")]
        public string MaskToken { get; set; }

        /// <summary>
        /// Gets or sets the widget data used for the model's interactive demo examples.
        /// </summary>
        [JsonPropertyName("widgetData")]
        public List<WidgetData> WidgetData { get; set; }

        /// <summary>
        /// Gets or sets the model index information (typically null).
        /// </summary>
        [JsonPropertyName("model-index")]
        public object ModelIndex { get; set; }

        /// <summary>
        /// Gets or sets the model configuration including architecture and tokenizer settings.
        /// </summary>
        [JsonPropertyName("config")]
        public ModelConfig Config { get; set; }

        /// <summary>
        /// Gets or sets the model card data containing metadata like license, language, and datasets.
        /// </summary>
        [JsonPropertyName("cardData")]
        public CardData CardData { get; set; }

        /// <summary>
        /// Gets or sets the transformers library specific information for loading the model.
        /// </summary>
        [JsonPropertyName("transformersInfo")]
        public TransformersInfo TransformersInfo { get; set; }

        /// <summary>
        /// Gets or sets the SafeTensors format information including parameter counts by data type.
        /// </summary>
        [JsonPropertyName("safetensors")]
        public SafeTensors SafeTensors { get; set; }

        /// <summary>
        /// Gets or sets the GGUF format information for quantized models.
        /// </summary>
        [JsonPropertyName("gguf")]
        public GgufInfo Gguf { get; set; }

        /// <summary>
        /// Gets or sets the list of files (siblings) in the model repository.
        /// </summary>
        [JsonPropertyName("siblings")]
        public List<Sibling> Siblings { get; set; }

        /// <summary>
        /// Gets or sets the list of Hugging Face Spaces that use this model.
        /// </summary>
        [JsonPropertyName("spaces")]
        public List<string> Spaces { get; set; }

        /// <summary>
        /// Gets or sets the inference status (e.g., "warm" indicating the model is loaded).
        /// </summary>
        [JsonPropertyName("inference")]
        public string Inference { get; set; }

        /// <summary>
        /// Gets or sets the total storage used by the model in bytes.
        /// </summary>
        [JsonPropertyName("usedStorage")]
        public long? UsedStorage { get; set; }

        /// <summary>
        /// Gets or sets the gated request configuration for models with access control.
        /// </summary>
        [JsonPropertyName("gatedRequest")]
        public GatedRequest GatedRequest { get; set; }

        /// <summary>
        /// Gets or sets the papers with code ID if the model is linked to a paper.
        /// </summary>
        [JsonPropertyName("paperswithcode_id")]
        public string PapersWithCodeId { get; set; }

        /// <summary>
        /// Gets or sets the description of the model.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the citation information for the model.
        /// </summary>
        [JsonPropertyName("citation")]
        public string Citation { get; set; }

        /// <summary>
        /// Gets or sets the resource group associated with the model.
        /// </summary>
        [JsonPropertyName("resourceGroup")]
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Gets or sets whether XET is enabled for the model.
        /// </summary>
        [JsonPropertyName("xetEnabled")]
        public bool? XetEnabled { get; set; }

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the HuggingFaceModelMetadata class.
        /// </summary>
        public HuggingFaceModelMetadata()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion

    }
}
