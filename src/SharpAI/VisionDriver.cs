namespace SharpAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using SharpAI.Engines;
    using SharpAI.Serialization;
    using SyslogLogging;

    /// <summary>
    /// Vision driver to handle image-based prompts (base64 images) with supported engines.
    /// </summary>
    public class VisionDriver
    {
        #region Private-Members

        private string _Header = "[VisionDriver] ";
        private LoggingModule _Logging = null;
        private Serializer _Serializer = null;
        private ModelDriver _Models = null;
        private string _MultiModalProjectorPath = null;
        private static readonly Regex _DataUrlPrefix =
            new(@"^data:image\/[a-zA-Z0-9.+-]+;base64,", RegexOptions.Compiled);

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Vision driver.
        /// </summary>
        /// <param name="logging">Logging module.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="models">Model driver.</param>
        /// <param name="multiModalProjectorPath">Path to a LLaVA projector GGUF.</param>
        public VisionDriver(LoggingModule logging, Serializer serializer, ModelDriver models, string multiModalProjectorPath)
        {
            _Logging = logging ?? new LoggingModule();
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _Models = models ?? throw new ArgumentNullException(nameof(models));
            _MultiModalProjectorPath = multiModalProjectorPath;
            _Logging.Debug(_Header + "initialized");
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Generate a vision-enabled completion by providing one or more images in base64 format
        /// </summary>
        /// <param name="model">The model identifier to use.</param>
        /// <param name="imagesBase64">A collection of base64-encoded images. At least one must be valid.</param>
        /// <param name="prompt">Optional accompanying user prompt.</param>
        /// <param name="maxTokens">Maximum number of tokens to generate 1024 (minimum is 100).</param>
        /// <param name="temperature">Sampling temperature (0.0�1.0).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The generated text response from the model.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if maxTokens &lt; 100 or temperature is outside [0.0,1.0].</exception>
        /// <exception cref="ArgumentException">Thrown if no valid images are provided.</exception>
        /// <exception cref="ArgumentNullException">Thrown if imagesBase64 is null.</exception>
        public async Task<string> GenerateCompletion(
            string model,
            IEnumerable<string> imagesBase64,
            string prompt = "",
            int maxTokens = 512,
            float temperature = 0.1f,
            CancellationToken token = default)
        {
            if (maxTokens < 100) throw new ArgumentOutOfRangeException(nameof(maxTokens));
            if (temperature < 0.0f || temperature > 1.0f) throw new ArgumentOutOfRangeException(nameof(temperature));

            List<byte[]> bytes = new List<byte[]>();
            foreach (string b64 in imagesBase64 ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(b64)) continue;
                bytes.Add(DecodeBase64ToBytes(b64));
            }
            LlamaSharpEngine engine = GetModelEngine(model);
            engine.ConfigureVision(_MultiModalProjectorPath);
            return await engine.GenerateVisionCompletionAsync(
                bytes, prompt ?? string.Empty, maxTokens, temperature, token
            ).ConfigureAwait(false);
        }

        /// <summary>
        /// Generate a streaming vision-enabled completion by providing one or more images in base64 format.
        /// This method provides real-time token streaming for vision model responses.
        /// </summary>
        /// <param name="model">The model identifier to use.</param>
        /// <param name="imagesBase64">A collection of base64-encoded images. At least one must be valid.</param>
        /// <param name="prompt">Optional accompanying user prompt.</param>
        /// <param name="maxTokens">Maximum number of tokens to generate (minimum is 100).</param>
        /// <param name="temperature">Sampling temperature (0.0�1.0).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>An async enumerable of text chunks as they are generated.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if maxTokens &lt; 100 or temperature is outside [0.0,1.0].</exception>
        /// <exception cref="ArgumentException">Thrown if no valid images are provided.</exception>
        /// <exception cref="ArgumentNullException">Thrown if imagesBase64 is null.</exception>
        public async IAsyncEnumerable<string> GenerateCompletionStream(
            string model,
            IEnumerable<string> imagesBase64,
            string prompt = "",
            int maxTokens = 512,
            float temperature = 0.1f,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (maxTokens < 100) throw new ArgumentOutOfRangeException(nameof(maxTokens));
            if (temperature < 0.0f || temperature > 1.0f) throw new ArgumentOutOfRangeException(nameof(temperature));

            List<byte[]> bytes = new List<byte[]>();
            foreach (string b64 in imagesBase64 ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(b64)) continue;

                try
                {
                    bytes.Add(DecodeBase64ToBytes(b64));
                }
                catch (Exception ex)
                {
                    _Logging?.Warn(_Header + $"failed to decode base64 image: {ex.Message}");
                }
            }
            LlamaSharpEngine engine = GetModelEngine(model);
            engine.ConfigureVision(_MultiModalProjectorPath);
            await foreach (string chunk in engine.GenerateVisionCompletionStreamAsync(
                bytes,
                prompt ?? string.Empty,
                maxTokens,
                temperature,
                token).ConfigureAwait(false))
            {
                yield return chunk;
            }
        }

        #endregion

        #region Private-Methods

        private LlamaSharpEngine GetModelEngine(string model) => _Models.GetEngine(model);

        private static byte[] DecodeBase64ToBytes(string input)
        {
            string cleaned = _DataUrlPrefix.Replace(input?.Trim() ?? "", string.Empty);
            try
            {
                return Convert.FromBase64String(cleaned);
            }
            catch
            {
                int mod = cleaned.Length % 4;
                if (mod != 0) cleaned = cleaned + new string('=', 4 - mod);
                return Convert.FromBase64String(cleaned);
            }
        }

        #endregion
    }
}
