namespace SharpAI.Prompts
{
    using System.Collections.Generic;

    /// <summary>
    /// Abstraction over a loaded model's ability to render chat prompts using the chat template embedded
    /// in the model itself (the GGUF <c>tokenizer.chat_template</c>). Implemented by the inference engine
    /// and consumed by <see cref="ChatTemplateResolver"/>. Kept as an interface so the resolver's
    /// decision logic can be tested without loading a native model.
    /// </summary>
    public interface IChatTemplateSource
    {
        /// <summary>
        /// Gets whether the loaded model carries a usable embedded chat template. When false, callers
        /// should fall back to a family-based template.
        /// </summary>
        bool SupportsEmbeddedChatTemplate { get; }

        /// <summary>
        /// Render the supplied messages into a prompt string using the model's embedded chat template.
        /// </summary>
        /// <param name="messages">Ordered conversation messages. May not be null.</param>
        /// <param name="addGenerationPrompt">When true, append the assistant generation prefix so the
        /// model continues as the assistant.</param>
        /// <returns>The rendered prompt.</returns>
        string RenderEmbeddedChatPrompt(IReadOnlyList<ChatMessage> messages, bool addGenerationPrompt);
    }
}
