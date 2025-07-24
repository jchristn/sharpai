namespace SharpAI.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Ollama list local models result.
    /// </summary>
    public class ListLocalModelsResult
    {
        #region Public-Members

        /// <summary>
        /// Models.
        /// </summary>
        [JsonPropertyName("models")]
        public List<ModelInfo> Models
        {
            get
            {
                return _Models;
            }
            set
            {
                if (value == null) value = new List<ModelInfo>();
                _Models = value;
            }
        }

        #endregion

        #region Private-Members

        private List<ModelInfo> _Models = new List<ModelInfo>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Ollama list local models result.
        /// </summary>
        public ListLocalModelsResult()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
