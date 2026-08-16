namespace SharpAI.Prompts
{
    using System;

    /// <summary>
    /// Result of resolving a chat prompt: the rendered prompt, the stop sequences that should accompany
    /// it, and whether the model's embedded template was used (versus a family-based fallback).
    /// </summary>
    public class ChatTemplateResult
    {
        #region Public-Members

        /// <summary>
        /// The rendered prompt string. Never null; empty when there were no messages.
        /// </summary>
        public string Prompt
        {
            get
            {
                return _Prompt;
            }
            set
            {
                _Prompt = value ?? String.Empty;
            }
        }

        /// <summary>
        /// Stop sequences to apply during generation. Empty when the embedded template is used, because
        /// the model's native end-of-turn tokens terminate generation. Never null.
        /// </summary>
        public string[] StopSequences
        {
            get
            {
                return _StopSequences;
            }
            set
            {
                _StopSequences = value ?? Array.Empty<string>();
            }
        }

        /// <summary>
        /// Whether the model's embedded chat template produced this prompt. False indicates a family-based
        /// fallback was used.
        /// </summary>
        public bool UsedEmbeddedTemplate { get; set; } = false;

        #endregion

        #region Private-Members

        private string _Prompt = String.Empty;
        private string[] _StopSequences = Array.Empty<string>();

        #endregion
    }
}
