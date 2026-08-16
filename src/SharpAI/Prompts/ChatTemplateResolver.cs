namespace SharpAI.Prompts
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Resolves a chat prompt for a model, preferring the model's embedded chat template (the GGUF
    /// <c>tokenizer.chat_template</c>) and falling back to a hand-maintained family template only when the
    /// model has no usable embedded template or rendering fails. This mirrors how llama.cpp and Ollama
    /// format prompts and avoids the subtle mis-formatting that a family-based guess can produce.
    /// </summary>
    public static class ChatTemplateResolver
    {
        #region Public-Methods

        /// <summary>
        /// Resolve the prompt and accompanying stop sequences for a chat request.
        /// </summary>
        /// <param name="source">The model's template source. May be null, in which case only the fallback
        /// path is used.</param>
        /// <param name="modelFamily">Model family name used to select the fallback template.</param>
        /// <param name="messages">Ordered conversation messages. May not be null.</param>
        /// <param name="addGenerationPrompt">When true, append the assistant generation prefix.</param>
        /// <returns>The resolved prompt, stop sequences, and which path was taken. Never null.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="messages"/> is null.</exception>
        public static ChatTemplateResult Resolve(
            IChatTemplateSource source,
            string modelFamily,
            List<ChatMessage> messages,
            bool addGenerationPrompt = true)
        {
            if (messages == null) throw new ArgumentNullException(nameof(messages));

            if (source != null && source.SupportsEmbeddedChatTemplate)
            {
                try
                {
                    string embedded = source.RenderEmbeddedChatPrompt(messages, addGenerationPrompt);
                    if (!String.IsNullOrEmpty(embedded))
                    {
                        return new ChatTemplateResult
                        {
                            Prompt = embedded,
                            StopSequences = Array.Empty<string>(),
                            UsedEmbeddedTemplate = true
                        };
                    }
                }
                catch
                {
                    // Fall through to the family-based template on any rendering failure.
                }
            }

            ChatFormatEnum format = ChatFormatHelper.ModelFamilyToChatFormat(modelFamily, ChatFormatEnum.Simple);

            return new ChatTemplateResult
            {
                Prompt = ChatPromptBuilder.Build(format, messages),
                StopSequences = ChatFormatHelper.GetDefaultStopSequences(format),
                UsedEmbeddedTemplate = false
            };
        }

        #endregion
    }
}
