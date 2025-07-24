namespace SharpAI.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Ollama message.
    /// </summary>
    public class Message
    {
        #region Public-Members

        /// <summary>
        /// Role, generally system, user, or assistant.
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = null;

        /// <summary>
        /// Content.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = null;

        /// <summary>
        /// Timestamp, in UTC time.
        /// </summary>
        [JsonPropertyName("")]
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Ollama message.
        /// </summary>
        public Message()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
