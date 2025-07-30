namespace SharpAI.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Model info for Ollama API response.
    /// </summary>
    public class ModelInfo
    {
        #region Public-Members

        /// <summary>
        /// Model name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Name));
                _Name = value;
            }
        }

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
        /// Timestamp when the model was last modified.
        /// </summary>
        [JsonPropertyName("modified_at")]
        public string ModifiedAt
        {
            get
            {
                return _ModifiedAt;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(ModifiedAt));
                _ModifiedAt = value;
            }
        }

        /// <summary>
        /// Size of the model in bytes.
        /// </summary>
        [JsonPropertyName("size")]
        public long Size
        {
            get
            {
                return _Size;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(Size));
                _Size = value;
            }
        }

        /// <summary>
        /// SHA256 digest of the model.
        /// </summary>
        [JsonPropertyName("digest")]
        public string Digest
        {
            get
            {
                return _Digest;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Digest));
                _Digest = value;
            }
        }

        /// <summary>
        /// Model info details object.
        /// </summary>
        [JsonPropertyName("details")]
        public ModelInfoDetails Details
        {
            get
            {
                return _Details;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Details));
                _Details = value;
            }
        }

        #endregion

        #region Private-Members

        private string _Name = string.Empty;
        private string _Model = string.Empty;
        private string _ModifiedAt = string.Empty;
        private long _Size = 0;
        private string _Digest = string.Empty;
        private ModelInfoDetails _Details = new ModelInfoDetails();
        private static string _TimestampFormat = "yyyy-MM-ddTHH:mm:sszzz";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Model info for Ollama API response.
        /// </summary>
        public ModelInfo()
        {
        }

        /// <summary>
        /// Convert from ModelFile to ModelInfo.
        /// </summary>
        /// <param name="mf">ModelFile instance.</param>
        /// <returns>ModelInfo instance.</returns>
        public static ModelInfo FromModelFile(ModelFile mf)
        {
            if (mf == null) throw new ArgumentNullException(nameof(mf));

            return new ModelInfo
            {
                Name = mf.Name,
                Model = mf.Name,
                ModifiedAt = mf.ModelCreationUtc != null ? mf.ModelCreationUtc.Value.ToString(_TimestampFormat) : DateTime.UtcNow.ToString(_TimestampFormat),
                Size = mf.ContentLength,
                Digest = mf.SHA256Hash ?? string.Empty,
                Details = new ModelInfoDetails
                {
                    ParentModel = mf.ParentModel ?? string.Empty,
                    Format = mf.Format ?? "gguf",
                    Family = mf.Family ?? "llama",
                    Families = new List<string> { mf.Family ?? "llama" },
                    ParameterSize = mf.ParameterCount.ToString(),
                    QuantizationLevel = mf.Quantization ?? string.Empty
                }
            };
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}