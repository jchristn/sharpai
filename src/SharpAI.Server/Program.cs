namespace SharpAI.Server
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using SharpAI;
    using SharpAI.Engines;
    using SharpAI.Hosting;
    using SharpAI.Models.Ollama;
    using SharpAI.Serialization;
    using SharpAI.Server.API.REST.Ollama;
    using SharpAI.Server.Classes;
    using SharpAI.Server.Classes.Settings;
    using SharpAI.Services;
    using SwiftStack;
    using SwiftStack.Rest;
    using SyslogLogging;
    using Watson.ORM.Sqlite;
    
    using Constants = SharpAI.Constants;

    /// <summary>
    /// SharpAI Server.  We are happy to see you.
    /// </summary>
    public static class Program
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Public-Members

        #endregion

        #region Private-Members

        private static string _Header = "[SharpAI] ";
        private static string _Version = "0.1.0";
        private static Serializer _Serializer = new Serializer();
        private static Settings _Settings = null;
        private static LoggingModule _Logging = null;
        private static WatsonORM _ORM = null;

        private static ModelFileService _ModelFileService = null;
        private static ModelEngineService _ModelEngineService = null;

        private static HuggingFaceClient _HuggingFaceClient = null;
        private static SwiftStackApp _App = null;
        private static OllamaApiHandler _OllamaApiHandler = null;
        private static CancellationTokenSource _TokenSource = new CancellationTokenSource();

        #endregion

        #region Entrypoint

        /// <summary>
        /// SharpAI Server.  We are happy to see you.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <returns>Task.</returns>
        public static async Task Main(string[] args)
        {
            Welcome();
            ParseArguments(args);
            LoadSettings();
            InitializeGlobals();
            InitializeRestServer();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _TokenSource.Cancel();
                _Logging.Debug(_Header + "shutdown requested");
            };

            _Logging.Debug(_Header + "starting SharpAI server");
            await _App.Rest.Run(_TokenSource.Token);
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        private static void Welcome()
        {
            Console.WriteLine("");
            Console.WriteLine(Constants.Logo);
            Console.WriteLine(" SharpAI Server v" + _Version);
            Console.WriteLine(" (c)2025 Joel Christner");
            Console.WriteLine("");
        }

        private static void ParseArguments(string[] args)
        {

        }

        private static void LoadSettings()
        {
            if (!File.Exists(Constants.SettingsFile))
            {
                Console.WriteLine("Settings file " + Constants.SettingsFile + " does not exist, creating");

                _Settings = new Settings();
                _Settings.SoftwareVersion = _Version;

                _Serializer.SerializeJsonToFile(Constants.SettingsFile, _Settings, true);
            }
            else
            {
                _Settings = _Serializer.DeserializeJsonFromFile<Settings>(Constants.SettingsFile);
            }

            if (_Settings.HuggingFace == null
                || String.IsNullOrEmpty(_Settings.HuggingFace.ApiKey)
                || _Settings.HuggingFace.ApiKey.Equals(Constants.DefaultHuggingFaceApiKey))
            {
                Console.WriteLine();
                Console.WriteLine("Modify the " + Constants.SettingsFile + " file to set your HuggingFace API key, exiting");
                Console.WriteLine();
                Environment.Exit(1);
            }
        }

        private static void InitializeGlobals()
        {
            #region Logging

            List<SyslogLogging.SyslogServer> servers = new List<SyslogLogging.SyslogServer>();

            if (_Settings.Logging.Servers != null && _Settings.Logging.Servers.Count > 0)
            {
                foreach (SharpAI.Server.Classes.Settings.SyslogServer server in _Settings.Logging.Servers)
                {
                    servers.Add(new SyslogLogging.SyslogServer(server.Hostname, server.Port));
                }
            }

            if (!Directory.Exists(_Settings.Logging.LogDirectory)) Directory.CreateDirectory(_Settings.Logging.LogDirectory);

            _Logging = new LoggingModule(servers, _Settings.Logging.ConsoleLogging);
            _Logging.Settings.FileLogging = FileLoggingMode.FileWithDate;
            _Logging.Settings.LogFilename = _Settings.Logging.LogDirectory + _Settings.Logging.LogFilename;

            #endregion

            #region ORM

            _ORM = new WatsonORM(_Settings.Database);

            _ORM.InitializeDatabase();
            _ORM.InitializeTables(new List<Type>
            {
                typeof(Models.ModelFile)
            });

            #endregion

            #region Services

            _ModelFileService = new ModelFileService(_Logging, _ORM, _Settings.Storage.ModelsDirectory);
            _ModelEngineService = new ModelEngineService(_Logging);
            _HuggingFaceClient = new HuggingFaceClient(_Logging, _Settings.HuggingFace.ApiKey);

            #endregion

            #region Handlers

            _OllamaApiHandler = new OllamaApiHandler(
                _Settings, 
                _Logging, 
                _Serializer, 
                _ModelFileService, 
                _ModelEngineService,
                _HuggingFaceClient);

            #endregion
        }

        private static void InitializeRestServer()
        {
            _App = new SwiftStackApp("SharpAI Server", true); // quiet
            _App.Rest.WebserverSettings = _Settings.Rest;

            #region General-Routes

            _App.Rest.ExceptionRoute = async (req, e) =>
            {
                Type exType = e.GetType();

                _Logging.Warn(_Header + "exception of type " + exType.Name + ": " + Environment.NewLine + e.ToString());

                switch (e)
                {
                    case KeyNotFoundException:
                        req.Response.StatusCode = 404;
                        throw new SwiftStackException(ApiResultEnum.NotFound, e.Message);
                    case ArgumentNullException:
                    case ArgumentException:
                    case InvalidOperationException:
                        req.Response.StatusCode = 400;
                        throw new SwiftStackException(ApiResultEnum.BadRequest, e.Message);
                    default:
                        req.Response.StatusCode = 500;
                        throw new SwiftStackException(ApiResultEnum.InternalError, e.Message);
                }
            };

            _App.Rest.Get("/", async (req) =>
            {
                req.Http.Response.ContentType = Constants.HtmlContentType;
                return Constants.HtmlHomepage;
            }, false);

            _App.Rest.Head("/", async (req) => null, false);

            _App.Rest.Head("/favicon.ico", async (req) => null, false);

            _App.Rest.Get("/favicon.ico", async (req) =>
            {
                req.Http.Response.ContentType = Constants.FaviconContentType;
                return File.ReadAllBytes(Constants.FaviconFilename);
            }, false);

            #endregion

            #region Ollama-Endpoints

            _App.Rest.Post<PullModelRequest>("/api/pull", async (req) =>
            {
                PullModelRequest pmr = req.GetData<PullModelRequest>();
                return await _OllamaApiHandler.PullModel(req, pmr, _TokenSource.Token).ConfigureAwait(false);
            }, false); // pull a model

            _App.Rest.Delete<DeleteModelRequest>("/api/delete", async (req) =>
            {
                DeleteModelRequest dmr = req.GetData<DeleteModelRequest>();
                return await _OllamaApiHandler.DeleteModel(req, dmr, _TokenSource.Token).ConfigureAwait(false);
            }, false); // delete a model

            _App.Rest.Get("/api/tags", async (req) =>
            {
                return await _OllamaApiHandler.ListLocalModels(req, _TokenSource.Token).ConfigureAwait(false);
            }, false); // list local models

            #endregion

            #region Embeddings

            _App.Rest.Post<GenerateEmbeddingsRequest>("/api/embed", async (req) =>
            {
                GenerateEmbeddingsRequest ger = req.GetData<GenerateEmbeddingsRequest>();
                return await _OllamaApiHandler.GenerateEmbeddings(req, ger, _TokenSource.Token).ConfigureAwait(false);
            }, false); // generate embeddings (single, multiple)

            #endregion

            #region Completions

            _App.Rest.Post<GenerateCompletionRequest>("/api/generate", async (req) =>
            {
                GenerateCompletionRequest gcr = req.GetData<GenerateCompletionRequest>();
                object ret = await _OllamaApiHandler.GenerateCompletion(req, gcr, _TokenSource.Token).ConfigureAwait(false);
                if (req.Http.Response.ChunkedTransfer) return null;
                else return ret;
            }, false); // generate text

            _App.Rest.Post<GenerateChatCompletionRequest>("/api/chat", async (req) =>
            {
                GenerateChatCompletionRequest gccr = req.GetData<GenerateChatCompletionRequest>();
                object ret = await _OllamaApiHandler.GenerateChatCompletion(req, gccr, _TokenSource.Token).ConfigureAwait(false);
                if (req.Http.Response.ChunkedTransfer) return null;
                else return ret;
            }, false); // generate chat completion

            #endregion
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}