namespace SharpAI
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
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

        /// <summary>
        /// Default context tokens.  Default is 4000.  Minimum value is 256.
        /// </summary>
        public int DefaultContextTokens
        {
            get => _DefaultContextTokens;
            set => _DefaultContextTokens = (value >= 256 ? value : throw new ArgumentOutOfRangeException(nameof(DefaultContextTokens)));
        }

        /// <summary>
        /// Context utilization ratio.  Default is 0.3.  Value must be greater than 0 and less than or equal to 1.
        /// </summary>
        public double ContextUtilizationRatio
        {
            get => _ContextUtilizationRatio;
            set => _ContextUtilizationRatio = (value > 0d && value <= 1d ? value : throw new ArgumentOutOfRangeException(nameof(ContextUtilizationRatio)));
        }

        /// <summary>
        /// Chunk size when working with context.  Default is 512.  Minimum value is 256.
        /// </summary>
        public int ChunkSize
        {
            get => _ChunkSize;
            set => _ChunkSize = (value >= 256 ? value : throw new ArgumentOutOfRangeException(nameof(ChunkSize)));
        }

        /// <summary>
        /// The amount of overlap that should exist between tokens.  Default is 100.  Minimum value is 0.
        /// </summary>
        public int ChunkOverlap
        {
            get => _ChunkOverlap;
            set => _ChunkOverlap = (value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(ChunkOverlap)));
        }

        #endregion

        #region Private-Members

        private string _Header = "[ChatDriver] ";
        private LoggingModule _Logging = null;
        private Serializer _Serializer = null;
        private ModelDriver _Models = null;
        private int _DefaultContextTokens = 4000;
        private double _ContextUtilizationRatio = 0.30;
        private int _ChunkSize = 512;
        private int _ChunkOverlap = 100;
        private readonly ConcurrentDictionary<int, List<string>> _ContextCache = new ConcurrentDictionary<int, List<string>>();

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
            var optimizedPrompt = OptimizePromptForTokenLimitExact(model, processedPrompt, maxTokens, engine);
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
            var optimizedPrompt = OptimizePromptForTokenLimitExact(model, processedPrompt, maxTokens, engine);

            return await engine.GenerateChatCompletionAsync(optimizedPrompt, maxTokens, temperature, null, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private LlamaSharpEngine GetModelEngine(string model) => _Models.GetEngine(model);

        private string ProcessPromptWithContext(string prompt, string model)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return prompt;

            var contextContentRaw = ExtractContextFromPrompt(prompt);
            if (string.IsNullOrWhiteSpace(contextContentRaw))
                return prompt;

            var userQuery = ExtractUserQueryFromPrompt(prompt);
            var relevantChunks = GetCachedRelevantChunks(contextContentRaw, userQuery, model);
            var optimizedContextContent = string.Join("\n\n", relevantChunks);
            var processedPrompt = ReplaceContextInPrompt(prompt, optimizedContextContent);

            _Logging.Debug($"{_Header}processed prompt with optimized context: {relevantChunks.Count} relevant chunks selected");
            return processedPrompt;
        }

        private string TruncatePromptByTokens(string prompt, int allowedTokens, LlamaSharpEngine engine)
        {
            if (string.IsNullOrEmpty(prompt)) return prompt;
            if (CountTokensFast(engine, prompt) <= allowedTokens) return prompt;

            var lines = prompt.Split('\n');
            if (lines.Length <= 2)
            {
                int approxChars = Math.Max(allowedTokens * 4 - 3, 0);
                return prompt.Length <= approxChars ? prompt : prompt.Substring(0, approxChars) + "...";
            }

            var head = new List<string>(lines.Length / 2);
            var tail = new List<string>(lines.Length / 2);
            int used = 0;
            int headBudget = (int)(allowedTokens * 0.67);

            foreach (var line in lines)
            {
                int t = CountTokensFast(engine, line) + 1;
                if (used + t > headBudget) break;
                head.Add(line);
                used += t;
            }

            int remaining = allowedTokens - used;
            for (int i = lines.Length - 1; i >= 0 && remaining > 0; i--)
            {
                int t = CountTokensFast(engine, lines[i]) + 1;
                if (t > remaining) break;
                tail.Insert(0, lines[i]);
                remaining -= t;
            }

            var joined = string.Join('\n', head) + "\n...\n" + string.Join('\n', tail);

            if (CountTokensFast(engine, joined) > allowedTokens)
            {
                int approxChars = Math.Max(allowedTokens * 4 - 3, 0);
                if (joined.Length > approxChars) joined = joined.Substring(0, approxChars) + "...";
            }
            return joined;
        }

        private string OptimizePromptForTokenLimitExact(string model, string prompt, int maxGenTokens, LlamaSharpEngine engine)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return prompt;

            int nCtxUsable = GetMaxContextTokens(model, engine);
            const int safety = 64;

            int promptTokens = CountTokensFast(engine, prompt);
            if (promptTokens + maxGenTokens + safety <= nCtxUsable)
            {
                if (promptTokens > nCtxUsable * 0.8)
                    _Logging.Debug($"{_Header}prompt near token limit: ~{promptTokens} tokens (limit: {nCtxUsable})");
                return prompt;
            }

            int allowedPromptTokens = Math.Max(256, nCtxUsable - maxGenTokens - safety);
            var shrunk = TruncatePromptByTokens(prompt, allowedPromptTokens, engine);

            int shrunkTokens = CountTokensFast(engine, shrunk);
            _Logging.Warn($"{_Header}prompt truncated token-aware: ~{shrunkTokens} tokens (from ~{promptTokens}, limit: {nCtxUsable})");
            return shrunk;
        }

        private List<string> GetCachedRelevantChunks(string context, string userQuery, string model)
        {
            int key = HashCode.Combine(
                context.GetHashCode(StringComparison.Ordinal),
                userQuery.GetHashCode(StringComparison.Ordinal));

            if (_ContextCache.TryGetValue(key, out var cached))
                return cached;

            var chunks = FindRelevantChunks(context, userQuery, model, CancellationToken.None);

            if (_ContextCache.Count > 100)
                _ContextCache.Clear();

            _ContextCache[key] = chunks;
            return chunks;
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
            var compressed = LightCompress(optimizedContextContent);
            return beforeContext + "\n" + compressed + "\n" + afterContext;
        }

        private int GetMaxContextTokens(string model, LlamaSharpEngine engine)
        {
            try
            {
                var contextSize = engine.GetContextSize();

                if (contextSize > 0)
                {
                    var maxTokens = (int)(contextSize * _ContextUtilizationRatio);
                    _Logging.Debug($"{_Header}model '{model}' context size: {contextSize}, using {maxTokens} tokens ({_ContextUtilizationRatio:P0} utilization)");
                    return maxTokens;
                }
                else
                {
                    _Logging.Debug($"{_Header}model '{model}' context size not available, using default: {_DefaultContextTokens} tokens");
                    return _DefaultContextTokens;
                }
            }
            catch (Exception ex)
            {
                _Logging.Warn($"{_Header}Error getting context size for model '{model}', using default: {ex.Message}");
                return _DefaultContextTokens;
            }
        }

        private List<string> ChunkText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            int step = Math.Max(1, _ChunkSize - _ChunkOverlap);
            int estimated = Math.Max(1, text.Length / step);
            var chunks = new List<string>(estimated);

            for (int i = 0; i < text.Length; i += step)
            {
                int len = Math.Min(_ChunkSize, text.Length - i);
                chunks.Add(text.Substring(i, len));
            }

            return chunks;
        }

        private List<string> FindRelevantChunks(string pdfContent, string userQuery, string model, CancellationToken token)
        {
            var chunks = ChunkText(pdfContent);
            if (chunks.Count == 0) return new List<string>();

            var queryWords = userQuery
                .Split(new[] { ' ', '\t', '\r', '\n', '.', ',', '!', '?', ':', ';', '(', ')', '[', ']', '{', '}', '/', '\\', '"', '\'' },
                       StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim())
                .Where(w => w.Length > 2)
                .Select(w => w.ToLowerInvariant())
                .Distinct()
                .ToArray();

            if (queryWords.Length == 0)
                return chunks.Take(5).ToList();

            var scored = new List<(int score, string chunk)>(chunks.Count);
            foreach (var chunk in chunks)
            {
                int s = 0;
                foreach (var qw in queryWords)
                {
                    if (chunk.AsSpan().IndexOf(qw, StringComparison.OrdinalIgnoreCase) >= 0)
                        s++;
                }
                if (s > 0) scored.Add((s, chunk));
            }

            List<string> result;
            if (scored.Count > 0)
            {
                result = scored
                    .OrderByDescending(t => t.score)
                    .Take(5)
                    .Select(t => t.chunk)
                    .ToList();
            }
            else
            {
                result = chunks.Take(5).ToList();
            }

            _Logging.Debug($"{_Header}selected {result.Count} relevant chunks from {chunks.Count} total chunks");
            return result;
        }

        private int CountTokensFast(LlamaSharpEngine engine, string text)
        {
            return engine.CountTokens(text);
        }

        private static string LightCompress(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[ \t]+", " ");
            var lines = s.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).Distinct();
            var trimmed = lines.Select(line =>
            {
                var parts = line.Split(new[] { ". ", "!\n", "?\n", ".\n" }, StringSplitOptions.None);
                return string.Join(". ", parts.Take(3)) + (parts.Length > 3 ? "..." : "");
            });
            return string.Join("\n", trimmed);
        }

        #endregion
    }
}