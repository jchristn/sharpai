namespace SharpAI.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Ollama generate embeddings request.
    /// </summary>
    public class GenerateEmbeddingsRequest
    {
        #region Public-Members

        /// <summary>
        /// Model.
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
        /// Input object, either a single string or an array of strings.
        /// </summary>
        [JsonPropertyName("input")]
        public object Input
        {
            get
            {
                return _Input;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Input));
                _Input = value;
            }
        }

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
                _Options = value ?? new GenerationOptions();
            }
        }

        /// <summary>
        /// Controls how long the model will stay loaded into memory following the request.
        /// </summary>
        [JsonPropertyName("keep_alive")]
        public string KeepAlive
        {
            get
            {
                return _KeepAlive;
            }
            set
            {
                _KeepAlive = value;
            }
        }

        /// <summary>
        /// Indicates whether the input is a singleton string.
        /// </summary>
        public bool IsInputSingleton
        {
            get
            {
                if (_Input is string)
                    return true;

                if (_Input is JsonElement element)
                {
                    return element.ValueKind == JsonValueKind.String;
                }

                return false;
            }
        }

        /// <summary>
        /// Indicates whether the input is an array of strings.
        /// </summary>
        public bool IsInputArray
        {
            get
            {
                if (_Input is string[] || _Input is List<string>)
                    return true;

                if (_Input is JsonElement element)
                {
                    return element.ValueKind == JsonValueKind.Array;
                }

                return false;
            }
        }

        #endregion

        #region Private-Members

        private string _Model = null;
        private object _Input = null;
        private GenerationOptions _Options = new GenerationOptions();
        private string _KeepAlive = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Ollama generate embeddings request.
        /// </summary>
        public GenerateEmbeddingsRequest()
        {
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Get the input as a single string. Throws exception if input is not a singleton.
        /// </summary>
        /// <returns>Single input string.</returns>
        public string GetInputAsSingleton()
        {
            if (!IsInputSingleton) throw new InvalidOperationException("Input is not a singleton string");

            if (_Input is string str)
                return str;

            if (_Input is JsonElement element && element.ValueKind == JsonValueKind.String)
                return element.GetString();

            throw new InvalidOperationException("Input is not a singleton string");
        }

        /// <summary>
        /// Get the input as an array of strings. Handles both array and singleton inputs.
        /// </summary>
        /// <returns>Array of input strings.</returns>
        public string[] GetInputAsArray()
        {
            if (IsInputSingleton)
            {
                return new string[] { GetInputAsSingleton() };
            }
            else if (_Input is string[] stringArray)
            {
                return stringArray;
            }
            else if (_Input is List<string> stringList)
            {
                return stringList.ToArray();
            }
            else if (_Input is JsonElement element && element.ValueKind == JsonValueKind.Array)
            {
                var list = new List<string>();
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        list.Add(item.GetString());
                    }
                }
                return list.ToArray();
            }
            else
            {
                throw new InvalidOperationException("Input is not in a recognized format");
            }
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}