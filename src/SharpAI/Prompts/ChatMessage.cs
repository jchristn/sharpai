namespace SharpAI.Prompts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a chat message with role, content, and timestamp information.
    /// Used for building conversation histories in chat completion scenarios.
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// Gets or sets the role of the message sender.
        /// Common values include "system", "user", and "assistant".
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content of the message.
        /// Contains the actual text content of the chat message.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the message was created.
        /// Defaults to the current UTC time when the ChatMessage instance is created.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Represents a chat message with role, content, and timestamp information.
        /// Used for building conversation histories in chat completion scenarios.
        /// </summary>
        public ChatMessage()
        {

        }
    }
}