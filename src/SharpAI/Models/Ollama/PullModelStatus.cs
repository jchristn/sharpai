namespace SharpAI.Models.Ollama
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama pull model status message.
    /// </summary>
    public class PullModelStatus
    {
        #region Public-Members

        /// <summary>
        /// Status.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = null;

        /// <summary>
        /// Digest (SHA-256).
        /// </summary>
        [JsonPropertyName("digest")]
        public string Digest { get; set; } = null;

        /// <summary>
        /// Total bytes.
        /// </summary>
        [JsonPropertyName("total")]
        public long? Total { get; set; } = null;

        /// <summary>
        /// Completed bytes.
        /// </summary>
        [JsonPropertyName("completed")]
        public long? Completed { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
        /*
{
    "status": "pulling d18a5cc71b84",
    "digest": "sha256:d18a5cc71b84bc4af394a31116bd3932b42241de70c77d2b76d69a314ec8aa12",
    "total": 11338,
    "completed": 11338
}
         */
    }
}
