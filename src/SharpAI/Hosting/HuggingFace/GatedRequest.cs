namespace SharpAI.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents gated access request configuration for models with access control.
    /// </summary>
    public class GatedRequest
    {
        /// <summary>
        /// Gets or sets the gate type (e.g., "manual", "automatic").
        /// </summary>
        [JsonPropertyName("gate")]
        public string Gate { get; set; }

        /// <summary>
        /// Gets or sets the custom prompt shown to users requesting access.
        /// </summary>
        [JsonPropertyName("gatedPrompt")]
        public string GatedPrompt { get; set; }

        /// <summary>
        /// Gets or sets additional fields required for access requests.
        /// </summary>
        [JsonPropertyName("extraFields")]
        public Dictionary<string, object> ExtraFields { get; set; }
    }
}
