namespace SharpAI.Server.Classes.Settings
{
    using System;

    /// <summary>
    /// Debug settings.
    /// </summary>
    public class DebugSettings
    {
        /// <summary>
        /// True to enable request body logging.
        /// </summary>
        public bool RequestBody { get; set; } = false;

        /// <summary>
        /// Debug settings.
        /// </summary>
        public DebugSettings()
        {

        }
    }
}