namespace SharpAI.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI generate embeddings request.
    /// </summary>
    public class OpenAIGenerateEmbeddingsRequest
    {
        /// <summary>
        /// ID of the model to use (required).
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null;

        /// <summary>
        /// Input text to embed (required).
        /// Can be a string or an array of strings.
        /// </summary>
        [JsonPropertyName("input")]
        [JsonConverter(typeof(OpenAIEmbeddingsInputConverter))]
        public object Input
        {
            get => _Input;
            set => _Input = value;
        }

        /// <summary>
        /// The format to return the embeddings in.
        /// Can be either "float" or "base64".
        /// </summary>
        [JsonPropertyName("encoding_format")]
        public string EncodingFormat { get; set; } = null;

        /// <summary>
        /// The number of dimensions the resulting output embeddings should have.
        /// Only supported in some models.
        /// </summary>
        [JsonPropertyName("dimensions")]
        public int? Dimensions
        {
            get => _Dimensions;
            set
            {
                if (value.HasValue && value.Value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(Dimensions), "Dimensions must be greater than 0");
                _Dimensions = value;
            }
        }

        /// <summary>
        /// A unique identifier representing your end-user.
        /// </summary>
        [JsonPropertyName("user")]
        public string User { get; set; } = null;

        private object _Input;
        private int? _Dimensions;

        /// <summary>
        /// Gets the input as a single string.
        /// Throws an exception if the input is a list.
        /// </summary>
        /// <returns>The input string.</returns>
        /// <exception cref="InvalidOperationException">Thrown when input is a list instead of a single string.</exception>
        public string GetInput()
        {
            if (_Input == null)
                return null;

            if (_Input is string singleInput)
                return singleInput;

            if (_Input is List<string> || _Input is string[])
                throw new InvalidOperationException("Input is a list. Use GetInputs() to retrieve multiple input strings.");

            throw new InvalidOperationException($"Input is of unexpected type: {_Input.GetType()}");
        }

        /// <summary>
        /// Gets the input as an array of strings.
        /// If input is a single string, returns an array with one element.
        /// </summary>
        /// <returns>Array of input strings.</returns>
        public string[] GetInputs()
        {
            if (_Input == null)
                return null;

            if (_Input is string singleInput)
                return new string[] { singleInput };

            if (_Input is string[] arrayInputs)
                return arrayInputs;

            if (_Input is List<string> listInputs)
                return listInputs.ToArray();

            throw new InvalidOperationException($"Input is of unexpected type: {_Input.GetType()}");
        }

        /// <summary>
        /// Sets multiple input strings from an array.
        /// </summary>
        /// <param name="inputs">The array of input strings.</param>
        public void SetInputs(string[] inputs)
        {
            if (inputs == null || inputs.Length == 0)
                throw new ArgumentException("Inputs cannot be null or empty", nameof(inputs));
            if (inputs.Any(string.IsNullOrEmpty))
                throw new ArgumentException("Input array cannot contain null or empty strings", nameof(inputs));
            _Input = inputs;
        }

        /// <summary>
        /// Checks if the input is a single string.
        /// </summary>
        /// <returns>True if input is a single string, false otherwise.</returns>
        public bool IsSingleInput()
        {
            return _Input is string;
        }

        /// <summary>
        /// OpenAI generate embeddings request.
        /// </summary>
        public OpenAIGenerateEmbeddingsRequest()
        {
        }
    }
}