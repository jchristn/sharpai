﻿namespace SharpAI.Prompts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Provides methods for building prompts in various chat formats used by different language models.
    /// </summary>
    public static class PromptBuilder
    {
        /// <summary>
        /// Builds a formatted prompt string based on the specified chat format and messages.
        /// </summary>
        /// <param name="chatFormat">The chat format to use for formatting the messages.</param>
        /// <param name="messages">List of chat messages to format into a prompt.</param>
        /// <returns>A formatted prompt string ready for model inference.</returns>
        /// <exception cref="ArgumentNullException">Thrown when messages is null.</exception>
        /// <exception cref="ArgumentException">Thrown when messages is empty.</exception>
        public static string Build(ChatFormat chatFormat, List<ChatMessage> messages)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            if (messages.Count == 0)
                throw new ArgumentException("Messages list cannot be empty.", nameof(messages));

            return chatFormat switch
            {
                ChatFormat.Simple => BuildSimple(messages),
                ChatFormat.ChatML => BuildChatML(messages),
                ChatFormat.Llama2 => BuildLlama2(messages),
                ChatFormat.Llama3 => BuildLlama3(messages),
                ChatFormat.Alpaca => BuildAlpaca(messages),
                ChatFormat.Mistral => BuildMistral(messages),
                ChatFormat.HumanAssistant => BuildHumanAssistant(messages),
                ChatFormat.Zephyr => BuildZephyr(messages),
                ChatFormat.Phi => BuildPhi(messages),
                ChatFormat.DeepSeek => BuildDeepSeek(messages),
                _ => BuildSimple(messages) // Default fallback
            };
        }

        private static string BuildSimple(List<ChatMessage> messages)
        {
            var sb = new StringBuilder();
            foreach (var message in messages)
            {
                sb.AppendLine($"{message.Role}: {message.Content}");
            }
            sb.Append("assistant:");
            return sb.ToString();
        }

        private static string BuildChatML(List<ChatMessage> messages)
        {
            var sb = new StringBuilder();
            foreach (var message in messages)
            {
                sb.AppendLine($"<|im_start|>{message.Role}");
                sb.Append(message.Content);
                sb.AppendLine("<|im_end|>");
            }
            sb.AppendLine("<|im_start|>assistant");
            return sb.ToString();
        }

        private static string BuildLlama2(List<ChatMessage> messages)
        {
            var sb = new StringBuilder();
            var systemMessage = messages.FirstOrDefault(m => m.Role.Equals("system", StringComparison.OrdinalIgnoreCase));
            var conversationMessages = messages.Where(m => !m.Role.Equals("system", StringComparison.OrdinalIgnoreCase)).ToList();

            sb.Append("<s>[INST] ");

            if (systemMessage != null)
            {
                sb.AppendLine("<<SYS>>");
                sb.AppendLine(systemMessage.Content);
                sb.AppendLine("<</SYS>>");
                sb.AppendLine();
            }

            for (int i = 0; i < conversationMessages.Count; i++)
            {
                var message = conversationMessages[i];

                if (message.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    if (i > 0) sb.Append("<s>[INST] ");
                    sb.Append(message.Content);
                    sb.Append(" [/INST]");
                }
                else if (message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append($" {message.Content} </s>");
                }
            }

            if (!conversationMessages.Last().Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(" ");
            }

            return sb.ToString();
        }

        private static string BuildLlama3(List<ChatMessage> messages)
        {
            var sb = new StringBuilder();
            sb.Append("<|begin_of_text|>");

            foreach (var message in messages)
            {
                sb.Append($"<|start_header_id|>{message.Role}<|end_header_id|>");
                sb.AppendLine();
                sb.AppendLine();
                sb.Append(message.Content);
                sb.Append("<|eot_id|>");
            }

            sb.Append("<|start_header_id|>assistant<|end_header_id|>");
            sb.AppendLine();
            sb.AppendLine();

            return sb.ToString();
        }

        private static string BuildAlpaca(List<ChatMessage> messages)
        {
            var sb = new StringBuilder();
            var systemMessage = messages.FirstOrDefault(m => m.Role.Equals("system", StringComparison.OrdinalIgnoreCase));

            if (systemMessage != null)
            {
                sb.AppendLine("### System:");
                sb.AppendLine(systemMessage.Content);
                sb.AppendLine();
            }

            foreach (var message in messages.Where(m => !m.Role.Equals("system", StringComparison.OrdinalIgnoreCase)))
            {
                if (message.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine("### Instruction:");
                    sb.AppendLine(message.Content);
                    sb.AppendLine();
                }
                else if (message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine("### Response:");
                    sb.AppendLine(message.Content);
                    sb.AppendLine();
                }
            }

            sb.AppendLine("### Response:");
            return sb.ToString();
        }

        private static string BuildMistral(List<ChatMessage> messages)
        {
            var sb = new StringBuilder();
            var conversationMessages = messages.Where(m => !m.Role.Equals("system", StringComparison.OrdinalIgnoreCase)).ToList();

            // Mistral typically includes system message as part of first user message
            var systemMessage = messages.FirstOrDefault(m => m.Role.Equals("system", StringComparison.OrdinalIgnoreCase));

            for (int i = 0; i < conversationMessages.Count; i++)
            {
                var message = conversationMessages[i];

                if (message.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append("<s>[INST] ");

                    // Include system message with first user message
                    if (i == 0 && systemMessage != null)
                    {
                        sb.Append(systemMessage.Content);
                        sb.Append("\n\n");
                    }

                    sb.Append(message.Content);
                    sb.Append(" [/INST]");
                }
                else if (message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append($" {message.Content}</s>");
                }
            }

            if (!conversationMessages.Last().Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(" ");
            }

            return sb.ToString();
        }

        private static string BuildHumanAssistant(List<ChatMessage> messages)
        {
            var sb = new StringBuilder();

            foreach (var message in messages)
            {
                if (message.Role.Equals("system", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"System: {message.Content}");
                    sb.AppendLine();
                }
                else if (message.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"Human: {message.Content}");
                    sb.AppendLine();
                }
                else if (message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"Assistant: {message.Content}");
                    sb.AppendLine();
                }
            }

            sb.Append("Assistant:");
            return sb.ToString();
        }

        private static string BuildZephyr(List<ChatMessage> messages)
        {
            var sb = new StringBuilder();

            foreach (var message in messages)
            {
                sb.AppendLine($"<|{message.Role}|>");
                sb.Append(message.Content);
                sb.AppendLine("</s>");
            }

            sb.AppendLine("<|assistant|>");
            return sb.ToString();
        }

        private static string BuildPhi(List<ChatMessage> messages)
        {
            var sb = new StringBuilder();
            var systemMessage = messages.FirstOrDefault(m => m.Role.Equals("system", StringComparison.OrdinalIgnoreCase));

            if (systemMessage != null)
            {
                sb.AppendLine($"System: {systemMessage.Content}");
                sb.AppendLine();
            }

            foreach (var message in messages.Where(m => !m.Role.Equals("system", StringComparison.OrdinalIgnoreCase)))
            {
                if (message.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"Instruct: {message.Content}");
                }
                else if (message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"Output: {message.Content}");
                }
            }

            sb.Append("Output: ");
            return sb.ToString();
        }

        private static string BuildDeepSeek(List<ChatMessage> messages)
        {
            var sb = new StringBuilder();

            foreach (var message in messages)
            {
                if (message.Role.Equals("system", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"System: {message.Content}");
                    sb.AppendLine();
                }
                else if (message.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"User: {message.Content}");
                    sb.AppendLine();
                }
                else if (message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"Assistant: {message.Content}");
                    sb.AppendLine();
                }
            }

            sb.Append("Assistant: ");
            return sb.ToString();
        }
    }
}