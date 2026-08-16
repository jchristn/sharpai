namespace Test.Shared
{
    using System;
    using System.IO;

    /// <summary>
    /// Locates a GGUF model to use for model-dependent (integration) test suites. Resolution order:
    /// the <c>SHARPAI_TEST_MODEL_GGUF</c> environment variable (a path to a .gguf file), then the first
    /// .gguf found under a <c>test-models</c> directory at or above the test output directory. When no
    /// model is found, model-dependent suites skip cleanly rather than fail, so CI without a model stays
    /// green while a developer or CI job that provides one exercises real inference.
    /// </summary>
    public static class ModelFixture
    {
        #region Public-Members

        /// <summary>
        /// Environment variable that may point directly at a .gguf file.
        /// </summary>
        public static readonly string EnvVarName = "SHARPAI_TEST_MODEL_GGUF";

        /// <summary>
        /// Whether a test model was located.
        /// </summary>
        public static bool IsAvailable
        {
            get { return Path != null; }
        }

        /// <summary>
        /// The resolved model path, or null when none is available.
        /// </summary>
        public static string? Path
        {
            get
            {
                Resolve();
                return _Path;
            }
        }

        /// <summary>
        /// Human-readable reason used as the skip reason for model-dependent test cases.
        /// </summary>
        public static string SkipReason
        {
            get
            {
                return "No test GGUF model available. Set " + EnvVarName +
                    " to a .gguf path, or place a .gguf under a 'test-models' directory, to run model-dependent suites.";
            }
        }

        #endregion

        #region Private-Members

        private static readonly object _Lock = new object();
        private static bool _Resolved = false;
        private static string? _Path = null;

        #endregion

        #region Private-Methods

        private static void Resolve()
        {
            if (_Resolved) return;

            lock (_Lock)
            {
                if (_Resolved) return;
                _Resolved = true;

                string? env = Environment.GetEnvironmentVariable(EnvVarName);
                if (!String.IsNullOrEmpty(env) && File.Exists(env))
                {
                    _Path = env;
                    return;
                }

                string? dir = AppContext.BaseDirectory;
                for (int i = 0; i < 8 && !String.IsNullOrEmpty(dir); i++)
                {
                    string candidate = System.IO.Path.Combine(dir, "test-models");
                    if (Directory.Exists(candidate))
                    {
                        string[] models = Directory.GetFiles(candidate, "*.gguf", SearchOption.AllDirectories);
                        if (models.Length > 0)
                        {
                            _Path = models[0];
                            return;
                        }
                    }

                    DirectoryInfo? parent = Directory.GetParent(dir!);
                    dir = parent?.FullName;
                }
            }
        }

        #endregion
    }
}
