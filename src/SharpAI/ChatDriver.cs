namespace SharpAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using SharpAI.Engines;
    using SharpAI.Serialization;
    using SyslogLogging;

    /// <summary>
    /// Chat driver for handling chat completions with context optimization.
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
        private const int DEFAULT_CONTEXT_TOKENS = 4000;
        private const int MAX_MESSAGES_IN_CONTEXT = 10;
        private const double CONTEXT_UTILIZATION_RATIO = 0.8;
        private const int CHUNK_SIZE = 1000;
        private const int CHUNK_OVERLAP = 200;

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
        /// Supports context extraction and optimization from formatted prompt strings.
        /// </summary>
        /// <param name="model">Model.</param>
        /// <param name="prompt">Formatted prompt string. Context can be provided in system messages with <context></context> tags.</param>
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
            var processedPrompt = ProcessPromptWithContext(prompt, model);
            var optimizedPrompt = OptimizePromptForTokenLimit(model, processedPrompt, engine);
            await foreach (string curr in engine.GenerateChatCompletionStreamAsync(optimizedPrompt, maxTokens, temperature, null, token).ConfigureAwait(false))
            {
                yield return curr;
            }
        }

        /// <summary>
        /// Generate a chat completion without streaming.
        /// Supports context extraction and optimization from formatted prompt strings.
        /// </summary>
        /// <param name="model">Model.</param>
        /// <param name="prompt">Formatted prompt string. Context can be provided in system messages with <context></context> tags.</param>
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
            var processedPrompt = ProcessPromptWithContext(prompt, model);
            var optimizedPrompt = OptimizePromptForTokenLimit(model, processedPrompt, engine);

            return await engine.GenerateChatCompletionAsync(optimizedPrompt, maxTokens, temperature, null, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private LlamaSharpEngine GetModelEngine(string model) => _Models.GetEngine(model);

        private string ProcessPromptWithContext(string prompt, string model)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return prompt;

            var contextContent = ExtractContextFromPrompt(prompt);
            if (string.IsNullOrWhiteSpace(contextContent))
                return prompt;

            var userQuery = ExtractUserQueryFromPrompt(prompt);
            var relevantChunks = FindRelevantChunks(contextContent, userQuery, model, CancellationToken.None);
            var optimizedContextContent = string.Join("\n\n", relevantChunks);
            var processedPrompt = ReplaceContextInPrompt(prompt, optimizedContextContent);

            _Logging.Debug($"{_Header}processed prompt with optimized context: {relevantChunks.Count} relevant chunks selected");

            return processedPrompt;
        }

        private string ExtractContextFromPrompt(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return null;

            var startTag = "<context>";
            var endTag = "</context>";

            var startIndex = prompt.LastIndexOf(startTag, StringComparison.OrdinalIgnoreCase);
            while (startIndex != -1)
            {
                var endIndex = prompt.IndexOf(endTag, startIndex, StringComparison.OrdinalIgnoreCase);
                if (endIndex == -1)
                    return null;

                var contextStart = startIndex + startTag.Length;
                var content = prompt.Substring(contextStart, endIndex - contextStart).Trim();
                if (!string.IsNullOrEmpty(content))
                    return content;

                if (startIndex == 0) break;
                startIndex = prompt.LastIndexOf(startTag, startIndex - 1, StringComparison.OrdinalIgnoreCase);
            }

            return null;
        }

        private string ExtractUserQueryFromPrompt(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return "";

            var userPatterns = new[] { "user:", "User:", "human:", "Human:" };

            foreach (var pattern in userPatterns)
            {
                var index = prompt.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (index != -1)
                {
                    var startIndex = index + pattern.Length;
                    var endIndex = prompt.IndexOf('\n', startIndex);
                    if (endIndex == -1)
                        endIndex = prompt.Length;

                    return prompt.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }

            return "";
        }

        private string ReplaceContextInPrompt(string originalPrompt, string optimizedContextContent)
        {
            if (string.IsNullOrWhiteSpace(originalPrompt))
                return originalPrompt;

            var startTag = "<context>";
            var endTag = "</context>";

            var startIndex = originalPrompt.LastIndexOf(startTag, StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1)
                return originalPrompt;

            var endIndex = originalPrompt.IndexOf(endTag, startIndex, StringComparison.OrdinalIgnoreCase);
            if (endIndex == -1)
                return originalPrompt;

            var beforeContext = originalPrompt.Substring(0, startIndex + startTag.Length);
            var afterContext = originalPrompt.Substring(endIndex);

            return beforeContext + "\n" + optimizedContextContent + "\n" + afterContext;
        }

        private int GetMaxContextTokens(string model, LlamaSharpEngine engine)
        {
            try
            {
                var contextSize = engine.GetContextSize();

                if (contextSize > 0)
                {
                    var maxTokens = (int)(contextSize * CONTEXT_UTILIZATION_RATIO);
                    _Logging.Debug($"{_Header}model '{model}' context size: {contextSize}, using {maxTokens} tokens ({CONTEXT_UTILIZATION_RATIO:P0} utilization)");
                    return maxTokens;
                }
                else
                {
                    _Logging.Debug($"{_Header}model '{model}' context size not available, using default: {DEFAULT_CONTEXT_TOKENS} tokens");
                    return DEFAULT_CONTEXT_TOKENS;
                }
            }
            catch (Exception ex)
            {
                _Logging.Warn($"{_Header}Error getting context size for model '{model}', using default: {ex.Message}");
                return DEFAULT_CONTEXT_TOKENS;
            }
        }

        private string OptimizePromptForTokenLimit(string model, string prompt, LlamaSharpEngine engine)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return prompt;

            const int charsPerToken = 4;
            var maxContextTokens = GetMaxContextTokens(model, engine);
            var maxChars = maxContextTokens * charsPerToken;

            if (prompt.Length <= maxChars)
            {
                if (prompt.Length > maxChars * 0.8)
                    _Logging.Debug($"{_Header}prompt near token limit: {prompt.Length} chars, ~{prompt.Length / charsPerToken} tokens (limit: {maxContextTokens})");
                return prompt;
            }

            var originalLength = prompt.Length;
            var optimizedPrompt = TruncatePromptIntelligently(prompt, maxChars);
            _Logging.Warn($"{_Header}prompt truncated: {optimizedPrompt.Length} chars, ~{optimizedPrompt.Length / charsPerToken} tokens (truncated from {originalLength} chars, limit: {maxContextTokens})");

            return optimizedPrompt;
        }

        private string TruncatePromptIntelligently(string prompt, int maxChars)
        {
            if (prompt.Length <= maxChars)
                return prompt;

            var lines = prompt.Split('\n').ToList();

            if (lines.Count <= 2)
                return prompt.Substring(0, maxChars - 3) + "...";

            var preservedLines = new List<string>();
            var currentLength = 0;
            var reservedForEnd = maxChars / 3;

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (currentLength + line.Length + 1 <= maxChars - reservedForEnd)
                {
                    preservedLines.Add(line);
                    currentLength += line.Length + 1;
                }
                else
                    break;
            }

            var endLines = new List<string>();
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                var line = lines[i];
                if (currentLength + line.Length + 1 <= maxChars && endLines.Count < MAX_MESSAGES_IN_CONTEXT)
                {
                    endLines.Insert(0, line);
                    currentLength += line.Length + 1;
                }
                else
                    break;
            }

            var result = string.Join("\n", preservedLines.Concat(endLines));

            if (result.Length > maxChars)
                result = result.Substring(0, maxChars - 3) + "...";

            return result;
        }

        private List<string> ChunkText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            var chunks = new List<string>();
            var sentences = text.Split(new[] { ". ", ".\n", "!\n", "?\n" }, StringSplitOptions.RemoveEmptyEntries);

            var currentChunk = new StringBuilder();

            foreach (var sentence in sentences)
            {
                if (currentChunk.Length + sentence.Length > CHUNK_SIZE && currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    var words = currentChunk.ToString().Split(' ');
                    var overlapWords = words.Skip(Math.Max(0, words.Length - (CHUNK_OVERLAP / 10))).ToArray();
                    currentChunk = new StringBuilder(string.Join(" ", overlapWords) + " ");
                }

                currentChunk.Append(sentence + ". ");
            }

            if (currentChunk.Length > 0)
                chunks.Add(currentChunk.ToString().Trim());

            return chunks;
        }

        private List<string> FindRelevantChunks(string pdfContent, string userQuery, string model, CancellationToken token)
        {
            var chunks = ChunkText(pdfContent);
            if (chunks.Count == 0) return new List<string>();

            var queryWords = userQuery.ToLower()
                .Split(new[] { ' ', '\t', '\n', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToHashSet();

            var scoredChunks = chunks.Select(chunk => new
            {
                Chunk = chunk,
                Score = CalculateRelevanceScore(chunk.ToLower(), queryWords)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(10)
            .Select(x => x.Chunk)
            .ToList();

            if (scoredChunks.Count == 0 && chunks.Count > 0)
            {
                scoredChunks = chunks.Take(5).ToList();
            }

            _Logging.Debug($"{_Header}selected {scoredChunks.Count} relevant chunks from {chunks.Count} total chunks");
            return scoredChunks;
        }

        private int CalculateRelevanceScore(string text, HashSet<string> queryWords)
        {
            var textWords = text.Split(new[] { ' ', '\t', '\n', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            return textWords.Count(word => queryWords.Contains(word.ToLower()));
        }

        #endregion
    }
}