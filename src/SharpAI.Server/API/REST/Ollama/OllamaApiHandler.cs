﻿namespace SharpAI.Server.API.REST.Ollama
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using SharpAI.Engines;
    using SharpAI.Helpers;
    using SharpAI.Hosting;
    using SharpAI.Hosting.HuggingFace;
    using SharpAI.Models;
    using SharpAI.Models.Ollama;
    using SharpAI.Serialization;
    using SharpAI.Server.Classes.Settings;
    using SharpAI.Services;
    using SwiftStack;
    using SwiftStack.Rest;
    using SyslogLogging;

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

        private ConcurrentDictionary<string, bool> _Pulls = new ConcurrentDictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);

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

        internal async Task<object> PullModel(
            AppRequest req, 
            OllamaPullModelRequest pmr, 
            CancellationToken token = default)
        {
            string modelName = null;
            if (!String.IsNullOrEmpty(pmr.Name)) modelName = pmr.Name;
            if (!String.IsNullOrEmpty(pmr.Model)) modelName = pmr.Model;

            if (String.IsNullOrEmpty(modelName))
            {
                _Logging.Warn(_Header + "no model name supplied");

                req.Http.Response.StatusCode = 400;

                return new
                {
                    error = "invalid model name"
                };
            }

            #region Hold-Concurrent-Pulls

            int heldCount = 0;
            while (_Pulls.ContainsKey(modelName))
            {
                if (heldCount % 10 == 0)
                    _Logging.Debug(_Header + "holding pull request for " + modelName + " due to an existing pull");

                heldCount++;
                await Task.Delay(1000, token).ConfigureAwait(false);
            }

            _Pulls.TryAdd(modelName, true);

            #endregion

            try
            {
                #region Check-for-Existing

                ModelFile existing = _ModelFileService.GetByName(modelName);
                if (existing != null)
                {
                    _Logging.Debug(_Header + "model " + modelName + " already exists");

                    req.Http.Response.ContentType = Constants.JsonContentType;

                    return new OllamaPullModelResultMessage
                    {
                        Status = "success"
                    };
                }

                #endregion

                #region Identify-GGUF-Files

                req.Http.Response.ContentType = Constants.NdJsonContentType;
                req.Http.Response.ChunkedTransfer = true;

                List<GgufFileInfo> ggufFiles = await _HuggingFaceClient.GetGgufFilesAsync(modelName, token).ConfigureAwait(false);
                if (ggufFiles == null || ggufFiles.Count < 1)
                {
                    _Logging.Warn(_Header + "no GGUF files found for model " + modelName);

                    string notFound = _Serializer.SerializeJson(new
                    {
                        error = "pull model manifest: file does not exist"
                    }, false) + Environment.NewLine;

                    req.Http.Response.StatusCode = 404;
                    await req.Http.Response.SendChunk(Encoding.UTF8.GetBytes(notFound), true, token).ConfigureAwait(false);
                    return null;
                }

                string pullingManifest = _Serializer.SerializeJson(new
                {
                    status = "pulling manifest"
                }, false) + Environment.NewLine;

                await req.Http.Response.SendChunk(Encoding.UTF8.GetBytes(pullingManifest), false, token).ConfigureAwait(false);

                GgufFileInfo preferred = null;

                if (_Settings.QuantizationPriority == null || _Settings.QuantizationPriority.Count < 1)
                    preferred = GgufSelector.SortByOllamaPreference(ggufFiles).First();
                else
                    preferred = GgufSelector.SortByPreference(ggufFiles, _Settings.QuantizationPriority).First();

                _Logging.Debug(_Header + "using GGUF file " + preferred.Path + " as the preferred file for model " + modelName);

                #endregion

                #region Get-Download-URLs

                List<string> urls = _HuggingFaceClient.GetDownloadUrls(modelName, preferred);
                if (urls == null || urls.Count < 1)
                {
                    _Logging.Warn("no download URLs found for model " + modelName);
                    throw new SwiftStackException(ApiResultEnum.InternalError, "No download URLs found for the specified model " + modelName + ".");
                }

                string msg = _Header + "attempting download of model " + modelName + " from the following URLs:";
                foreach (string url in urls)
                {
                    msg += Environment.NewLine + "| " + url;
                }

                _Logging.Debug(_Header + msg);

                #endregion

                #region Download

                ModelFile modelFile = new ModelFile
                {
                    Name = modelName
                };

                bool success = false;
                string filename = null;
                string successUrl = null;

                Action<string, long, decimal> progressCallback = async (filename, bytesDownloaded, percentComplete) =>
                {
                    if (percentComplete > 0 && percentComplete < 1)
                    {
                        string complete = percentComplete.ToString("F3");

                        string progress = _Serializer.SerializeJson(new
                        {
                            status = "pulling " + modelName,
                            downloaded = bytesDownloaded,
                            percent = Convert.ToDecimal(complete)
                        }, false) + Environment.NewLine;

                        await req.Http.Response.SendChunk(Encoding.UTF8.GetBytes(progress), false, token).ConfigureAwait(false);
                    }
                };

                long fileLength = 0;

                filename = Path.Combine(_Settings.Storage.ModelsDirectory, modelFile.GUID.ToString());

                foreach (string url in urls)
                {
                    _Logging.Debug(_Header + "attempting download of model " + modelName + " using URL " + url + " to file " + modelFile.GUID.ToString());

                    success = await _HuggingFaceClient.TryDownloadFileAsync(url, filename, progressCallback, token).ConfigureAwait(false);
                    if (success)
                    {
                        fileLength = new FileInfo(filename).Length;
                        if (File.Exists(filename) && new FileInfo(filename).Length == preferred.Size)
                        {
                            _Logging.Info(_Header + "successfully downloaded model " + modelName + " using URL " + url + " to file " + filename);
                            successUrl = url;
                            success = true;
                            break;
                        }
                        else
                        {
                            success = false;
                        }
                    }
                }

                if (!success || String.IsNullOrEmpty(filename))
                {
                    _Logging.Warn(_Header + "unable to download model " + modelName + " using " + urls.Count + " URL(s)");
                    throw new SwiftStackException(ApiResultEnum.InternalError, "Unable to download model " + modelName + " using " + urls.Count + " URL(s).");
                }

                _Logging.Info(_Header + "downloaded GGUF file for " + modelName);

                #endregion

                #region Persist

                using (LlamaSharpEngine engine = _ModelEngineService.GetByModelFile(Path.Combine(_Settings.Storage.ModelsDirectory, modelFile.GUID.ToString())))
                {
                    modelFile.Embeddings = engine.SupportsEmbeddings;
                    modelFile.Completions = engine.SupportsGeneration;
                }

                using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    (byte[] md5, byte[] sha1, byte[] sha256) = HashHelper.ComputeAllHashes(fs);

                    string writingManifest = _Serializer.SerializeJson(new
                    {
                        status = "writing manifest",
                        downloaded = fileLength,
                        percent = 1
                    }, false) + Environment.NewLine;

                    await req.Http.Response.SendChunk(Encoding.UTF8.GetBytes(writingManifest), false, token).ConfigureAwait(false);

                    HuggingFaceModelMetadata md = null;

                    try
                    {
                        md = await _HuggingFaceClient.GetModelMetadata(modelName, token).ConfigureAwait(false);
                        if (md == null)
                        {
                            _Logging.Warn(_Header + "unable to retrieve metadata for " + modelName);
                            throw new SwiftStackException(ApiResultEnum.InternalError, "Unable to retrieve metadata for model '" + modelName + "'.");
                        }
                    }
                    catch (Exception e)
                    {
                        _Logging.Warn(_Header + "exception retrieving model metadata:" + Environment.NewLine + e.ToString());
                    }

                    long parameterCount = 0;
                    if (md.SafeTensors != null) parameterCount = md.SafeTensors.Total;

                    modelFile.ContentLength = preferred.Size != null ? preferred.Size.Value : 0;
                    modelFile.MD5Hash = Convert.ToHexString(md5);
                    modelFile.SHA1Hash = Convert.ToHexString(sha1);
                    modelFile.SHA256Hash = Convert.ToHexString(sha256);
                    modelFile.Quantization = preferred.QuantizationType;
                    modelFile.ParameterCount = parameterCount;
                    modelFile.ModelCreationUtc = preferred.LastModified;
                    modelFile.SourceUrl = successUrl;

                    _ModelFileService.Add(modelFile);

                    req.Http.Response.ContentType = Constants.JsonContentType;

                    string complete = _Serializer.SerializeJson(new
                    {
                        status = "success",
                        downloaded = fileLength,
                        percent = 1
                    }, false) + Environment.NewLine;

                    _Logging.Info(_Header + "successfully pulled model " + modelName);

                    await req.Http.Response.SendChunk(Encoding.UTF8.GetBytes(complete), true, token).ConfigureAwait(false);

                    return null;
                }

                #endregion
            }
            catch (KeyNotFoundException)
            {
                _Logging.Warn(_Header + "unable to find repository or GGUF files for " + pmr.Model);

                string notFound = _Serializer.SerializeJson(new
                {
                    error = "pull model manifest: file does not exist"
                }, false) + Environment.NewLine;

                await req.Http.Response.SendChunk(Encoding.UTF8.GetBytes(notFound), true, token).ConfigureAwait(false);
                return null;
            }
            finally
            {
                _Pulls.TryRemove(pmr.Model, out _);
            }
        }

        internal async Task<object> DeleteModel(
            AppRequest req, 
            OllamaDeleteModelRequest dmr, 
            CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(dmr.Model)) throw new ArgumentNullException(nameof(dmr.Model));

            req.Http.Response.ContentType = Constants.JsonContentType;

            ModelFile modelFile = _ModelFileService.GetByName(dmr.Model);
            if (modelFile == null)
            {
                _Logging.Warn(_Header + "model " + dmr.Model + " not found");

                req.Http.Response.StatusCode = 404;

                return new
                {
                    error = $"model '{dmr.Model}' not found"
                };
            }
            else
            {
                _ModelFileService.Delete(modelFile.GUID);
                File.Delete(Path.Combine(_Settings.Storage.ModelsDirectory, modelFile.GUID.ToString()));
                return null;
            }
        }

        internal async Task<object> ListLocalModels(
            AppRequest req, 
            CancellationToken token = default)
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

        internal async Task<object> GenerateEmbeddings(
            AppRequest req, 
            OllamaGenerateEmbeddingsRequest ger, 
            CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(ger.Model)) throw new ArgumentNullException(nameof(ger.Model));

            req.Http.Response.ContentType = Constants.JsonContentType;

            OllamaGenerateEmbeddingsResult ret = new OllamaGenerateEmbeddingsResult
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

            if (ger.IsSingleInput())
            {
                string input = ger.GetInput();

                float[][] embeddings = new float[1][];

                ret.Embeddings = new float[1][];

                if (!String.IsNullOrEmpty(input))
                {
                    embeddings[0] = await engine.GenerateEmbeddingsAsync(input, token).ConfigureAwait(false);
                    ret.Embeddings = embeddings;
                }
            }
            else
            {
                string[] inputs = ger.GetInputs();

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

                ret.Embeddings = await engine.GenerateEmbeddingsAsync(inputs.ToArray(), token).ConfigureAwait(false);
            }

            return ret;
        }

        internal async Task<object> GenerateCompletion(
            AppRequest req, 
            OllamaGenerateCompletionRequest gcr, 
            CancellationToken token = default)
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

            if (gcr.Stream == null || !gcr.Stream.Value)
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

        internal async Task<object> GenerateChatCompletion(
            AppRequest req, 
            OllamaGenerateChatCompletionRequest gcr, 
            CancellationToken token = default)
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
                foreach (OllamaChatMessage msg in gcr.Messages)
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

            if (gcr.Stream == null || !gcr.Stream.Value)
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
