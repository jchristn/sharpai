namespace SharpAI.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Ollama generate embeddings result.
    /// </summary>
    public class GenerateEmbeddingsResult
    {
        #region Public-Members

        /// <summary>
        /// Model identifier.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model
        {
            get
            {
                return _Model;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Model));
                _Model = value;
            }
        }

        /// <summary>
        /// Embeddings.
        /// </summary>
        [JsonPropertyName("embeddings")]
        public float[][] Embeddings
        {
            get
            {
                return _Embeddings;
            }
            set
            {
                if (value == null) value = new float[0][];
                _Embeddings = value;
            }
        }

        #endregion

        #region Private-Members

        private string _Model = null;
        private float[][] _Embeddings = new float[0][];

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Ollama generate embeddings result.
        /// </summary>
        public GenerateEmbeddingsResult()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
