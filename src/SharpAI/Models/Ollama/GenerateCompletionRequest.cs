namespace SharpAI.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Ollama generate completion request.
    /// </summary>
    public class GenerateCompletionRequest
    {
        #region Public-Members

        /// <summary>
        /// Model.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null;

        /// <summary>
        /// Prompt.
        /// </summary>
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = null;

        /// <summary>
        /// True to enable response streaming.
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        /// <summary>
        /// Generation options.
        /// </summary>
        [JsonPropertyName("options")]
        public GenerationOptions Options
        {
            get
            {
                return _Options;
            }
            set
            {
                if (value == null) value = new GenerationOptions();
                _Options = value;
            }
        }

        #endregion

        #region Private-Members

        private GenerationOptions _Options = new GenerationOptions();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Ollama generate completion request.
        /// </summary>
        public GenerateCompletionRequest()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
