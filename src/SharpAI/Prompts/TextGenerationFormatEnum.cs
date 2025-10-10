namespace SharpAI.Prompts
{
    /// <summary>
    /// Defines common text generation prompt patterns.
    /// </summary>
    public enum TextGenerationFormatEnum
    {
        /// <summary>
        /// Raw text with no formatting. The prompt is used as-is.
        /// </summary>
        /// <example>
        /// Write a story about a robot learning to paint.
        /// </example>
        Raw,

        /// <summary>
        /// Completion format where the model continues from the prompt.
        /// </summary>
        /// <example>
        /// Once upon a time, in a small village by the sea,
        /// </example>
        Completion,

        /// <summary>
        /// Instruction following format with clear directive.
        /// </summary>
        /// <example>
        /// ### Instruction:
        /// Write a haiku about summer rain.
        /// 
        /// ### Response:
        /// </example>
        Instruction,

        /// <summary>
        /// Question-answer format for knowledge queries.
        /// </summary>
        /// <example>
        /// Question: What are the main causes of climate change?
        /// 
        /// Answer:
        /// </example>
        QuestionAnswer,

        /// <summary>
        /// Creative writing format with genre and style hints.
        /// </summary>
        /// <example>
        /// Genre: Science Fiction
        /// Style: Descriptive, atmospheric
        /// Topic: First contact with aliens
        /// 
        /// Story:
        /// </example>
        CreativeWriting,

        /// <summary>
        /// Code generation format with language specification.
        /// </summary>
        /// <example>
        /// Language: Python
        /// Task: Implement a binary search algorithm
        /// 
        /// ```python
        /// </example>
        CodeGeneration,

        /// <summary>
        /// Academic/formal writing format.
        /// </summary>
        /// <example>
        /// Title: The Impact of Artificial Intelligence on Healthcare
        /// Type: Research Summary
        /// 
        /// Abstract:
        /// </example>
        Academic,

        /// <summary>
        /// List generation format for structured output.
        /// </summary>
        /// <example>
        /// Create a list of 10 creative business ideas for sustainable technology:
        /// 
        /// 1.
        /// </example>
        ListGeneration,

        /// <summary>
        /// Template filling format with placeholders.
        /// </summary>
        /// <example>
        /// Complete the following template:
        /// 
        /// Subject: [TOPIC]
        /// Dear [RECIPIENT],
        /// 
        /// I am writing to inform you about
        /// </example>
        TemplateFilling,

        /// <summary>
        /// Dialogue generation format for conversational content.
        /// </summary>
        /// <example>
        /// Characters: Alice (scientist), Bob (journalist)
        /// Setting: Research laboratory
        /// Topic: New discovery
        /// 
        /// Alice:
        /// </example>
        Dialogue
    }
}
