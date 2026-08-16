namespace SharpAI.Server.Classes.Settings
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Authentication settings. Authentication is disabled by default, matching Ollama's open local-server
    /// behavior; when disabled every request runs as an implicit, fully-authorized system principal. When
    /// enabled, the OpenAPI document, Swagger UI, health, and readiness endpoints remain anonymous, and
    /// other endpoints require a valid administrator API key (full user/credential/session/RBAC support is
    /// layered on top of this foundation).
    /// </summary>
    public class AuthSettings
    {
        #region Public-Members

        /// <summary>
        /// Whether authentication is enabled. Default false (open server).
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Valid administrator API keys (checked against the x-api-key header). Never null.
        /// </summary>
        public List<string> AdminApiKeys
        {
            get
            {
                return _AdminApiKeys;
            }
            set
            {
                _AdminApiKeys = value ?? new List<string>();
            }
        }

        /// <summary>
        /// Key material used to encrypt session bearer tokens (AES-256, SHA-256-derived key). When null or
        /// empty a random per-boot key is generated, which invalidates existing tokens on restart; set a
        /// stable value to keep sessions valid across restarts. Never null after loading.
        /// </summary>
        public string TokenSigningKey
        {
            get
            {
                return _TokenSigningKey;
            }
            set
            {
                _TokenSigningKey = value ?? String.Empty;
            }
        }

        /// <summary>
        /// Lifetime of a login session in minutes. Default 60; clamped to the range 1..43200 (30 days).
        /// </summary>
        public int SessionTtlMinutes
        {
            get
            {
                return _SessionTtlMinutes;
            }
            set
            {
                _SessionTtlMinutes = Math.Clamp(value, 1, 43200);
            }
        }

        #endregion

        #region Private-Members

        private List<string> _AdminApiKeys = new List<string>();
        private string _TokenSigningKey = String.Empty;
        private int _SessionTtlMinutes = 60;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AuthSettings()
        {
        }

        #endregion
    }
}
