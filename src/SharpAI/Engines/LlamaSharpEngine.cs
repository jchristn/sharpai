﻿namespace SharpAI.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LLama.Common;
    using LLama;
    using LLama.Abstractions;
    using LLama.Sampling;
    using SyslogLogging;

    /// <summary>
    /// LlamaSharp implementation of the AI provider base class.
    /// Provides text generation, embeddings, and chat completion capabilities using the LlamaSharp library.
    /// </summary>
    public class LlamaSharpEngine : EngineBase
    {
        #region Public-Members

        /// <summary>
        /// Gets the number of dimensions in the embedding vectors generated by this engine.
        /// Returns -1 if not yet determined, 0 if embeddings are not supported.
        /// </summary>
        public override int EmbeddingDimensions
        {
            get
            {
                if (_EmbeddingDimensions == -1 && _Embedder != null)
                {
                    // Get embedding dimensions by testing with a small input
                    try
                    {
                        var testEmbeddings = _Embedder.GetEmbeddings("test").Result;
                        var testEmbedding = testEmbeddings.Single();
                        _EmbeddingDimensions = testEmbedding.Length;
                    }
                    catch
                    {
                        _EmbeddingDimensions = 0;
                    }
                }
                return _EmbeddingDimensions;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this engine supports GPU acceleration.
        /// LlamaSharp supports GPU acceleration when CUDA is available.
        /// </summary>
        public override bool SupportsGpu => true;

        /// <summary>
        /// Gets a value indicating whether this engine supports generating embeddings.
        /// Support depends on whether the embedder was successfully initialized.
        /// </summary>
        public override bool SupportsEmbeddings => _Embedder != null;

        /// <summary>
        /// Gets a value indicating whether this engine supports text generation.
        /// LlamaSharp always supports text generation.
        /// </summary>
        public override bool SupportsGeneration => true;

        /// <summary>
        /// Gets a value indicating whether this engine has been successfully initialized.
        /// </summary>
        public override bool IsInitialized => _IsInitialized;

        #endregion

        #region Private-Members

        private string _Header = "[LlamaSharpEngine] ";
        private LoggingModule _Logging = null;

        private LLamaWeights _Model = null;
        private LLamaContext _Context = null;
        private LLamaEmbedder _Embedder = null;
        private InteractiveExecutor _Executor = null;
        private StatelessExecutor _StatelessExecutor = null;
        private ChatSession _ChatSession = null;
        private bool _IsInitialized = false;
        private bool _Disposed = false;
        private int _EmbeddingDimensions = -1;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the LlamaSharpEngine class.
        /// </summary>
        /// <param name="logging">Optional logging module for capturing debug and error information. If null, a new LoggingModule will be created.</param>
        public LlamaSharpEngine(LoggingModule logging = null)
        {
            _Logging = logging ?? new LoggingModule();

            _Logging.Debug(_Header + "initialized");
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Releases all resources used by the LlamaSharpEngine.
        /// Properly disposes of the model, context, and embedder instances.
        /// </summary>
        public override void Dispose()
        {
            if (_Disposed) return;

            try
            {
                // these do not implement IDisposable
                _ChatSession = null;
                _Executor = null;
                _StatelessExecutor = null;

                _Embedder?.Dispose();
                _Context?.Dispose();
                _Model?.Dispose();
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "disposal exception:" + Environment.NewLine + ex.ToString());
            }
            finally
            {
                _Disposed = true;
                _IsInitialized = false;
            }
        }

        #region Initialization

        /// <summary>
        /// Initializes the LlamaSharp engine with the specified model file.
        /// Loads the model, creates context, and sets up executors for text generation and embeddings.
        /// </summary>
        /// <param name="modelPath">The file path to the GGUF model file to load.</param>
        /// <returns>A task that represents the asynchronous initialization operation.</returns>
        /// <exception cref="Exception">Thrown when the model fails to load or initialize.</exception>
        public override async Task InitializeAsync(string modelPath)
        {
            if (_IsInitialized) return;

            ModelPath = modelPath;

            await Task.Run(() =>
            {
                try
                {
                    var gpuLayers = GetOptimalGpuLayers();
                    _Logging.Debug(_Header + $"initializing LlamaSharp with {(gpuLayers > 0 ? "GPU" : "CPU")} acceleration{(gpuLayers > 0 ? $" ({gpuLayers} layers)" : "")}");

                    // FIX: hard-coded params
                    ModelParams parameters = new ModelParams(modelPath)
                    {
                        ContextSize = 2048,
                        GpuLayerCount = gpuLayers,
                        Embeddings = false  // Disable embeddings to avoid conflicts
                    };

                    _Model = LLamaWeights.LoadFromFile(parameters);
                    _Context = _Model.CreateContext(parameters);

                    _Executor = new InteractiveExecutor(_Context); // text generation
                    _StatelessExecutor = new StatelessExecutor(_Model, parameters); // text generation
                    _ChatSession = new ChatSession(_Executor);

                    // For embeddings, try to create a separate instance
                    try
                    {
                        ModelParams embeddingParams = new ModelParams(modelPath)
                        {
                            // FIX: hard-coded params
                            ContextSize = 2048,
                            GpuLayerCount = gpuLayers,
                            Embeddings = true
                        };

                        LLamaWeights embeddingModel = LLamaWeights.LoadFromFile(embeddingParams);
                        _Embedder = new LLamaEmbedder(embeddingModel, embeddingParams);
                    }
                    catch (Exception ex)
                    {
                        _Logging.Warn(_Header + "failed to initialize embeddings:" + Environment.NewLine + ex.ToString());
                        _Embedder = null;
                    }

                    _IsInitialized = true;

                    _Logging.Debug(_Header + "LlamaSharp provider initialized successfully");
                }
                catch (Exception ex)
                {
                    throw new Exception("failed to initialize LlamaSharp:" + Environment.NewLine + ex.ToString());
                }
            });
        }

        /// <summary>
        /// Determines the optimal number of GPU layers to use based on available hardware.
        /// </summary>
        /// <returns>The number of GPU layers to use, or 0 if GPU acceleration is not available. Returns -1 to use all available GPU layers.</returns>
        public override int GetOptimalGpuLayers()
        {
            try
            {
                var gpuDeviceCount = LLama.Native.NativeApi.llama_max_devices();

                if (gpuDeviceCount > 0)
                {
                    _Logging.Debug(_Header + $"CUDA detected, {gpuDeviceCount} GPU device(s) available");
                    return -1; // Use all available GPU layers
                }
                else
                {
                    _Logging.Debug(_Header + "no CUDA devices detected, using CPU");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                _Logging.Debug(_Header + "GPU detection exception, using CPU:" + Environment.NewLine + ex.ToString());
                return 0;
            }
        }

        #endregion

        #region Embeddings

        /// <inheritdoc />
        public override async Task<float[]> GenerateEmbeddingsAsync(
            string text, 
            CancellationToken token = default)
        {
            ThrowIfNotInitialized();

            if (_Embedder == null) throw new InvalidOperationException("Embeddings are not supported. The embedder failed to initialize.");

            IReadOnlyList<float[]> embeddings = await _Embedder.GetEmbeddings(text, token).ConfigureAwait(false);
            return embeddings.Single();
        }

        /// <inheritdoc />
        public override async Task<float[][]> GenerateEmbeddingsAsync(
            string[] texts,
            CancellationToken token = default)
        {
            ThrowIfNotInitialized();

            float[][] embeddings = new float[texts.Length][];

            for (int i = 0; i < texts.Length; i++)
            {
                embeddings[i] = await GenerateEmbeddingsAsync(texts[i], token).ConfigureAwait(false);
            }

            return embeddings;
        }

        #endregion

        #region Text Generation

        /// <inheritdoc />
        public override async Task<string> GenerateTextAsync(
            string prompt,
            int maxTokens = 512,
            float temperature = 0.7f,
            string[] stopSequences = null,
            CancellationToken token = default)
        {
            ThrowIfNotInitialized();

            try
            {
                InferenceParams inferenceParams = new InferenceParams
                {
                    MaxTokens = Math.Max(maxTokens, 100),
                    AntiPrompts = stopSequences?.ToList() ?? new List<string>(),
                    SamplingPipeline = new DefaultSamplingPipeline
                    {
                        Temperature = temperature
                    }
                };

                StringBuilder result = new StringBuilder();

                await foreach (string curr in _StatelessExecutor.InferAsync(prompt, inferenceParams, token).ConfigureAwait(false))
                {
                    result.Append(curr);
                }

                return result.ToString().Trim();
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "exception generating text:" + Environment.NewLine + ex.ToString());
                throw new Exception($"Failed to generate text:{Environment.NewLine}{ex.ToString()}", ex);
            }
        }

        /// <inheritdoc />
        public override async IAsyncEnumerable<string> GenerateTextStreamAsync(
            string prompt,
            int maxTokens = 512,
            float temperature = 0.7f,
            string[] stopSequences = null,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            ThrowIfNotInitialized();

            InferenceParams inferenceParams = new InferenceParams
            {
                MaxTokens = Math.Max(maxTokens, 100),
                AntiPrompts = stopSequences?.ToList() ?? new List<string>(),
                SamplingPipeline = new DefaultSamplingPipeline
                {
                    Temperature = temperature
                }
            };

            await foreach (var curr in _StatelessExecutor.InferAsync(prompt, inferenceParams, token).ConfigureAwait(false))
            {
                yield return curr;
            }
        }

        #endregion

        #region Chat

        /// <inheritdoc />
        public override async Task<string> GenerateChatCompletionAsync(
            string prompt, 
            int maxTokens = 512, 
            float temperature = 0.7f, 
            string[] stopSequences = null,
            CancellationToken token = default)
        {
            ThrowIfNotInitialized();

            try
            {
                InferenceParams inferenceParams = new InferenceParams
                {
                    MaxTokens = Math.Max(maxTokens, 100),
                    AntiPrompts = stopSequences?.ToList() ?? new List<string> { "user:", "User:", "human:", "Human:" }, // Default anti-prompt for chat
                    SamplingPipeline = new DefaultSamplingPipeline
                    {
                        Temperature = temperature
                    }
                };

                StringBuilder result = new StringBuilder();

                await foreach (var curr in _StatelessExecutor!.InferAsync(prompt, inferenceParams, token).ConfigureAwait(false))
                {
                    result.Append(curr);
                }

                return result.ToString().Trim();
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "exception generating chat completion:" + Environment.NewLine + ex.ToString());
                throw new Exception($"Failed to generate chat completion:{Environment.NewLine}{ex.ToString()}", ex);
            }
        }

        /// <inheritdoc />
        public override async IAsyncEnumerable<string> GenerateChatCompletionStreamAsync(
            string prompt,
            int maxTokens = 512, 
            float temperature = 0.7f, 
            string[] stopSequences = null,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            ThrowIfNotInitialized();

            InferenceParams inferenceParams = new InferenceParams
            {
                MaxTokens = Math.Max(maxTokens, 100),
                AntiPrompts = stopSequences?.ToList() ?? new List<string> { "user:", "User:", "human:", "Human:" }, // Default anti-prompt for chat
                SamplingPipeline = new DefaultSamplingPipeline
                {
                    Temperature = temperature
                }
            };

            await foreach (var curr in _StatelessExecutor!.InferAsync(prompt, inferenceParams, token).ConfigureAwait(false))
            {
                yield return curr;
            }
        }

        #endregion

        #endregion

        #region Private-Methods

        private void ThrowIfNotInitialized()
        {
            if (!_IsInitialized) throw new InvalidOperationException("Provider must be initialized before use. Call InitializeAsync() first.");
            if (_Disposed) throw new ObjectDisposedException(nameof(LlamaSharpEngine));
        }

        #endregion
    }
}