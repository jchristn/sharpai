namespace SharpAI.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents example data for the model's interactive widget.
    /// </summary>
    public class WidgetData
    {
        /// <summary>
        /// Gets or sets the source sentence for comparison in similarity tasks.
        /// </summary>
        [JsonPropertyName("source_sentence")]
        public string SourceSentence { get; set; }

        /// <summary>
        /// Gets or sets the list of sentences to compare against the source for similarity scoring.
        /// </summary>
        [JsonPropertyName("sentences")]
        public List<string> Sentences { get; set; }

        /// <summary>
        /// Gets or sets the text input for generation tasks.
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the context for question-answering tasks.
        /// </summary>
        [JsonPropertyName("context")]
        public string Context { get; set; }

        /// <summary>
        /// Gets or sets the question for question-answering tasks.
        /// </summary>
        [JsonPropertyName("question")]
        public string Question { get; set; }

        /// <summary>
        /// Gets or sets example inputs for the widget.
        /// </summary>
        [JsonPropertyName("example_inputs")]
        public object ExampleInputs { get; set; }
    }
}
