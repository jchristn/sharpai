namespace SharpAI
{
    using CommunityToolkit.HighPerformance;
    using DatabaseWrapper.Core;
    using ExpressionTree;
    using SharpAI.Engines;
    using SharpAI.Helpers;
    using SharpAI.Hosting;
    using SharpAI.Models;
    using SharpAI.Services;
    using SQLitePCL;
    using SyslogLogging;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Watson.ORM.Sqlite;

    /// <summary>
    /// Model driver.
    /// </summary>
    public class ModelDriver
    {
        #region Public-Members

        /// <summary>
        /// HuggingFace API key.
        /// </summary>
        public string HuggingFaceApiKey
        {
            get
            {
                return _HuggingFaceApiKey;
            }
        }

        /// <summary>
        /// Model directory.
        /// </summary>
        public string ModelDirectory
        {
            get
            {
                return _ModelDirectory;
            }
        }

        #endregion

        #region Private-Members

        private string _Header = "[ModelDriver] ";
        private LoggingModule _Logging = null;
        private WatsonORM _ORM = null;
        private string _HuggingFaceApiKey = null;
        private string _ModelDirectory = "./models/";
        private ModelEngineService _ModelEngines = null;
        private ModelFileService _ModelFiles = null;
        private HuggingFaceClient _HuggingFace = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Model driver.
        /// </summary>
        /// <param name="logging">Logging module.</param>
        /// <param name="orm">ORM.</param>
        /// <param name="huggingFaceApiKey">HuggingFace API key.</param>
        /// <param name="modelDirectory">Model storage directory.</param>
        public ModelDriver(
            LoggingModule logging, 
            WatsonORM orm,
            string huggingFaceApiKey, 
            string modelDirectory = "./models/")
        {
            if (String.IsNullOrEmpty(huggingFaceApiKey)) throw new ArgumentNullException(nameof(huggingFaceApiKey));
            if (String.IsNullOrEmpty(modelDirectory)) throw new ArgumentNullException(nameof(modelDirectory));

            modelDirectory = DirectoryHelper.NormalizeDirectory(modelDirectory);
                        
            _Logging = logging ?? new LoggingModule();
            _ORM = orm ?? throw new ArgumentNullException(nameof(orm));
            _HuggingFaceApiKey = huggingFaceApiKey;
            _ModelDirectory = modelDirectory;
            _ModelEngines = new ModelEngineService(_Logging);
            _ModelFiles = new ModelFileService(_Logging, _ORM, _ModelDirectory);
            _HuggingFace = new HuggingFaceClient(_Logging, HuggingFaceApiKey);

            _Logging.Debug(_Header + "initialized");
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Retrieve all.
        /// </summary>
        /// <returns></returns>
        public List<ModelFile> All()
        {
            return _ModelFiles.All();
        }

        /// <summary>
        /// Enumerate.
        /// </summary>
        /// <param name="continuationToken">Continuation token.</param>
        /// <param name="maxResults">Maximum number of results to retrieve.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="filter">Filters to add to the request.</param>
        /// <param name="ordering">Ordering.</param>
        /// <returns>Enumeration result.</returns>
        public EnumerationResult<ModelFile> Enumerate(
            Guid? continuationToken = null,
            int maxResults = 100,
            int skip = 0,
            Dictionary<string, string> filter = null,
            EnumerationOrderEnum ordering = EnumerationOrderEnum.CreatedDescending)
        {
            return _ModelFiles.Enumerate(continuationToken, maxResults, skip, filter, ordering);
        }

        /// <summary>
        /// Get by GUID.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <returns>Instance.</returns>
        public ModelFile GetByGuid(Guid guid)
        {
            return _ModelFiles.GetByGuid(guid);
        }

        /// <summary>
        /// Get by name.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <returns>Instance.</returns>
        public ModelFile GetByName(string name)
        {
            return _ModelFiles.GetByName(name);
        }

        /// <summary>
        /// Retrieve the full path and filename of a given model by name.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <returns>Path and filename.</returns>
        public string GetFilename(string name)
        {
            return _ModelFiles.GetFilename(name);
        }

        /// <summary>
        /// Get many.
        /// </summary>
        /// <param name="guids">GUIDs.</param>
        /// <returns>List.</returns>
        public List<ModelFile> GetMany(List<Guid> guids)
        {
            return _ModelFiles.GetMany(guids);
        }

        /// <summary>
        /// Exists by GUID.
        /// </summary>        
        /// <param name="guid">GUID.</param>
        /// <returns>True if exists.</returns>
        public bool ExistsByGuid(Guid guid)
        {
            return _ModelFiles.ExistsByGuid(guid);
        }

        /// <summary>
        /// Retrieve first.
        /// </summary>
        /// <param name="expr">Expr.</param>
        /// <returns>Instance.</returns>
        public ModelFile First(Expr expr)
        {
            return _ModelFiles.First(expr);
        }

        /// <summary>
        /// Add.
        /// </summary>
        /// <param name="name">Model name.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Instance.</returns>
        public async Task<ModelFile> Add(string name, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            ModelFile existing = _ModelFiles.GetByName(name);
            if (existing != null)
            {
                _Logging.Debug(_Header + "model " + name + " already exists");
                return existing;
            }

            List<GgufFileInfo> ggufFiles = await _HuggingFace.GetGgufFilesAsync(name, token).ConfigureAwait(false);
            if (ggufFiles == null)
            {
                _Logging.Warn(_Header + "no GGUF files found for model " + name);
                throw new Exception("No GGUF files found for model '" + name + "'.");
            }

            GgufFileInfo preferred = GgufSelector.SortByOllamaPreference(ggufFiles).First();
            _Logging.Debug(_Header + "using GGUF file " + preferred.Path + " as the preferred file for model " + name);

            List<string> urls = _HuggingFace.GetDownloadUrls(name, preferred);
            if (urls == null || urls.Count < 1)
            {
                _Logging.Warn("no download URLs found for model " + name);
                throw new Exception("No download URLs found for model " + name + ".");
            }

            string msg = _Header + "attempting download of model " + name + " from the following URLs:";
            foreach (string url in urls)
            {
                msg += Environment.NewLine + "| " + url;
            }

            ModelFile modelFile = new ModelFile
            {
                Name = name
            };

            bool success = false;
            string filename = null;
            string successUrl = null;

            foreach (string url in urls)
            {
                filename = Path.Combine(_ModelDirectory, modelFile.GUID.ToString());
                _Logging.Debug(_Header + "attempting download of model " + name + " using URL " + url + " to file " + modelFile.GUID.ToString());

                success = await _HuggingFace.TryDownloadFileAsync(url, filename, token).ConfigureAwait(false);
                if (success && File.Exists(filename) && new FileInfo(filename).Length == preferred.Size)
                {
                    _Logging.Info(_Header + "successfully downloaded model " + name + " using URL " + url + " to file " + filename);
                    successUrl = url;
                    success = true;
                    break;
                }
                else
                {
                    success = false;
                }
            }

            if (!success || String.IsNullOrEmpty(filename))
            {
                _Logging.Warn(_Header + "unable to download model " + name + " using " + urls.Count + " URL(s)");
                throw new Exception("Unable to download model " + name + " using " + urls.Count + " URL(s).");
            }

            _Logging.Info(_Header + "downloaded GGUF file for " + name);

            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                (byte[] md5, byte[] sha1, byte[] sha256) = HashHelper.ComputeAllHashes(fs);

                ModelFile created = _ModelFiles.Add(new ModelFile
                {
                    GUID = modelFile.GUID,
                    Name = name,
                    ContentLength = preferred.Size != null ? preferred.Size.Value : 0,
                    MD5Hash = Convert.ToHexString(md5),
                    SHA1Hash = Convert.ToHexString(sha1),
                    SHA256Hash = Convert.ToHexString(sha256),
                    Quantization = preferred.QuantizationType,
                    SourceUrl = successUrl,
                    ModelCreationUtc = preferred.LastModified,
                    CreatedUtc = DateTime.UtcNow
                });

                _Logging.Info(_Header + "successfully added model " + name + " using GUID " + created.GUID);
                return created;
            }
        }

        /// <summary>
        /// Delete.
        /// </summary>
        /// <param name="name">Model name.</param>
        public void Delete(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            ModelFile modelFile = _ModelFiles.GetByName(name);
            if (modelFile == null)
            {
                _Logging.Warn(_Header + "model " + name + " not found");
                throw new KeyNotFoundException("Model " + name + " was not found.");
            }
            else
            {
                _ModelFiles.Delete(modelFile.GUID);
                File.Delete(Path.Combine(_ModelDirectory, modelFile.GUID.ToString()));
                _Logging.Info(_Header + "successfully deleted model " + name + " in GUID " + modelFile.GUID);
            }
        }

        /// <summary>
        /// Get the engine for a given model.
        /// </summary>
        /// <param name="name">Model name.</param>
        /// <returns>Engine.</returns>
        public LlamaSharpEngine GetEngine(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            // Get the model file by name first
            ModelFile modelFile = _ModelFiles.GetByName(name);
            if (modelFile == null) throw new KeyNotFoundException($"Model '{name}' was not found."); 

            string modelFilePath = Path.Combine(_ModelDirectory, modelFile.GUID.ToString());
            return _ModelEngines.GetByModelFile(modelFilePath);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
