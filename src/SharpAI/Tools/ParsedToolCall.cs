namespace SharpAI.Tools
{
    using System;

    /// <summary>
    /// A tool/function call extracted from a model's raw output: the function name and its arguments as a
    /// JSON string. Used to translate model output into OpenAI/Ollama <c>tool_calls</c> structures.
    /// </summary>
    public class ParsedToolCall
    {
        #region Public-Members

        /// <summary>
        /// The tool/function name. Never null.
        /// </summary>
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value ?? String.Empty;
            }
        }

        /// <summary>
        /// The call arguments as a JSON object string. Defaults to "{}"; never null.
        /// </summary>
        public string ArgumentsJson
        {
            get
            {
                return _ArgumentsJson;
            }
            set
            {
                _ArgumentsJson = String.IsNullOrEmpty(value) ? "{}" : value;
            }
        }

        #endregion

        #region Private-Members

        private string _Name = String.Empty;
        private string _ArgumentsJson = "{}";

        #endregion
    }
}
