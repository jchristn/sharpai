namespace SharpAI.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Ollama show model information request.
    /// </summary>
    public class ShowModelInfoRequest
    {
        #region Public-Members

        /// <summary>
        /// Name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Ollama show model information request.
        /// </summary>
        public ShowModelInfoRequest()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
