namespace SharpAI.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents the model card metadata including license, language, and datasets.
    /// </summary>
    public class CardData
    {
        /// <summary>
        /// Gets or sets the language of the model (e.g., "en" for English) or list of languages.
        /// </summary>
        [JsonPropertyName("language")]
        public object Language { get; set; }

        /// <summary>
        /// Gets or sets the license under which the model is distributed (e.g., "apache-2.0").
        /// </summary>
        [JsonPropertyName("license")]
        public string License { get; set; }

        /// <summary>
        /// Gets or sets the library name from the model card.
        /// </summary>
        [JsonPropertyName("library_name")]
        public string LibraryName { get; set; }

        /// <summary>
        /// Gets or sets the list of tags from the model card.
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }

        /// <summary>
        /// Gets or sets the list of datasets used to train the model.
        /// </summary>
        [JsonPropertyName("datasets")]
        public List<string> Datasets { get; set; }

        /// <summary>
        /// Gets or sets the pipeline tag from the model card.
        /// </summary>
        [JsonPropertyName("pipeline_tag")]
        public string PipelineTag { get; set; }

        /// <summary>
        /// Gets or sets the base model that this model was derived from.
        /// </summary>
        [JsonPropertyName("base_model")]
        public string BaseModel { get; set; }

        /// <summary>
        /// Gets or sets the name of the person or organization that created the model.
        /// </summary>
        [JsonPropertyName("model_creator")]
        public string ModelCreator { get; set; }

        /// <summary>
        /// Gets or sets the human-readable name of the model.
        /// </summary>
        [JsonPropertyName("model_name")]
        public string ModelName { get; set; }

        /// <summary>
        /// Gets or sets the name of the person or organization that quantized the model.
        /// </summary>
        [JsonPropertyName("quantized_by")]
        public string QuantizedBy { get; set; }

        /// <summary>
        /// Gets or sets whether inference is enabled for this model.
        /// </summary>
        [JsonPropertyName("inference")]
        public object Inference { get; set; }

        /// <summary>
        /// Gets or sets the model type from the card data.
        /// </summary>
        [JsonPropertyName("model_type")]
        public string ModelType { get; set; }

        /// <summary>
        /// Gets or sets the task categories for the model.
        /// </summary>
        [JsonPropertyName("task_categories")]
        public List<string> TaskCategories { get; set; }

        /// <summary>
        /// Gets or sets the task IDs for the model.
        /// </summary>
        [JsonPropertyName("task_ids")]
        public List<string> TaskIds { get; set; }
    }
}
