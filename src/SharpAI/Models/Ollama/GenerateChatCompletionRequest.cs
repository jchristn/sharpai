namespace SharpAI.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Ollama generate chat completion request.
    /// </summary>
    public class GenerateChatCompletionRequest
    {
        #region Public-Members

        /// <summary>
        /// Model.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null;

        /// <summary>
        /// Messages.
        /// </summary>
        [JsonPropertyName("messages")]
        public List<Message> Messages
        {
            get
            {
                return _Messages;
            }
            set
            {
                if (value == null) value = new List<Message>();
                _Messages = value;
            }
        }

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

        private List<Message> _Messages = new List<Message>();
        private GenerationOptions _Options = new GenerationOptions();

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
