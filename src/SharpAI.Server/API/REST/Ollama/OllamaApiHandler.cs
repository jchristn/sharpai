namespace SharpAI.Server.API.REST.Ollama
{
    using SharpAI.Engines;
    using SharpAI.Helpers;
    using SharpAI.Hosting;
    using SharpAI.Models;
    using SharpAI.Models.Ollama;
    using SharpAI.Serialization;
    using SharpAI.Server.Classes.Settings;
    using SharpAI.Services;
    using SwiftStack;
    using SwiftStack.Rest;
    using SyslogLogging;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Constants = SharpAI.Constants;

    internal class OllamaApiHandler
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Public-Members

        #endregion

        #region Private-Members

        private string _Header = "[OllamaApiHandler] ";
        private Settings _Settings = null;
        private LoggingModule _Logging = null;
        private Serializer _Serializer = null;
        private ModelFileService _ModelFileService = null;
        private ModelEngineService _ModelEngineService = null;
        private HuggingFaceClient _HuggingFaceClient = null;

        private static string _TimestampFormat = "yyyy-MM-ddTHH:mm:ss.ffffffZ";

        #endregion

        #region Constructors-and-Factories

        internal OllamaApiHandler(
            Settings settings,
            LoggingModule logging,
            Serializer serializer,
            ModelFileService modelFileService,
            ModelEngineService modelEngineService,
            HuggingFaceClient huggingFaceClient)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _ModelFileService = modelFileService ?? throw new ArgumentNullException(nameof(modelFileService));
            _ModelEngineService = modelEngineService ?? throw new ArgumentNullException(nameof(modelEngineService));
            _HuggingFaceClient = huggingFaceClient ?? throw new ArgumentNullException(nameof(huggingFaceClient));

            _Logging.Debug(_Header + "initialized");
        }

        #endregion

        #region Public-Methods

        internal async Task<object> PullModel(AppRequest req, PullModelRequest pmr, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(pmr.Model)) throw new ArgumentNullException(nameof(pmr.Model));

            #region Check-for-Existing

            ModelFile existing = _ModelFileService.GetByName(pmr.Model);
            if (existing != null)
            {
                _Logging.Debug(_Header + "model " + pmr.Model + " already exists");

                req.Http.Response.ContentType = Constants.JsonContentType;

                return new PullModelStatus
                {
                    Status = "success"
                };
            }

            #endregion

            #region Identify-GGUF-Files

            List<GgufFileInfo> ggufFiles = await _HuggingFaceClient.GetGgufFilesAsync(pmr.Model, token).ConfigureAwait(false);
            if (ggufFiles == null || ggufFiles.Count < 1)
            {
                _Logging.Warn(_Header + "no GGUF files found for model " + pmr.Model);
                throw new SwiftStackException(ApiResultEnum.InternalError, "No GGUF files found for the specified model " + pmr.Model + ".");
            }

            GgufFileInfo preferred = GgufSelector.SortByOllamaPreference(ggufFiles).First();
            _Logging.Debug(_Header + "using GGUF file " + preferred.Path + " as the preferred file for model " + pmr.Model);

            #endregion

            #region Get-Download-URLs

            List<string> urls = _HuggingFaceClient.GetDownloadUrls(pmr.Model, preferred);
            if (urls == null || urls.Count < 1)
            {
                _Logging.Warn("no download URLs found for model " + pmr.Model);
                throw new SwiftStackException(ApiResultEnum.InternalError, "No download URLs found for the specified model " + pmr.Model + ".");
            }

            string msg = _Header + "attempting download of model " + pmr.Model + " from the following URLs:";
            foreach (string url in urls)
            {
                msg += Environment.NewLine + "| " + url;
            }

            _Logging.Debug(_Header + msg);

            #endregion

            #region Download

            ModelFile modelFile = new ModelFile
            {
                Name = pmr.Model
            };

            bool success = false;
            string filename = null;
            string successUrl = null;

            foreach (string url in urls)
            {
                filename = Path.Combine(_Settings.Storage.ModelsDirectory, modelFile.GUID.ToString());
                _Logging.Debug(_Header + "attempting download of model " + pmr.Model + " using URL " + url + " to file " + modelFile.GUID.ToString());
                
                success = await _HuggingFaceClient.TryDownloadFileAsync(url, filename, token).ConfigureAwait(false);
                if (success && File.Exists(filename) && new FileInfo(filename).Length == preferred.Size)
                {
                    _Logging.Info(_Header + "successfully downloaded model " + pmr.Model + " using URL " + url + " to file " + filename);
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
                _Logging.Warn(_Header + "unable to download model " + pmr.Model + " using " + urls.Count + " URL(s)");
                throw new SwiftStackException(ApiResultEnum.InternalError, "Unable to download model " + pmr.Model + " using " + urls.Count + " URL(s).");
            }

            _Logging.Info(_Header + "downloaded GGUF file for " + pmr.Model);

            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                (byte[] md5, byte[] sha1, byte[] sha256) = HashHelper.ComputeAllHashes(fs);

                _ModelFileService.Add(new ModelFile
                {
                    GUID = modelFile.GUID,
                    Name = pmr.Model,
                    ContentLength = preferred.Size != null ? preferred.Size.Value : 0,
                    MD5Hash = Convert.ToHexString(md5),
                    SHA1Hash = Convert.ToHexString(sha1),
                    SHA256Hash = Convert.ToHexString(sha256),
                    Quantization = preferred.QuantizationType,
                    SourceUrl = successUrl,
                    ModelCreationUtc = preferred.LastModified,
                    CreatedUtc = DateTime.UtcNow
                });

                req.Http.Response.ContentType = Constants.JsonContentType;

                return new 
                {
                    status = "success"
                };
            }

            #endregion
        }

        internal async Task<object> DeleteModel(AppRequest req, DeleteModelRequest dmr, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(dmr.Name)) throw new ArgumentNullException(nameof(dmr.Name));

            req.Http.Response.ContentType = Constants.JsonContentType;

            ModelFile modelFile = _ModelFileService.GetByName(dmr.Name);
            if (modelFile == null)
            {
                _Logging.Warn(_Header + "model " + dmr.Name + " not found");

                req.Http.Response.StatusCode = 404;

                return new
                {
                    error = $"model '{dmr.Name}' not found"
                };
            }
            else
            {
                _ModelFileService.Delete(modelFile.GUID);
                File.Delete(Path.Combine(_Settings.Storage.ModelsDirectory, modelFile.GUID.ToString()));
                return null;
            }
        }

        internal async Task<object> ListLocalModels(AppRequest req, CancellationToken token = default)
        {
            List<ModelFile> modelFiles = _ModelFileService.All();
            if (modelFiles == null || modelFiles.Count < 1)
            {
                _Logging.Debug(_Header + "no models downloaded");

                return new
                {
                    models = new List<string>()
                };
            }

            List<object> ret = new List<object>();

            foreach (ModelFile modelFile in modelFiles)
            {
                ret.Add(modelFile.ToOllamaModelDetails());
            }

            return ret;
        }

        internal async Task<object> GenerateEmbeddings(AppRequest req, GenerateEmbeddingsRequest ger, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(ger.Model)) throw new ArgumentNullException(nameof(ger.Model));

            req.Http.Response.ContentType = Constants.JsonContentType;

            GenerateEmbeddingsResult ret = new GenerateEmbeddingsResult
            {
                Model = ger.Model
            };

            ModelFile modelFile = _ModelFileService.GetByName(ger.Model);
            if (modelFile == null)
            {
                _Logging.Warn(_Header + "model " + ger.Model + " not found");

                req.Http.Response.StatusCode = 404;

                return new
                {
                    error = $"model '{ger.Model}' not found, try pulling it first"
                };
            }

            LlamaSharpEngine engine = _ModelEngineService.GetByModelFile(Path.Combine(_Settings.Storage.ModelsDirectory, modelFile.GUID.ToString()));

            if (!engine.SupportsEmbeddings)
            {
                _Logging.Warn(_Header + "model '" + ger.Model + "' does not support embeddings");

                req.Http.Response.StatusCode = 500;

                return new
                {
                    error = $"model '{ger.Model}' does not support embeddings"
                };
            }

            if (ger.IsInputSingleton)
            {
                string input = ger.GetInputAsSingleton();
                ret.Embeddings = new float[1][];

                if (!String.IsNullOrEmpty(input))
                {
                    ret.Embeddings[0] = await engine.GenerateEmbeddingsAsync(input, token).ConfigureAwait(false);
                }
            }
            else
            {
                string[] inputs = ger.GetInputAsArray();

                foreach (string input in inputs)
                {
                    if (String.IsNullOrEmpty(input))
                    {
                        _Logging.Warn(_Header + "input contains null or invalid entries");

                        req.Http.Response.StatusCode = 400;

                        return new
                        {
                            error = $"invalid input type"
                        };
                    }
                }

                ret.Embeddings = await engine.GenerateEmbeddingsAsync(inputs, token).ConfigureAwait(false);
            }

            return ret;
        }

        internal async Task<object> GenerateCompletion(AppRequest req, GenerateCompletionRequest gcr, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(gcr.Model)) throw new ArgumentNullException(nameof(gcr.Model));

            req.Http.Response.ContentType = Constants.JsonContentType;

            ModelFile modelFile = _ModelFileService.GetByName(gcr.Model);
            if (modelFile == null)
            {
                _Logging.Warn(_Header + "model " + gcr.Model + " not found");

                req.Http.Response.StatusCode = 404;

                return new
                {
                    error = $"model '{gcr.Model}' not found"
                };
            }

            LlamaSharpEngine engine = _ModelEngineService.GetByModelFile(Path.Combine(_Settings.Storage.ModelsDirectory, modelFile.GUID.ToString()));

            string json = null;

            if (!engine.SupportsGeneration)
            {
                _Logging.Warn(_Header + "'" + gcr.Model + "' does not support generate");

                req.Http.Response.StatusCode = 400;

                return new
                {
                    error = $"model '{gcr.Model}' does not support generate"
                };
            }

            if (!gcr.Stream)
            {
                string response = await engine.GenerateTextAsync(
                    gcr.Prompt,
                    gcr.Options.NumPredict != null ? gcr.Options.NumPredict.Value : 128,
                    gcr.Options.Temperature != null ? gcr.Options.Temperature.Value : 0.6f,
                    null,
                    token).ConfigureAwait(false);

                return new
                {
                    model = gcr.Model,
                    created_at = DateTime.UtcNow.ToString(_TimestampFormat),
                    response = response,
                    done = true,
                    done_reason = "stop"
                };
            }
            else
            {
                string nextToken = null;

                req.Http.Response.ContentType = Constants.NdJsonContentType;
                req.Http.Response.ChunkedTransfer = true;

                await foreach (string curr in engine.GenerateTextStreamAsync(
                    gcr.Prompt,
                    gcr.Options.NumPredict != null ? gcr.Options.NumPredict.Value : 128,
                    gcr.Options.Temperature != null ? gcr.Options.Temperature.Value : 0.6f,
                    null,
                    token).ConfigureAwait(false))
                {
                    if (nextToken != null)
                    {
                        json = _Serializer.SerializeJson(new
                        {
                            model = gcr.Model,
                            created_at = DateTime.UtcNow.ToString(_TimestampFormat),
                            response = nextToken,
                            done = false

                        }, false) + Environment.NewLine;

                        await req.Http.Response.SendChunk(Encoding.UTF8.GetBytes(json), false, token).ConfigureAwait(false);
                    }

                    nextToken = curr;
                }

                json = _Serializer.SerializeJson(new
                {
                    model = gcr.Model,
                    created_at = DateTime.UtcNow.ToString(_TimestampFormat),
                    response = nextToken,
                    done = true,
                    done_reason = "stop"

                }, false);

                await req.Http.Response.SendChunk(Encoding.UTF8.GetBytes(json), true, token).ConfigureAwait(false);

                return null;
            }
        }

        internal async Task<object> GenerateChatCompletion(AppRequest req, GenerateChatCompletionRequest gcr, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(gcr.Model)) throw new ArgumentNullException(nameof(gcr.Model));

            req.Http.Response.ContentType = Constants.JsonContentType;

            ModelFile modelFile = _ModelFileService.GetByName(gcr.Model);
            if (modelFile == null)
            {
                _Logging.Warn(_Header + "model " + gcr.Model + " not found");

                req.Http.Response.StatusCode = 404;

                return new
                {
                    error = $"model '{gcr.Model}' not found"
                };
            }

            LlamaSharpEngine engine = _ModelEngineService.GetByModelFile(Path.Combine(_Settings.Storage.ModelsDirectory, modelFile.GUID.ToString()));

            string json = null;
            StringBuilder promptBuilder = new StringBuilder();
            if (gcr.Messages != null && gcr.Messages.Count > 0)
            {
                int added = 0;
                foreach (Message msg in gcr.Messages)
                {
                    if (added > 0) promptBuilder.Append("\n");
                    promptBuilder.Append($"{msg.Role}: {msg.Content}"); 
                }
            }

            if (!engine.SupportsGeneration)
            {
                _Logging.Warn(_Header + "'" + gcr.Model + "' does not support generate");

                req.Http.Response.StatusCode = 400;

                return new
                {
                    error = $"model '{gcr.Model}' does not support generate"
                };
            }

            if (!gcr.Stream)
            {
                string response = await engine.GenerateChatCompletionAsync(
                    promptBuilder.ToString(),
                    gcr.Options.NumPredict != null ? gcr.Options.NumPredict.Value : 128,
                    gcr.Options.Temperature != null ? gcr.Options.Temperature.Value : 0.6f,
                    null,
                    token).ConfigureAwait(false);

                return new
                {
                    model = gcr.Model,
                    created_at = DateTime.UtcNow.ToString(_TimestampFormat),
                    response = response,
                    done = true,
                    done_reason = "stop"
                };
            }
            else
            {
                string nextToken = null;

                req.Http.Response.ContentType = Constants.NdJsonContentType;
                req.Http.Response.ChunkedTransfer = true;

                await foreach (string curr in engine.GenerateChatCompletionStreamAsync(
                    promptBuilder.ToString(),
                    gcr.Options.NumPredict != null ? gcr.Options.NumPredict.Value : 128,
                    gcr.Options.Temperature != null ? gcr.Options.Temperature.Value : 0.6f,
                    null,
                    token).ConfigureAwait(false))
                {
                    if (nextToken != null)
                    {
                        json = _Serializer.SerializeJson(new
                        {
                            model = gcr.Model,
                            created_at = DateTime.UtcNow.ToString(_TimestampFormat),
                            response = nextToken,
                            done = false

                        }, false) + Environment.NewLine;

                        await req.Http.Response.SendChunk(Encoding.UTF8.GetBytes(json), false, token).ConfigureAwait(false);
                    }

                    nextToken = curr;
                }

                json = _Serializer.SerializeJson(new
                {
                    model = gcr.Model,
                    created_at = DateTime.UtcNow.ToString(_TimestampFormat),
                    response = nextToken,
                    done = true,
                    done_reason = "stop"

                }, false) + Environment.NewLine;

                await req.Http.Response.SendChunk(Encoding.UTF8.GetBytes(json), true, token).ConfigureAwait(false);

                return null;
            }
        }

        #endregion

        #region Private-Methods

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
