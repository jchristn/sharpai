namespace SharpAI.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Model info details object for nested details in Ollama API response.
    /// </summary>
    public class ModelInfoDetails
    {
        #region Public-Members

        /// <summary>
        /// Parent model name.
        /// </summary>
        [JsonPropertyName("parent_model")]
        public string ParentModel
        {
            get
            {
                return _ParentModel;
            }
            set
            {
                _ParentModel = value ?? string.Empty;
            }
        }

        /// <summary>
        /// Model format.
        /// </summary>
        [JsonPropertyName("format")]
        public string Format
        {
            get
            {
                return _Format;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Format));
                _Format = value;
            }
        }

        /// <summary>
        /// Model family.
        /// </summary>
        [JsonPropertyName("family")]
        public string Family
        {
            get
            {
                return _Family;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Family));
                _Family = value;
            }
        }

        /// <summary>
        /// Model families.
        /// </summary>
        [JsonPropertyName("families")]
        public List<string> Families
        {
            get
            {
                return _Families;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Families));
                _Families = value;
            }
        }

        /// <summary>
        /// Parameter size.
        /// </summary>
        [JsonPropertyName("parameter_size")]
        public string ParameterSize
        {
            get
            {
                return _ParameterSize;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(ParameterSize));
                _ParameterSize = value;
            }
        }

        /// <summary>
        /// Quantization level.
        /// </summary>
        [JsonPropertyName("quantization_level")]
        public string QuantizationLevel
        {
            get
            {
                return _QuantizationLevel;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(QuantizationLevel));
                _QuantizationLevel = value;
            }
        }

        #endregion

        #region Private-Members

        private string _ParentModel = string.Empty;
        private string _Format = "gguf";
        private string _Family = "llama";
        private List<string> _Families = new List<string>();
        private string _ParameterSize = string.Empty;
        private string _QuantizationLevel = string.Empty;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Model info details object for nested details in Ollama API response.
        /// </summary>
        public ModelInfoDetails()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}