namespace SharpAI.Helpers
{
    using System;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Filters model thinking/reasoning tokens from generated output. Reasoning models emit a block of
    /// chain-of-thought between an opening and closing marker (for example &lt;think&gt;...&lt;/think&gt;
    /// for Qwen3/DeepSeek-R1) that should typically be hidden from end users. The markers are configurable
    /// so the same filter serves models that use different tokens.
    /// </summary>
    public class ThinkingFilter
    {
        #region Public-Members

        /// <summary>
        /// Gets a value indicating whether the filter is currently inside a thinking block.
        /// </summary>
        public bool InsideThinkingBlock
        {
            get { return _InsideThinkingBlock; }
        }

        /// <summary>
        /// The opening marker for a thinking block. Default "&lt;think&gt;".
        /// </summary>
        public string OpenTag
        {
            get { return _ThinkOpen; }
        }

        /// <summary>
        /// The closing marker for a thinking block. Default "&lt;/think&gt;".
        /// </summary>
        public string CloseTag
        {
            get { return _ThinkClose; }
        }

        #endregion

        #region Private-Members

        private bool _InsideThinkingBlock = false;
        private StringBuilder _Buffer = new StringBuilder();

        private readonly string _ThinkOpen;
        private readonly string _ThinkClose;

        private static readonly string _DefaultOpen = "<think>";
        private static readonly string _DefaultClose = "</think>";

        private static readonly Regex _DefaultThinkBlockRegex = new Regex(
            @"<think>[\s\S]*?</think>\s*",
            RegexOptions.Compiled);

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initialize a thinking filter with the default markers ("&lt;think&gt;" / "&lt;/think&gt;").
        /// </summary>
        public ThinkingFilter()
            : this(_DefaultOpen, _DefaultClose)
        {
        }

        /// <summary>
        /// Initialize a thinking filter with custom markers.
        /// </summary>
        /// <param name="openTag">Opening marker. May not be null or empty.</param>
        /// <param name="closeTag">Closing marker. May not be null or empty.</param>
        /// <exception cref="ArgumentNullException">Thrown when either marker is null or empty.</exception>
        public ThinkingFilter(string openTag, string closeTag)
        {
            if (String.IsNullOrEmpty(openTag)) throw new ArgumentNullException(nameof(openTag));
            if (String.IsNullOrEmpty(closeTag)) throw new ArgumentNullException(nameof(closeTag));

            _ThinkOpen = openTag;
            _ThinkClose = closeTag;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Filter a complete (non-streaming) response, removing all thinking blocks delimited by the
        /// default markers.
        /// </summary>
        /// <param name="text">The full response text.</param>
        /// <returns>Text with thinking blocks removed.</returns>
        public static string RemoveThinkingBlocks(string text)
        {
            if (String.IsNullOrEmpty(text)) return text;
            return _DefaultThinkBlockRegex.Replace(text, "").TrimStart();
        }

        /// <summary>
        /// Filter a complete (non-streaming) response, removing all thinking blocks delimited by the
        /// supplied markers.
        /// </summary>
        /// <param name="text">The full response text.</param>
        /// <param name="openTag">Opening marker. May not be null or empty.</param>
        /// <param name="closeTag">Closing marker. May not be null or empty.</param>
        /// <returns>Text with thinking blocks removed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when either marker is null or empty.</exception>
        public static string RemoveThinkingBlocks(string text, string openTag, string closeTag)
        {
            if (String.IsNullOrEmpty(openTag)) throw new ArgumentNullException(nameof(openTag));
            if (String.IsNullOrEmpty(closeTag)) throw new ArgumentNullException(nameof(closeTag));
            if (String.IsNullOrEmpty(text)) return text;

            string pattern = Regex.Escape(openTag) + @"[\s\S]*?" + Regex.Escape(closeTag) + @"\s*";
            return Regex.Replace(text, pattern, "").TrimStart();
        }

        /// <summary>
        /// Process a streaming token. Returns the token to emit, or null/empty if it should be suppressed.
        /// Call this for each token in the stream to filter thinking blocks in real time.
        /// </summary>
        /// <param name="token">The next token from the stream.</param>
        /// <returns>The text to emit (may be empty if inside a thinking block), or the buffered text if a partial tag match was resolved.</returns>
        public string ProcessToken(string token)
        {
            if (String.IsNullOrEmpty(token)) return token;

            _Buffer.Append(token);
            string buffered = _Buffer.ToString();

            if (_InsideThinkingBlock)
            {
                int closeIdx = buffered.IndexOf(_ThinkClose, StringComparison.Ordinal);
                if (closeIdx >= 0)
                {
                    // End of thinking block found — discard everything up to and including the close tag.
                    _InsideThinkingBlock = false;
                    string remainder = buffered.Substring(closeIdx + _ThinkClose.Length).TrimStart();
                    _Buffer.Clear();

                    if (remainder.Length > 0)
                    {
                        // Recurse in case there's another thinking block in the remainder
                        return ProcessToken(remainder);
                    }

                    return "";
                }

                // Still inside thinking, keep buffering but don't emit. Trim buffer to avoid unbounded
                // growth — keep only enough to catch a close tag that spans token boundaries.
                if (_Buffer.Length > _ThinkClose.Length * 2)
                {
                    string keep = buffered.Substring(buffered.Length - _ThinkClose.Length);
                    _Buffer.Clear();
                    _Buffer.Append(keep);
                }

                return "";
            }
            else
            {
                int openIdx = buffered.IndexOf(_ThinkOpen, StringComparison.Ordinal);
                if (openIdx >= 0)
                {
                    // Found opening tag
                    _InsideThinkingBlock = true;
                    string beforeThink = buffered.Substring(0, openIdx);
                    string afterOpen = buffered.Substring(openIdx + _ThinkOpen.Length);
                    _Buffer.Clear();
                    _Buffer.Append(afterOpen);

                    // Check if closing tag is already in the buffer
                    string result = ProcessToken("");
                    return beforeThink + result;
                }

                // Check if buffer ends with a partial match of the opening tag
                bool couldBePartialTag = false;
                for (int len = 1; len < _ThinkOpen.Length && len <= buffered.Length; len++)
                {
                    if (buffered.EndsWith(_ThinkOpen.Substring(0, len), StringComparison.Ordinal))
                    {
                        couldBePartialTag = true;
                        break;
                    }
                }

                if (couldBePartialTag)
                {
                    // Don't emit the potential partial tag yet, keep buffering
                    return "";
                }

                // No tag, emit everything
                _Buffer.Clear();
                return buffered;
            }
        }

        /// <summary>
        /// Flush any remaining buffered content. Call this when the stream ends.
        /// </summary>
        /// <returns>Any remaining buffered text that should be emitted. Empty when the stream ended inside a thinking block.</returns>
        public string Flush()
        {
            string remaining = _Buffer.ToString();
            bool wasInside = _InsideThinkingBlock;
            _Buffer.Clear();
            _InsideThinkingBlock = false;

            // If we were inside a thinking block, discard the remaining unclosed content.
            if (wasInside) return "";
            return remaining;
        }

        #endregion
    }
}
