namespace SharpAI.Server.Classes.Settings
{
    using System;

    /// <summary>
    /// HuggingFace settings.
    /// </summary>
    public class HuggingFaceSettings
    {
        /// <summary>
        /// API key.
        /// </summary>
        public string ApiKey
        {
            get => _ApiKey;
            set => _ApiKey = (!String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(ApiKey)));
        }

        private string _ApiKey = Constants.DefaultHuggingFaceApiKey;
        
        /// <summary>
        /// HuggingFace settings.
        /// </summary>
        public HuggingFaceSettings()
        {

        } 
    }
}