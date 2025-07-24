﻿namespace SharpAI.Services
{
    using SharpAI.Engines;
    using SyslogLogging;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Model engine service.
    /// </summary>
    public class ModelEngineService
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private string _Header = "[ModelEngineService] ";
        private LoggingModule _Logging = null;
        private Dictionary<string, LlamaSharpEngine> _Engines = new Dictionary<string, LlamaSharpEngine>();
        private readonly object _EnginesLock = new object();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Model engine service.
        /// </summary>
        /// <param name="logging">Logging.</param>
        public ModelEngineService(LoggingModule logging)
        {
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));

            _Logging.Debug(_Header + "initialized");
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Get the engine for a given model file.
        /// </summary>
        /// <param name="filename">Path and filename to the model.</param>
        /// <returns>Instance.</returns>
        public LlamaSharpEngine GetByModelFile(string filename)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            if (!File.Exists(filename)) throw new FileNotFoundException("The specified file could not be found.", filename);

            LlamaSharpEngine engine = null;

            lock (_EnginesLock)
            {
                if (_Engines.ContainsKey(filename)) return _Engines[filename];
                else
                {
                    engine = new LlamaSharpEngine(_Logging);
                    engine.ModelPath = filename;
                    engine.InitializeAsync(filename).Wait();
                    _Engines.Add(filename, engine);
                }
            }

            return engine;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
