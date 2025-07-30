﻿namespace SharpAI
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Reflection.PortableExecutable;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using SharpAI.Engines;
    using SharpAI.Serialization;
    using SyslogLogging;

    /// <summary>
    /// Chat driver.
    /// </summary>
    public class ChatDriver
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private string _Header = "[ChatDriver] ";
        private LoggingModule _Logging = null;
        private Serializer _Serializer = null;
        private ModelDriver _Models = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Chat driver.
        /// </summary>
        /// <param name="logging">Logging module.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="models">Model driver.</param>
        public ChatDriver(LoggingModule logging, Serializer serializer, ModelDriver models)
        {
            _Logging = logging ?? new LoggingModule();
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _Models = models ?? throw new ArgumentNullException(nameof(models));

            _Logging.Debug(_Header + "initialized");
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Generate a chat completion using streaming.
        /// </summary>
        /// <param name="model">Model.</param>
        /// <param name="prompt">Prompt containing conversation history.  Be sure to format this in accordance with the expectations of the model you are using.</param>
        /// <param name="maxTokens">Maximum number of tokens to generate.  Minimum is 100.</param>
        /// <param name="temperature">Temperature.  Minimum is 0.0, maximum is 1.0</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumerable of strings.</returns>
        public async IAsyncEnumerable<string> GenerateCompletionStreaming(
            string model,
            string prompt,
            int maxTokens = 512,
            float temperature = 0.7f,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (maxTokens < 100) throw new ArgumentOutOfRangeException(nameof(maxTokens));
            if (temperature < 0.0f || temperature > 1.0f) throw new ArgumentOutOfRangeException(nameof(temperature));
            if (prompt == null) prompt = "";

            LlamaSharpEngine engine = GetModelEngine(model);
            
            await foreach (string curr in engine.GenerateChatCompletionStreamAsync(prompt, maxTokens, temperature, null, token).ConfigureAwait(false))
            {
                yield return curr;
            }
        }

        /// <summary>
        /// Generate a chat completion without streaming.
        /// </summary>
        /// <param name="model">Model.</param>
        /// <param name="prompt">Prompt containing conversation history.  Be sure to format this in accordance with the expectations of the model you are using.</param>
        /// <param name="maxTokens">Maximum number of tokens to generate.  Minimum is 100.</param>
        /// <param name="temperature">Temperature.  Minimum is 0.0, maximum is 1.0</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>String.</returns>
        public async Task<string> GenerateCompletion(
            string model,
            string prompt,
            int maxTokens = 512,
            float temperature = 0.1f,
            CancellationToken token = default)
        {
            if (maxTokens < 100) throw new ArgumentOutOfRangeException(nameof(maxTokens));
            if (temperature < 0.0f || temperature > 1.0f) throw new ArgumentOutOfRangeException(nameof(temperature));
            if (prompt == null) prompt = "";

            LlamaSharpEngine engine = GetModelEngine(model);
            return await engine.GenerateChatCompletionAsync(prompt, maxTokens, temperature, null, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private LlamaSharpEngine GetModelEngine(string model) =>  _Models.GetEngine(model);

        #endregion
    }
}
