namespace SharpAI.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Base class for AI providers that support embeddings and text generation.
    /// </summary>
    public abstract class EngineBase : IDisposable
    {
        /// <summary>
        /// Path to the model file (ONNX, GGUF, etc.).
        /// </summary>
        public string ModelPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets the number of GPU layers to use for inference.
        /// </summary>
        /// <returns>
        /// 0 = CPU only (no GPU layers)
        /// -1 = Use all available GPU layers  
        /// >0 = Use specified number of GPU layers
        /// </returns>
        public abstract int GetOptimalGpuLayers();

        /// <summary>
        /// Gets the dimensionality of embeddings produced by this model.
        /// </summary>
        /// <returns>Number of dimensions in each embedding vector.</returns>
        public abstract int EmbeddingDimensions { get; }

        /// <summary>
        /// Gets whether this provider supports GPU acceleration.
        /// </summary>
        public abstract bool SupportsGpu { get; }

        /// <summary>
        /// Gets whether this provider supports embeddings.
        /// </summary>
        public abstract bool SupportsEmbeddings { get; }

        /// <summary>
        /// Gets whether this provider supports chat/text generation.
        /// </summary>
        public abstract bool SupportsGeneration { get; }

        #region Embeddings

        /// <summary>
        /// Generates embeddings for a single text input.
        /// </summary>
        /// <param name="text">Input text to embed.</param>
        /// <returns>Embedding vector as a float array.</returns>
        public abstract Task<float[]> GenerateEmbeddingsAsync(string text);

        /// <summary>
        /// Generates embeddings for multiple text inputs.
        /// </summary>
        /// <param name="texts">Array of input texts to embed.</param>
        /// <returns>Array of embedding vectors, one per input text.</returns>
        public abstract Task<float[][]> GenerateEmbeddingsAsync(string[] texts);

        #endregion

        #region Text Generation

        /// <summary>
        /// Generates a text completion for the given prompt.
        /// </summary>
        /// <param name="prompt">Input prompt.</param>
        /// <param name="maxTokens">Maximum number of tokens to generate.</param>
        /// <param name="temperature">Sampling temperature (0.0 to 2.0).</param>
        /// <param name="stopSequences">Sequences that will stop generation.</param>
        /// <returns>Generated text.</returns>
        public abstract Task<string> GenerateTextAsync(string prompt, int maxTokens = 512, float temperature = 0.7f, string[] stopSequences = null);

        /// <summary>
        /// Generates a streaming text completion for the given prompt.
        /// </summary>
        /// <param name="prompt">Input prompt.</param>
        /// <param name="maxTokens">Maximum number of tokens to generate.</param>
        /// <param name="temperature">Sampling temperature (0.0 to 2.0).</param>
        /// <param name="stopSequences">Sequences that will stop generation.</param>
        /// <returns>Async enumerable of generated text tokens.</returns>
        public abstract IAsyncEnumerable<string> GenerateTextStreamAsync(string prompt, int maxTokens = 512, float temperature = 0.7f, string[] stopSequences = null);

        #endregion

        #region Chat

        /// <summary>
        /// Generates a chat completion given a conversation history.
        /// </summary>
        /// <param name="messages">Conversation history.</param>
        /// <param name="maxTokens">Maximum number of tokens to generate.</param>
        /// <param name="temperature">Sampling temperature (0.0 to 2.0).</param>
        /// <param name="stopSequences">Sequences that will stop generation.</param>
        /// <returns>Generated chat response.</returns>
        public abstract Task<string> GenerateChatCompletionAsync(ChatMessage[] messages, int maxTokens = 512, float temperature = 0.7f, string[]stopSequences = null);

        /// <summary>
        /// Generates a streaming chat completion given a conversation history.
        /// </summary>
        /// <param name="messages">Conversation history.</param>
        /// <param name="maxTokens">Maximum number of tokens to generate.</param>
        /// <param name="temperature">Sampling temperature (0.0 to 2.0).</param>
        /// <param name="stopSequences">Sequences that will stop generation.</param>
        /// <returns>Async enumerable of generated chat response tokens.</returns>
        public abstract IAsyncEnumerable<string> GenerateChatCompletionStreamAsync(ChatMessage[] messages, int maxTokens = 512, float temperature = 0.7f, string[] stopSequences = null);

        #endregion

        #region Lifecycle

        /// <summary>
        /// Initializes the provider with the specified model.
        /// </summary>
        /// <param name="modelPath">Path to the model file.</param>
        /// <returns>Task representing the initialization.</returns>
        public abstract Task InitializeAsync(string modelPath);

        /// <summary>
        /// Gets whether the provider is initialized and ready for use.
        /// </summary>
        public abstract bool IsInitialized { get; }

        /// <summary>
        /// Disposes of the provider and releases resources.
        /// </summary>
        public abstract void Dispose();

        #endregion
    }
}
