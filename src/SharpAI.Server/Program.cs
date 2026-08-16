namespace SharpAI.Server
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using SharpAI;
    using SharpAI.Database;
    using SharpAI.Engines;
    using SharpAI.Classes.Runtime;
    using SharpAI.Hosting;
    using SharpAI.Models.Ollama;
    using SharpAI.Models.OpenAI;
    using SharpAI.Serialization;
    using SharpAI.Server.API.REST.Ollama;
    using SharpAI.Server.API.REST.OpenAI;
    using SharpAI.Server.Classes.Runtime;
    using SharpAI.Server.Classes.Settings;
    using SharpAI.Services;
    using SyslogLogging;
    using WatsonWebserver;
    using WatsonWebserver.Core;
    using WatsonWebserver.Core.OpenApi;

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
        private static string _Version = "5.0.0";
        private static Serializer _Serializer = new Serializer();
        private static Settings _Settings = null;
        private static LoggingModule _Logging = null;
        private static DatabaseDriverBase _Database = null;
        private static SharpAI.Server.Classes.Runtime.TelemetryHost _TelemetryHost = null;
        private static SharpAI.Server.Classes.Runtime.RequestHistoryCaptureService _RequestHistoryCapture = null;
        private static SharpAI.Server.Classes.Runtime.AuthenticationService _AuthService = null;
        private static SharpAI.Security.SessionTokenService _SessionTokens = null;
        private static SharpAI.Security.AuthenticationEngine _AuthEngine = null;
        private static SharpAI.Security.RbacEngine _RbacEngine = null;
        private static Timer _PruneTimer = null;

        private static ModelFileService _ModelFileService = null;
        private static ModelEngineService _ModelEngineService = null;

        private static HuggingFaceClient _HuggingFaceClient = null;
        private static Webserver _Server = null;
        private static OllamaApiHandler _OllamaApiHandler = null;
        private static OpenAIApiHandler _OpenAIApiHandler = null;
        private static CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private static bool _ShutdownRequested = false;

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
            InitializeLogging();
            InitializeTelemetry();
            InitializeBootstrapper();
            InitializeGlobals();
            InitializeRestServer();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;

                if (!_ShutdownRequested)
                {
                    _ShutdownRequested = true;
                    _TokenSource.Cancel();
                    _Logging.Debug(_Header + "shutdown requested");
                }
            };

            _Logging.Debug(_Header + "starting SharpAI server");
            _Server.Start();

            // Hourly request-history retention prune (first run after 5 minutes).
            _PruneTimer = new Timer(_ => PruneRequestHistory(), null, (int)TimeSpan.FromMinutes(5).TotalMilliseconds, (int)TimeSpan.FromHours(1).TotalMilliseconds);

            // Fire-and-forget: re-detect capabilities for existing models so the
            // DB reflects the authoritative GGUF-derived values. This runs in the
            // background so it doesn't delay server startup.
            _ = Task.Run(() => RedetectModelCapabilitiesAsync(_TokenSource.Token));

            try
            {
                await Task.Delay(Timeout.Infinite, _TokenSource.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // graceful shutdown
            }

            _Server.Stop();
            _Server.Dispose();
            _PruneTimer?.Dispose();
            _ModelEngineService?.Dispose();
            _Database?.Dispose();
            _TelemetryHost?.Dispose();
        }

        private static void PruneRequestHistory()
        {
            try
            {
                if (_Settings == null || _Settings.RequestHistory == null || !_Settings.RequestHistory.Enabled) return;
                if (_Database == null || !_Database.IsInitialized) return;

                int removed = _Database.RequestHistory.Prune(DateTime.UtcNow.AddDays(-_Settings.RequestHistory.RetentionDays));
                if (removed > 0) _Logging.Debug(_Header + "pruned " + removed + " request history row(s)");
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "request history prune failed:" + Environment.NewLine + ex.ToString());
            }
        }

        private static void InitializeTelemetry()
        {
            _TelemetryHost = new SharpAI.Server.Classes.Runtime.TelemetryHost(_Settings.Telemetry, _Logging);
        }

        private static void SeedAuthentication()
        {
            try
            {
                if (_Database == null || !_Database.IsInitialized) return;

                SharpAI.Security.Tenant tenant = _Database.Tenants.GetByName("default");
                if (tenant == null)
                {
                    tenant = _Database.Tenants.Create(new SharpAI.Security.Tenant
                    {
                        Name = "default",
                        IsProtected = true
                    });
                    _Logging.Info(_Header + "seeded default tenant " + tenant.Guid);
                }

                SharpAI.Security.User admin = _Database.Users.GetByEmail(tenant.Guid, "admin@sharpai.local");
                if (admin == null)
                {
                    string initialPassword = Guid.NewGuid().ToString("N").Substring(0, 16);
                    _Database.Users.Create(new SharpAI.Security.User
                    {
                        TenantGuid = tenant.Guid,
                        FirstName = "Administrator",
                        LastName = "Account",
                        Email = "admin@sharpai.local",
                        PasswordSha256 = SharpAI.Security.PasswordHasher.Hash(initialPassword),
                        IsAdmin = true,
                        IsTenantAdmin = true,
                        IsProtected = true
                    });

                    _Logging.Warn(
                        _Header + "seeded default administrator 'admin@sharpai.local' with initial password '" +
                        initialPassword + "' — change it after first login (this is shown only once)");
                }
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "authentication seeding failed:" + Environment.NewLine + ex.ToString());
            }
        }

        private static async Task RedetectModelCapabilitiesAsync(CancellationToken token)
        {
            try
            {
                System.Collections.Generic.List<Models.ModelFile> all = CollectAllModels();
                if (all == null || all.Count == 0)
                {
                    _Logging.Debug(_Header + "capability detection: no local models to inspect");
                    return;
                }

                _Logging.Info(_Header + "capability detection: starting for " + all.Count + " local model(s); " +
                    "reading GGUF metadata to determine embedding vs completion support");

                int inspected = 0;
                int updated = 0;
                int unchanged = 0;
                int skipped = 0;
                int failed = 0;

                foreach (Models.ModelFile mf in all)
                {
                    if (token.IsCancellationRequested)
                    {
                        _Logging.Warn(_Header + "capability detection: cancelled after " + inspected + " of " + all.Count + " model(s)");
                        return;
                    }

                    string path = Path.Combine(_Settings.Storage.ModelsDirectory, mf.GUID.ToString());
                    if (!File.Exists(path))
                    {
                        _Logging.Warn(_Header + "capability detection: skipping '" + mf.Name + "' - GGUF file missing at " + path);
                        skipped++;
                        continue;
                    }

                    try
                    {
                        _Logging.Debug(_Header + "capability detection: inspecting '" + mf.Name + "' (" + path + ")");

                        string detectedArch = null;
                        bool embeddings = false;
                        bool completions = true;
                        bool detectedViaMetadata = false;

                        // Try lightweight GGUF header reader first
                        try
                        {
                            SharpAI.Helpers.GgufMetadataReader.DetectCapabilities(
                                path,
                                out detectedArch,
                                out embeddings,
                                out completions);
                            detectedViaMetadata = true;
                        }
                        catch (Exception metaEx)
                        {
                            _Logging.Warn(_Header + "capability detection: lightweight read failed for '" + mf.Name +
                                "', falling back to full model load:" + Environment.NewLine + metaEx.ToString());
                        }

                        // Fall back to full engine initialization
                        if (!detectedViaMetadata)
                        {
                            using (LlamaSharpEngine engine = _ModelEngineService.GetByModelFile(path))
                            {
                                detectedArch = engine.Architecture;
                                embeddings = engine.SupportsEmbeddings;
                                completions = engine.SupportsGeneration;
                            }
                        }

                        string arch = detectedArch ?? "unknown";

                        string capabilityDesc =
                            (embeddings && completions) ? "embeddings + completions" :
                            (embeddings ? "embeddings only" :
                            (completions ? "completions only" : "neither"));

                        bool familyChanged =
                            !String.IsNullOrEmpty(detectedArch) &&
                            !String.Equals(mf.Family, detectedArch, StringComparison.OrdinalIgnoreCase);

                        if (mf.Embeddings != embeddings || mf.Completions != completions || familyChanged)
                        {
                            _Logging.Info(_Header + "capability detection: '" + mf.Name +
                                "' architecture='" + arch + "' - " + capabilityDesc +
                                " (was family='" + (mf.Family ?? "unknown") + "'" +
                                ", embeddings=" + mf.Embeddings + ", completions=" + mf.Completions +
                                "; now family='" + arch + "'" +
                                ", embeddings=" + embeddings + ", completions=" + completions +
                                ") - updating database");

                            mf.Embeddings = embeddings;
                            mf.Completions = completions;
                            if (!String.IsNullOrEmpty(detectedArch)) mf.Family = detectedArch;
                            _ModelFileService.Update(mf);
                            updated++;
                        }
                        else
                        {
                            _Logging.Debug(_Header + "capability detection: '" + mf.Name +
                                "' architecture='" + arch + "' - " + capabilityDesc + " (already correct in database)");
                            unchanged++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _Logging.Warn(_Header + "capability detection: failed to inspect '" + mf.Name + "':" + Environment.NewLine + ex.ToString());
                        failed++;
                    }

                    inspected++;
                    await Task.Yield();
                }

                _Logging.Info(_Header + "capability detection: complete - inspected " + inspected + ", updated " + updated +
                    ", unchanged " + unchanged + ", skipped " + skipped + ", failed " + failed);
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "capability detection: aborted with error: " + ex.ToString());
            }
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
        }

        private static void InitializeLogging()
        {
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
            _Logging.Settings.EnableColors = _Settings.Logging.EnableColors;
            _Logging.Settings.EnableConsole = _Settings.Logging.ConsoleLogging;
            _Logging.Settings.MinimumSeverity = (Severity)_Settings.Logging.MinimumSeverity;
        }

        private static void InitializeBootstrapper()
        {
            // This must happen before any LlamaSharp types are referenced
            try
            {
                NativeLibraryBootstrapper.Initialize(_Settings, _Logging);
            }
            catch (Exception ex)
            {
                Console.WriteLine("WARNING: Native library bootstrapper initialization failed:");
                Console.WriteLine(ex.ToString());

                if (SharpAIEnvironment.GetBool(SharpAIEnvironment.RequireBackend, false))
                {
                    Console.WriteLine(SharpAIEnvironment.RequireBackend + "=true; terminating startup.");
                    Environment.Exit(1);
                    return;
                }

                Console.WriteLine("Continuing with default LlamaSharp library loading...");
            }
        }

        private static void InitializeGlobals()
        {

            #region Database

            _Database = DatabaseDriverFactory.Create(_Settings.Database, _Logging);
            _Database.InitializeAsync().GetAwaiter().GetResult();

            #endregion

            #region Services

            _ModelFileService = new ModelFileService(_Logging, _Database.Models, _Settings.Storage.ModelsDirectory);
            _ModelEngineService = new ModelEngineService(_Logging);
            _HuggingFaceClient = new HuggingFaceClient(_Logging, _Settings.HuggingFace.ApiKey);
            _RequestHistoryCapture = new SharpAI.Server.Classes.Runtime.RequestHistoryCaptureService(_Database, _Settings.RequestHistory, _Logging);

            string tokenKeyMaterial = !String.IsNullOrEmpty(_Settings.Auth.TokenSigningKey)
                ? _Settings.Auth.TokenSigningKey
                : Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            if (String.IsNullOrEmpty(_Settings.Auth.TokenSigningKey))
                _Logging.Warn(_Header + "no Auth.TokenSigningKey configured; using a random per-boot key (session tokens will not survive restarts)");

            _SessionTokens = new SharpAI.Security.SessionTokenService(tokenKeyMaterial);
            _AuthEngine = new SharpAI.Security.AuthenticationEngine(_Database, _SessionTokens);
            _AuthEngine.DefaultSessionTtlMinutes = _Settings.Auth.SessionTtlMinutes;
            _RbacEngine = new SharpAI.Security.RbacEngine(_Database);
            _AuthService = new SharpAI.Server.Classes.Runtime.AuthenticationService(_Settings.Auth, _AuthEngine, _Database, _Logging);

            SeedAuthentication();

            try
            {
                int seededRoles = SharpAI.Security.RbacSeeder.Seed(_Database);
                if (seededRoles > 0) _Logging.Info(_Header + "seeded " + seededRoles + " built-in RBAC role(s)");
            }
            catch (Exception rbacEx)
            {
                _Logging.Warn(_Header + "RBAC role seeding failed:" + Environment.NewLine + rbacEx.ToString());
            }

            #endregion

            #region Handlers

            _OllamaApiHandler = new OllamaApiHandler(
                _Settings,
                _Logging,
                _Serializer,
                _ModelFileService,
                _ModelEngineService,
                _HuggingFaceClient);

            _OpenAIApiHandler = new OpenAIApiHandler(
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
            // Enable Watson 7.1 native telemetry (HTTP server metrics + spans) and serve an in-process
            // Prometheus endpoint on the existing listener when telemetry is enabled. Watson metrics are
            // scraped from /metrics; Watson spans are exported via Radiant (see TelemetryHost).
            if (_Settings.Telemetry != null && _Settings.Telemetry.Enable)
            {
                _Settings.Rest.Telemetry.Enable = true;
                _Settings.Rest.Telemetry.Prometheus.Enable = true;
                _Settings.Rest.Telemetry.Prometheus.Path = "/metrics";
            }

            _Server = new Webserver(_Settings.Rest, DefaultRoute);
            _Server.Events.Logger = (msg) => _Logging.Debug(_Header + msg);

            // Authentication runs before routing. When disabled (default), it installs the system principal
            // and never challenges; when enabled, it 401s unauthenticated requests to non-anonymous paths.
            _Server.Routes.AuthenticateRequest = _AuthService.AuthenticateRequestAsync;

            #region OpenAPI

            _Server.UseOpenApi(openApi =>
            {
                // The OpenAPI document (/openapi.json) and the Swagger UI (/swagger) are always served
                // anonymously so tooling and the dashboard's API Explorer can introspect the surface
                // without credentials. When authentication is added (plan W9), these two paths must remain
                // outside the authenticated route set.
                openApi.DocumentPath = "/openapi.json";
                openApi.SwaggerUiPath = "/swagger";
                openApi.EnableSwaggerUi = true;

                openApi.Info.Title = "SharpAI Server API";
                openApi.Info.Version = _Version;
                openApi.Info.Description =
                    "Local AI inference server with Ollama- and OpenAI-compatible REST endpoints. " +
                    "Provides model management, embeddings, completions, and chat completions against " +
                    "locally hosted GGUF models via LlamaSharp. The OpenAPI document at /openapi.json and " +
                    "the Swagger UI at /swagger are served without authentication.";
                openApi.Info.Contact = new OpenApiContact
                {
                    Name = "SharpAI",
                    Url = "https://github.com/jchristn/sharpai"
                };
                openApi.Info.License = new OpenApiLicense
                {
                    Name = "MIT",
                    Url = "https://opensource.org/licenses/MIT"
                };

                openApi.Tags.Add(new OpenApiTag { Name = "General", Description = "General server endpoints" });
                openApi.Tags.Add(new OpenApiTag { Name = "Settings", Description = "Server configuration management" });
                openApi.Tags.Add(new OpenApiTag { Name = "Request History", Description = "Captured request/response history" });
                openApi.Tags.Add(new OpenApiTag { Name = "Ollama - Models", Description = "Ollama-compatible model management" });
                openApi.Tags.Add(new OpenApiTag { Name = "Ollama - Inference", Description = "Ollama-compatible inference endpoints" });
                openApi.Tags.Add(new OpenApiTag { Name = "OpenAI - Inference", Description = "OpenAI-compatible inference endpoints" });
            });

            #endregion

            #region Middleware

            _Server.Routes.Preflight = async (ctx) =>
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                ctx.Response.Headers.Add("Access-Control-Allow-Methods", "OPTIONS, HEAD, GET, PUT, POST, DELETE, PATCH");
                ctx.Response.Headers.Add("Access-Control-Allow-Headers", "*");
                ctx.Response.Headers.Add("Access-Control-Max-Age", "86400");
                await ctx.Response.Send().ConfigureAwait(false);
            };

            _Server.Routes.PreRouting = async (ctx) =>
            {
                ctx.Response.Headers.Add(Constants.RequestIdHeader, Guid.NewGuid().ToString());

                if (_Settings.Debug.RequestBody)
                {
                    if (ctx.Request.ChunkedTransfer) _Logging.Debug(_Header + "chunked request body detected, skipping logging");
                    else if (!String.IsNullOrEmpty(ctx.Request.DataAsString))
                    {
                        _Logging.Debug(_Header + "request body:" + Environment.NewLine + ctx.Request.DataAsString);
                    }
                    else
                    {
                        _Logging.Debug(_Header + "no request body");
                    }
                }
            };

            _Server.Routes.PostRouting = async (ctx) =>
            {
                ctx.Timestamp.End = DateTime.UtcNow;

                _Logging.Debug(
                    _Header
                    + ctx.Request.Method + " " + ctx.Request.Url.RawWithQuery + " "
                    + ctx.Response.StatusCode + " "
                    + "(" + (ctx.Timestamp.TotalMs.HasValue ? ctx.Timestamp.TotalMs.Value.ToString("F2") : "?") + "ms)");

                if (_RequestHistoryCapture != null) _RequestHistoryCapture.Capture(ctx);

                await Task.CompletedTask.ConfigureAwait(false);
            };

            #endregion

            #region General-Routes

            _Server.Get("/", async (req) =>
            {
                req.Http.Response.ContentType = Constants.HtmlContentType;
                return Constants.HtmlHomepage;
            }, api => Describe(api, "General", "Server homepage")
                .WithDescription("Returns the default HTML homepage indicating the node is operational.")
                .WithResponse(200, OpenApiResponseMetadata.Text("Operational HTML page")));

            _Server.Head("/", async (req) => null, api => Describe(api, "General", "Server liveness check")
                .WithDescription("HEAD probe that returns 200 OK when the server is reachable."));

            _Server.Get("/health", async (req) =>
            {
                req.Http.Response.ContentType = Constants.JsonContentType;
                return new
                {
                    status = "healthy",
                    version = _Version,
                    backend = NativeLibraryBootstrapper.SelectedBackend,
                    native_initialized = NativeLibraryBootstrapper.IsInitialized,
                    utc = DateTime.UtcNow
                };
            }, api => Describe(api, "General", "Health check")
                .WithDescription("Lightweight liveness check for container and process monitoring.")
                .WithResponse(200, OpenApiResponseMetadata.Json("Health status", OpenApiSchemaMetadata.Create("object"))));

            _Server.Head("/health", async (req) => null, api => Describe(api, "General", "Health HEAD probe"));

            _Server.Get("/ready", async (req) =>
            {
                bool modelsDirectoryReady = DirectoryExistsAndWritable(_Settings?.Storage?.ModelsDirectory);

                bool logsDirectoryReady = DirectoryExistsAndWritable(_Settings?.Logging?.LogDirectory);

                bool ready = NativeLibraryBootstrapper.IsInitialized
                    && _Database != null && _Database.IsInitialized
                    && _ModelFileService != null
                    && _ModelEngineService != null
                    && modelsDirectoryReady
                    && logsDirectoryReady;

                req.Http.Response.ContentType = Constants.JsonContentType;
                if (!ready) req.Http.Response.StatusCode = 503;

                return new
                {
                    status = ready ? "ready" : "not_ready",
                    version = _Version,
                    backend = NativeLibraryBootstrapper.SelectedBackend,
                    native_initialized = NativeLibraryBootstrapper.IsInitialized,
                    database_initialized = _Database != null && _Database.IsInitialized,
                    models_directory = _Settings?.Storage?.ModelsDirectory,
                    models_directory_ready = modelsDirectoryReady,
                    logs_directory = _Settings?.Logging?.LogDirectory,
                    logs_directory_ready = logsDirectoryReady,
                    utc = DateTime.UtcNow
                };
            }, api => Describe(api, "General", "Readiness check")
                .WithDescription("Readiness check that verifies startup initialization and writable runtime directories.")
                .WithResponse(200, OpenApiResponseMetadata.Json("Ready status", OpenApiSchemaMetadata.Create("object")))
                .WithResponse(503, OpenApiResponseMetadata.Json("Not ready status", OpenApiSchemaMetadata.Create("object"))));

            _Server.Head("/favicon.ico", async (req) => null, api => Describe(api, "General", "Favicon HEAD probe"));

            _Server.Get("/favicon.ico", async (req) =>
            {
                req.Http.Response.ContentType = Constants.FaviconContentType;
                return File.ReadAllBytes(Constants.FaviconFilename);
            }, api => Describe(api, "General", "Serve favicon")
                .WithDescription("Returns the SharpAI favicon image.")
                .WithResponse(200, OpenApiResponseMetadata.Binary("PNG favicon", "image/png")));

            #endregion

            #region Settings-Endpoints

            _Server.Get("/api/settings", async (req) =>
            {
                Authorize(req, SharpAI.Security.ResourceTypes.Settings, SharpAI.Security.OperationTypeEnum.Read, null);
                return _Settings;
            }, api => Describe(api, "Settings", "Get current server settings")
                .WithDescription("Returns the current in-memory server settings loaded from sharpai.json.")
                .WithResponse(200, OpenApiResponseMetadata.Json("Current settings", OpenApiSchemaMetadata.Create("object"))));

            _Server.Put<Settings>("/api/settings", async (req) =>
            {
                Authorize(req, SharpAI.Security.ResourceTypes.Admin, SharpAI.Security.OperationTypeEnum.Admin, null);
                Settings updated = req.GetData<Settings>();
                if (updated == null) throw new WebserverException(ApiResultEnum.BadRequest, "Request body is required.");

                updated.CreatedUtc = _Settings.CreatedUtc;
                updated.SoftwareVersion = _Settings.SoftwareVersion;

                _Settings = updated;

                _Serializer.SerializeJsonToFile(Constants.SettingsFile, _Settings, true);

                _Logging.Info(_Header + "settings updated and saved to " + Constants.SettingsFile);

                return _Settings;
            }, api => Describe(api, "Settings", "Update server settings")
                .WithDescription(
                    "Replaces the in-memory server settings and rewrites sharpai.json on disk. " +
                    "CreatedUtc and SoftwareVersion are preserved from the current settings. " +
                    "Some settings (REST hostname, port, SSL, Database) require a server restart to take effect.")
                .WithRequestBody(OpenApiRequestBodyMetadata.Json(OpenApiSchemaMetadata.Create("object"), "Updated settings", true))
                .WithResponse(200, OpenApiResponseMetadata.Json("Updated settings", OpenApiSchemaMetadata.Create("object")))
                .WithResponse(400, OpenApiResponseMetadata.BadRequest()));

            #endregion

            #region RequestHistory-Endpoints

            _Server.Get("/v1.0/api/request-history", async (req) =>
            {
                Authorize(req, SharpAI.Security.ResourceTypes.RequestHistory, SharpAI.Security.OperationTypeEnum.Read, null);
                Models.RequestHistoryQuery query = new Models.RequestHistoryQuery();
                query.ApplyQuerystringOverrides(key => req.Http.Request.Query.Elements?[key]);
                return _Database.RequestHistory.Enumerate(query);
            }, api => Describe(api, "Request History", "List captured requests")
                .WithDescription("Paginated list of captured requests (bodies omitted). Filters: method, statusCode, pathContains, fromUtc, toUtc, tenantId, userId, pageNumber, pageSize.")
                .WithResponse(200, OpenApiResponseMetadata.Json("Request history page", OpenApiSchemaMetadata.Create("object"))));

            _Server.Get("/v1.0/api/request-history/summary", async (req) =>
            {
                Authorize(req, SharpAI.Security.ResourceTypes.RequestHistory, SharpAI.Security.OperationTypeEnum.Read, null);
                Models.RequestHistoryQuery query = new Models.RequestHistoryQuery();
                query.ApplyQuerystringOverrides(key => req.Http.Request.Query.Elements?[key]);
                return _Database.RequestHistory.Summarize(query);
            }, api => Describe(api, "Request History", "Summarize captured requests")
                .WithDescription("Time-bucketed counts and average durations for chart rendering. Emits a bucket for every interval including empty ones. Query: fromUtc, toUtc, bucketMinutes, plus the list filters.")
                .WithResponse(200, OpenApiResponseMetadata.Json("Request history summary", OpenApiSchemaMetadata.Create("object"))));

            _Server.Get("/v1.0/api/request-history/{id}", async (req) =>
            {
                Authorize(req, SharpAI.Security.ResourceTypes.RequestHistory, SharpAI.Security.OperationTypeEnum.Read, null);
                string id = req.Http.Request.Url.Parameters?["id"];
                Models.RequestHistoryEntry entry = _Database.RequestHistory.Read(id);
                if (entry == null) throw new WebserverException(ApiResultEnum.NotFound, "The specified request history entry was not found.");
                return entry;
            }, api => Describe(api, "Request History", "Read a captured request")
                .WithDescription("Returns a single captured request including headers and bodies.")
                .WithResponse(200, OpenApiResponseMetadata.Json("Request history entry", OpenApiSchemaMetadata.Create("object")))
                .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            _Server.Delete("/v1.0/api/request-history/{id}", async (req) =>
            {
                Authorize(req, SharpAI.Security.ResourceTypes.RequestHistory, SharpAI.Security.OperationTypeEnum.Delete, null);
                string id = req.Http.Request.Url.Parameters?["id"];
                bool deleted = _Database.RequestHistory.Delete(id);
                return new { deleted = deleted };
            }, api => Describe(api, "Request History", "Delete a captured request")
                .WithDescription("Deletes a single captured request by identifier.")
                .WithResponse(200, OpenApiResponseMetadata.Json("Delete result", OpenApiSchemaMetadata.Create("object"))));

            _Server.Delete("/v1.0/api/request-history", async (req) =>
            {
                Authorize(req, SharpAI.Security.ResourceTypes.RequestHistory, SharpAI.Security.OperationTypeEnum.Delete, null);
                Models.RequestHistoryQuery query = new Models.RequestHistoryQuery();
                query.ApplyQuerystringOverrides(key => req.Http.Request.Query.Elements?[key]);
                int deletedCount = _Database.RequestHistory.DeleteMany(query);
                return new { deletedCount = deletedCount };
            }, api => Describe(api, "Request History", "Bulk delete captured requests")
                .WithDescription("Deletes all captured requests matching the supplied filter. Returns the number of rows deleted.")
                .WithResponse(200, OpenApiResponseMetadata.Json("Bulk delete result", OpenApiSchemaMetadata.Create("object"))));

            #endregion

            #region Authentication-Endpoints

            _Server.Post("/v1.0/token", async (req) =>
            {
                string email = HeaderValue(req.Http, "x-email");
                string password = HeaderValue(req.Http, "x-password");
                string tenantGuid = HeaderValue(req.Http, "x-tenant-guid");

                if (String.IsNullOrEmpty(email) || String.IsNullOrEmpty(password))
                    throw new WebserverException(ApiResultEnum.BadRequest, "The x-email and x-password headers are required.");

                SharpAI.Security.AuthSession session;
                string token = _AuthEngine.Login(tenantGuid, email, password, 0, out session);
                if (token == null)
                    throw new WebserverException(ApiResultEnum.NotAuthorized, "Invalid credentials.");

                return new
                {
                    token = token,
                    sessionId = session.Guid,
                    tenantId = session.TenantGuid,
                    userId = session.UserGuid,
                    expiresUtc = session.ExpiresUtc
                };
            }, api => Describe(api, "Authentication", "Log in and create a session token")
                .WithDescription(
                    "Validates an email/password login and, on success, returns an opaque bearer token that " +
                    "references a revocable server-side session. Supply credentials in the x-email, x-password, " +
                    "and optional x-tenant-guid headers (the 'default' tenant is used when omitted). This " +
                    "endpoint is anonymous so it is reachable without a prior credential.")
                .WithResponse(200, OpenApiResponseMetadata.Json("Session token", OpenApiSchemaMetadata.Create("object")))
                .WithResponse(400, OpenApiResponseMetadata.BadRequest())
                .WithResponse(401, OpenApiResponseMetadata.Unauthorized()));

            _Server.Get("/v1.0/token", async (req) =>
            {
                SharpAI.Security.AuthSession session = _AuthEngine.ReadSession(ExtractBearerToken(req.Http));
                if (session == null)
                    throw new WebserverException(ApiResultEnum.NotAuthorized, "The bearer token is missing, invalid, or expired.");

                return new
                {
                    sessionId = session.Guid,
                    tenantId = session.TenantGuid,
                    userId = session.UserGuid,
                    createdUtc = session.CreatedUtc,
                    expiresUtc = session.ExpiresUtc
                };
            }, api => Describe(api, "Authentication", "Read the current session")
                .WithDescription("Returns details of the session referenced by the supplied bearer token (Authorization: Bearer, or x-token).")
                .WithResponse(200, OpenApiResponseMetadata.Json("Session details", OpenApiSchemaMetadata.Create("object")))
                .WithResponse(401, OpenApiResponseMetadata.Unauthorized()));

            _Server.Delete("/v1.0/token", async (req) =>
            {
                SharpAI.Security.AuthSession session = _AuthEngine.ReadSession(ExtractBearerToken(req.Http));
                if (session == null)
                    throw new WebserverException(ApiResultEnum.NotAuthorized, "The bearer token is missing, invalid, or expired.");

                _AuthEngine.RevokeSession(session.Guid, "Revoked by session holder.");
                return new { revoked = true, sessionId = session.Guid };
            }, api => Describe(api, "Authentication", "Revoke the current session")
                .WithDescription("Revokes (logs out) the session referenced by the supplied bearer token. The token is immediately invalid.")
                .WithResponse(200, OpenApiResponseMetadata.Json("Revocation result", OpenApiSchemaMetadata.Create("object")))
                .WithResponse(401, OpenApiResponseMetadata.Unauthorized()));

            _Server.Get("/v1.0/api/audit", async (req) =>
            {
                Authorize(req, SharpAI.Security.ResourceTypes.Audit, SharpAI.Security.OperationTypeEnum.Read, null);
                SharpAI.Security.RequestContext context = req.Http.Metadata as SharpAI.Security.RequestContext;

                Models.EnumerationQuery query = new Models.EnumerationQuery();
                query.ApplyQuerystringOverrides(key => req.Http.Request.Query.Elements?[key]);

                // Global admins (and the system principal when auth is disabled) may scope by the tenantGuid
                // query parameter (null = all tenants); everyone else is constrained to their own tenant.
                bool isGlobal = context == null || context.IsAdmin;
                string tenantScope = isGlobal
                    ? req.Http.Request.Query.Elements?["tenantGuid"]
                    : context.TenantGuid;

                return _Database.Audit.Enumerate(tenantScope, query);
            }, api => Describe(api, "Authentication", "List security audit events")
                .WithDescription(
                    "Paginated list of security audit events (authentication failures and privileged operations). " +
                    "Requires administrator or tenant-administrator privileges. Global administrators may scope " +
                    "with the tenantGuid query parameter; tenant administrators are constrained to their tenant.")
                .WithResponse(200, OpenApiResponseMetadata.Json("Audit event page", OpenApiSchemaMetadata.Create("object")))
                .WithResponse(403, OpenApiResponseMetadata.Forbidden()));

            _Server.Get("/v1.0/tenants/{tenantGuid}/users/{userGuid}/permissions", async (req) =>
            {
                string tenantGuid = req.Http.Request.Url.Parameters?["tenantGuid"];
                string userGuid = req.Http.Request.Url.Parameters?["userGuid"];
                AuthorizeInspection(req, tenantGuid, SharpAI.Security.PrincipalTypeEnum.User, userGuid);
                return EffectivePermissionsResponse(SharpAI.Security.PrincipalTypeEnum.User, userGuid, tenantGuid);
            }, api => Describe(api, "Authentication", "Inspect a user's effective permissions")
                .WithDescription(
                    "Returns the computed effective permission set for a user within a tenant, as (resourceType, " +
                    "operation, effect, scope, resourceGuid) grants. Requires Admin on the Admin resource, or the " +
                    "principal reading their own permissions.")
                .WithResponse(200, OpenApiResponseMetadata.Json("Effective permissions", OpenApiSchemaMetadata.Create("object")))
                .WithResponse(403, OpenApiResponseMetadata.Forbidden()));

            _Server.Get("/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}/permissions", async (req) =>
            {
                string tenantGuid = req.Http.Request.Url.Parameters?["tenantGuid"];
                string credentialGuid = req.Http.Request.Url.Parameters?["credentialGuid"];
                AuthorizeInspection(req, tenantGuid, SharpAI.Security.PrincipalTypeEnum.Credential, credentialGuid);
                return EffectivePermissionsResponse(SharpAI.Security.PrincipalTypeEnum.Credential, credentialGuid, tenantGuid);
            }, api => Describe(api, "Authentication", "Inspect a credential's effective permissions")
                .WithDescription(
                    "Returns the computed effective permission set for a credential within a tenant. Requires Admin " +
                    "on the Admin resource, or the credential's owner reading its own permissions.")
                .WithResponse(200, OpenApiResponseMetadata.Json("Effective permissions", OpenApiSchemaMetadata.Create("object")))
                .WithResponse(403, OpenApiResponseMetadata.Forbidden()));

            #endregion

            #region Ollama-Endpoints

            _Server.Post<OllamaPullModelRequest>("/api/pull", async (req) =>
            {
                Authorize(req, SharpAI.Security.ResourceTypes.Model, SharpAI.Security.OperationTypeEnum.Write, null);
                OllamaPullModelRequest pmr = req.GetData<OllamaPullModelRequest>();
                return await _OllamaApiHandler.PullModel(req, pmr, _TokenSource.Token).ConfigureAwait(false);
            }, api => Describe(api, "Ollama - Models", "Pull a model")
                .WithDescription("Downloads a model from HuggingFace. Streams progress as newline-delimited JSON.")
                .WithRequestBody(OpenApiRequestBodyMetadata.Json(OpenApiSchemaMetadata.Create("object"), "Pull model request", true))
                .WithResponse(200, OpenApiResponseMetadata.Json("Progress stream", OpenApiSchemaMetadata.Create("object")))
                .WithResponse(400, OpenApiResponseMetadata.BadRequest()));

            _Server.Delete<OllamaDeleteModelRequest>("/api/delete", async (req) =>
            {
                Authorize(req, SharpAI.Security.ResourceTypes.Model, SharpAI.Security.OperationTypeEnum.Delete, null);
                OllamaDeleteModelRequest dmr = req.GetData<OllamaDeleteModelRequest>();
                return await _OllamaApiHandler.DeleteModel(req, dmr, _TokenSource.Token).ConfigureAwait(false);
            }, api => Describe(api, "Ollama - Models", "Delete a model")
                .WithDescription("Deletes a locally stored model by name.")
                .WithRequestBody(OpenApiRequestBodyMetadata.Json(OpenApiSchemaMetadata.Create("object"), "Delete model request", true))
                .WithResponse(200, OpenApiResponseMetadata.Json("Deletion result", OpenApiSchemaMetadata.Create("object")))
                .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            _Server.Post<OllamaUnloadModelRequest>("/api/unload", async (req) =>
            {
                Authorize(req, SharpAI.Security.ResourceTypes.Model, SharpAI.Security.OperationTypeEnum.Write, null);
                OllamaUnloadModelRequest umr = req.GetData<OllamaUnloadModelRequest>();
                return await _OllamaApiHandler.UnloadModel(req, umr, _TokenSource.Token).ConfigureAwait(false);
            }, api => Describe(api, "Ollama - Models", "Unload a model from memory")
                .WithDescription(
                    "Unloads a model from memory, freeing GPU/CPU resources. " +
                    "If no model name is provided, all loaded models are unloaded. " +
                    "This is useful when switching between models on memory-constrained systems.")
                .WithRequestBody(OpenApiRequestBodyMetadata.Json(OpenApiSchemaMetadata.Create("object"), "Unload model request", false))
                .WithResponse(200, OpenApiResponseMetadata.Json("Unload result", OpenApiSchemaMetadata.Create("object")))
                .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            _Server.Get("/api/tags", async (req) =>
            {
                Authorize(req, SharpAI.Security.ResourceTypes.Model, SharpAI.Security.OperationTypeEnum.Read, null);
                return await _OllamaApiHandler.ListLocalModels(req, _TokenSource.Token).ConfigureAwait(false);
            }, api => Describe(api, "Ollama - Models", "List local models")
                .WithDescription("Returns the list of locally available models.")
                .WithResponse(200, OpenApiResponseMetadata.Json("Local models", OpenApiSchemaMetadata.Create("object"))));

            _Server.Get("/api/ps", async (req) =>
            {
                Authorize(req, SharpAI.Security.ResourceTypes.Model, SharpAI.Security.OperationTypeEnum.Read, null);
                return await _OllamaApiHandler.ListRunningModels(req, _TokenSource.Token).ConfigureAwait(false);
            }, api => Describe(api, "Ollama - Models", "List running (loaded) models")
                .WithDescription(
                    "Returns the list of models that are currently loaded in memory, matching the Ollama " +
                    "'/api/ps' (ollama ps) endpoint. The size_vram field reports the full model size when " +
                    "the CUDA or Metal backend is active and 0 when the CPU backend is active. When keep-alive " +
                    "eviction is enabled (SHARPAI_KEEP_ALIVE_SECONDS), expires_at reports the eviction time; " +
                    "otherwise it is null.")
                .WithResponse(200, OpenApiResponseMetadata.Json("Running models", OpenApiSchemaMetadata.Create("object"))));

            _Server.Get("/api/version", async (req) =>
            {
                return new { version = _Version };
            }, api => Describe(api, "Ollama - Models", "Server version")
                .WithDescription("Returns the SharpAI server version, matching Ollama's /api/version.")
                .WithResponse(200, OpenApiResponseMetadata.Json("Version", OpenApiSchemaMetadata.Create("object"))));

            _Server.Post<OllamaShowModelInfoRequest>("/api/show", async (req) =>
            {
                Authorize(req, SharpAI.Security.ResourceTypes.Model, SharpAI.Security.OperationTypeEnum.Read, null);
                OllamaShowModelInfoRequest sr = req.GetData<OllamaShowModelInfoRequest>();
                if (sr == null || String.IsNullOrEmpty(sr.Model))
                    throw new WebserverException(ApiResultEnum.BadRequest, "A model name is required.");

                Models.ModelFile mf = _ModelFileService.GetByName(sr.Model);
                if (mf == null)
                    throw new WebserverException(ApiResultEnum.NotFound, "The specified model was not found.");

                return new
                {
                    model = mf.Name,
                    details = new
                    {
                        format = "gguf",
                        family = mf.Family,
                        parameter_size = mf.ParameterSize,
                        quantization_level = mf.Quantization
                    },
                    capabilities = new { embeddings = mf.Embeddings, completions = mf.Completions },
                    size = mf.ContentLength,
                    digest = mf.SHA256Hash,
                    created_at = (object)(mf.ModelCreationUtc ?? mf.CreatedUtc)
                };
            }, api => Describe(api, "Ollama - Models", "Show model information")
                .WithDescription("Returns metadata and capabilities for a local model, matching Ollama's /api/show.")
                .WithRequestBody(OpenApiRequestBodyMetadata.Json(OpenApiSchemaMetadata.Create("object"), "Show request", true))
                .WithResponse(200, OpenApiResponseMetadata.Json("Model info", OpenApiSchemaMetadata.Create("object")))
                .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            _Server.Post<OllamaGenerateEmbeddingsRequest>("/api/embed", async (req) =>
            {
                Authorize(req, SharpAI.Security.ResourceTypes.Inference, SharpAI.Security.OperationTypeEnum.Execute, null);
                OllamaGenerateEmbeddingsRequest ger = req.GetData<OllamaGenerateEmbeddingsRequest>();
                return await _OllamaApiHandler.GenerateEmbeddings(req, ger, _TokenSource.Token).ConfigureAwait(false);
            }, api => Describe(api, "Ollama - Inference", "Generate embeddings")
                .WithDescription("Generates vector embeddings for a single input or array of inputs.")
                .WithRequestBody(OpenApiRequestBodyMetadata.Json(OpenApiSchemaMetadata.Create("object"), "Embeddings request", true))
                .WithResponse(200, OpenApiResponseMetadata.Json("Embeddings", OpenApiSchemaMetadata.Create("object"))));

            _Server.Post<OllamaGenerateCompletionRequest>("/api/generate", async (req) =>
            {
                return await RunInference(async () =>
                {
                    Authorize(req, SharpAI.Security.ResourceTypes.Inference, SharpAI.Security.OperationTypeEnum.Execute, null);
                OllamaGenerateCompletionRequest gcr = req.GetData<OllamaGenerateCompletionRequest>();
                    object ret = await _OllamaApiHandler.GenerateCompletion(req, gcr, _TokenSource.Token).ConfigureAwait(false);
                    if (req.Http.Response.ChunkedTransfer) return null;
                    else return ret;
                }).ConfigureAwait(false);
            }, api => Describe(api, "Ollama - Inference", "Generate text completion")
                .WithDescription("Generates a text completion for the given prompt. Supports streaming via chunked transfer.")
                .WithRequestBody(OpenApiRequestBodyMetadata.Json(OpenApiSchemaMetadata.Create("object"), "Completion request", true))
                .WithResponse(200, OpenApiResponseMetadata.Json("Completion response", OpenApiSchemaMetadata.Create("object")))
                .WithResponse(429, OpenApiResponseMetadata.Json("Server busy or at capacity — retry later", OpenApiSchemaMetadata.Create("object"))));

            _Server.Post<OllamaGenerateChatCompletionRequest>("/api/chat", async (req) =>
            {
                return await RunInference(async () =>
                {
                    Authorize(req, SharpAI.Security.ResourceTypes.Inference, SharpAI.Security.OperationTypeEnum.Execute, null);
                OllamaGenerateChatCompletionRequest gccr = req.GetData<OllamaGenerateChatCompletionRequest>();
                    object ret = await _OllamaApiHandler.GenerateChatCompletion(req, gccr, _TokenSource.Token).ConfigureAwait(false);
                    if (req.Http.Response.ChunkedTransfer) return null;
                    else return ret;
                }).ConfigureAwait(false);
            }, api => Describe(api, "Ollama - Inference", "Generate chat completion")
                .WithDescription("Generates a chat completion from a sequence of messages. Supports streaming via chunked transfer.")
                .WithRequestBody(OpenApiRequestBodyMetadata.Json(OpenApiSchemaMetadata.Create("object"), "Chat completion request", true))
                .WithResponse(200, OpenApiResponseMetadata.Json("Chat completion response", OpenApiSchemaMetadata.Create("object")))
                .WithResponse(429, OpenApiResponseMetadata.Json("Server busy or at capacity — retry later", OpenApiSchemaMetadata.Create("object"))));

            #endregion

            #region OpenAI-Endpoints

            _Server.Get("/v1/models", async (req) =>
            {
                Authorize(req, SharpAI.Security.ResourceTypes.Model, SharpAI.Security.OperationTypeEnum.Read, null);
                List<Models.ModelFile> models = CollectAllModels();
                List<object> data = new List<object>();
                if (models != null)
                {
                    foreach (Models.ModelFile m in models)
                    {
                        data.Add(new
                        {
                            id = m.Name,
                            @object = "model",
                            created = new DateTimeOffset(DateTime.SpecifyKind(m.CreatedUtc, DateTimeKind.Utc)).ToUnixTimeSeconds(),
                            owned_by = "sharpai"
                        });
                    }
                }
                return new { @object = "list", data = data };
            }, api => Describe(api, "OpenAI - Models", "List models (OpenAI-compatible)")
                .WithDescription("OpenAI-compatible model list; returns locally available models.")
                .WithResponse(200, OpenApiResponseMetadata.Json("Model list", OpenApiSchemaMetadata.Create("object"))));

            _Server.Post<OpenAIGenerateEmbeddingsRequest>("/v1/embeddings", async (req) =>
            {
                Authorize(req, SharpAI.Security.ResourceTypes.Inference, SharpAI.Security.OperationTypeEnum.Execute, null);
                OpenAIGenerateEmbeddingsRequest ger = req.GetData<OpenAIGenerateEmbeddingsRequest>();
                return await _OpenAIApiHandler.GenerateEmbeddings(req, ger, _TokenSource.Token).ConfigureAwait(false);
            }, api => Describe(api, "OpenAI - Inference", "Generate embeddings (OpenAI-compatible)")
                .WithDescription("OpenAI-compatible embeddings endpoint.")
                .WithRequestBody(OpenApiRequestBodyMetadata.Json(OpenApiSchemaMetadata.Create("object"), "Embeddings request", true))
                .WithResponse(200, OpenApiResponseMetadata.Json("Embeddings response", OpenApiSchemaMetadata.Create("object"))));

            _Server.Post<OpenAIGenerateCompletionRequest>("/v1/completions", async (req) =>
            {
                Authorize(req, SharpAI.Security.ResourceTypes.Inference, SharpAI.Security.OperationTypeEnum.Execute, null);
                return await RunInference(async () =>
                {
                    OpenAIGenerateCompletionRequest gcr = req.GetData<OpenAIGenerateCompletionRequest>();
                    object ret = await _OpenAIApiHandler.GenerateCompletion(req, gcr, _TokenSource.Token).ConfigureAwait(false);
                    if (req.Http.Response.ServerSentEvents) return null;
                    else return ret;
                }).ConfigureAwait(false);
            }, api => Describe(api, "OpenAI - Inference", "Generate text completion (OpenAI-compatible)")
                .WithDescription("OpenAI-compatible text completion endpoint. Supports streaming via server-sent events.")
                .WithRequestBody(OpenApiRequestBodyMetadata.Json(OpenApiSchemaMetadata.Create("object"), "Completion request", true))
                .WithResponse(200, OpenApiResponseMetadata.Json("Completion response", OpenApiSchemaMetadata.Create("object")))
                .WithResponse(429, OpenApiResponseMetadata.Json("Server busy or at capacity — retry later", OpenApiSchemaMetadata.Create("object"))));

            _Server.Post<OpenAIGenerateChatCompletionRequest>("/v1/chat/completions", async (req) =>
            {
                Authorize(req, SharpAI.Security.ResourceTypes.Inference, SharpAI.Security.OperationTypeEnum.Execute, null);
                return await RunInference(async () =>
                {
                    OpenAIGenerateChatCompletionRequest gccr = req.GetData<OpenAIGenerateChatCompletionRequest>();
                    object ret = await _OpenAIApiHandler.GenerateChatCompletion(req, gccr, _TokenSource.Token).ConfigureAwait(false);
                    if (req.Http.Response.ServerSentEvents) return null;
                    else return ret;
                }).ConfigureAwait(false);
            }, api => Describe(api, "OpenAI - Inference", "Generate chat completion (OpenAI-compatible)")
                .WithDescription("OpenAI-compatible chat completion endpoint. Supports streaming via server-sent events.")
                .WithRequestBody(OpenApiRequestBodyMetadata.Json(OpenApiSchemaMetadata.Create("object"), "Chat completion request", true))
                .WithResponse(200, OpenApiResponseMetadata.Json("Chat completion response", OpenApiSchemaMetadata.Create("object")))
                .WithResponse(429, OpenApiResponseMetadata.Json("Server busy or at capacity — retry later", OpenApiSchemaMetadata.Create("object"))));

            #endregion
        }

        private static List<Models.ModelFile> CollectAllModels()
        {
            // The Ollama /api/tags and OpenAI /v1/models contracts return the full model list. There is no
            // "get all" data-access method; we page through the enumeration to materialize the full set.
            List<Models.ModelFile> all = new List<Models.ModelFile>();
            Models.EnumerationQuery query = new Models.EnumerationQuery
            {
                PageSize = 1000,
                Order = Models.EnumerationOrderEnum.CreatedDescending
            };

            while (true)
            {
                Models.EnumerationResult<Models.ModelFile> page = _ModelFileService.Enumerate(query);
                if (page.Objects != null && page.Objects.Count > 0) all.AddRange(page.Objects);
                if (page.EndOfResults) break;
                query.PageNumber = query.PageNumber + 1;
            }

            return all;
        }

        private static async Task<object> RunInference(Func<Task<object>> handler)
        {
            try
            {
                return await handler().ConfigureAwait(false);
            }
            catch (SharpAI.Exceptions.EngineBusyException ex)
            {
                // All generation slots are busy — signal the client to back off and retry (HTTP 429).
                throw new WebserverException(ApiResultEnum.SlowDown, ex.Message);
            }
            catch (SharpAI.Exceptions.ModelAdmissionException ex)
            {
                // The model cannot be admitted within the memory budget — treat as a capacity/back-off condition.
                throw new WebserverException(ApiResultEnum.SlowDown, ex.Message);
            }
        }

        private static bool DirectoryExistsAndWritable(string directory)
        {
            if (String.IsNullOrWhiteSpace(directory)) return false;
            if (!Directory.Exists(directory)) return false;

            string testFile = Path.Combine(directory, ".sharpai-write-test-" + Guid.NewGuid().ToString("N"));

            try
            {
                using (FileStream stream = new FileStream(testFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    byte[] buffer = new byte[] { 0 };
                    stream.Write(buffer, 0, buffer.Length);
                }

                File.Delete(testFile);
                return true;
            }
            catch
            {
                try
                {
                    if (File.Exists(testFile)) File.Delete(testFile);
                }
                catch
                {
                }

                return false;
            }
        }

        private static async Task DefaultRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.ContentType = Constants.JsonContentType;
            await ctx.Response.Send("{\"error\":\"NotFound\",\"message\":\"No route matched\"}").ConfigureAwait(false);
        }

        private static OpenApiRouteMetadata Describe(OpenApiRouteMetadata api, string tag, string summary)
        {
            api.Summary = summary;
            api.WithTag(tag);
            return api;
        }

        private static string HeaderValue(WatsonWebserver.Core.HttpContextBase ctx, string name)
        {
            System.Collections.Specialized.NameValueCollection headers = ctx.Request.Headers;
            if (headers == null) return null;

            foreach (string key in headers.AllKeys)
            {
                if (key != null && key.Equals(name, StringComparison.OrdinalIgnoreCase)) return headers[key];
            }

            return null;
        }

        private static string ExtractBearerToken(WatsonWebserver.Core.HttpContextBase ctx)
        {
            string authorization = HeaderValue(ctx, "authorization");
            if (!String.IsNullOrEmpty(authorization) &&
                authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authorization.Substring(7).Trim();
            }

            return HeaderValue(ctx, "x-token");
        }

        /// <summary>
        /// Enforce RBAC for a route. When authentication is disabled the request runs as the implicit
        /// system principal (<c>IsAdmin</c>) and this is a no-op — preserving Ollama-parity open access.
        /// When enabled, administrators bypass; every other principal is evaluated against its effective
        /// permissions, and a denial is audited and surfaced as HTTP 403.
        /// </summary>
        private static void Authorize(WatsonWebserver.Core.ApiRequest req, string resourceType, SharpAI.Security.OperationTypeEnum operation, string resourceGuid)
        {
            SharpAI.Security.RequestContext context = req.Http.Metadata as SharpAI.Security.RequestContext;

            if (context == null)
            {
                if (!_Settings.Auth.Enabled) return;
                throw new WebserverException(ApiResultEnum.Forbidden, "Authorization context is unavailable.");
            }

            if (context.IsAdmin) return;

            SharpAI.Security.AuthorizationRequest ar = new SharpAI.Security.AuthorizationRequest
            {
                TenantGuid = context.TenantGuid,
                PrincipalType = context.PrincipalType,
                PrincipalGuid = context.PrincipalGuid,
                IsAdmin = context.IsAdmin,
                IsTenantAdmin = context.IsTenantAdmin,
                ResourceType = resourceType,
                Operation = operation,
                ResourceGuid = resourceGuid,
                OwnerUserGuid = context.OwnerUserGuid
            };

            SharpAI.Security.AuthorizationDecision decision = _RbacEngine.Authorize(ar);
            if (!decision.IsPermitted)
            {
                RecordAuthzDenial(req.Http, context, resourceType, operation, decision);
                throw new WebserverException(ApiResultEnum.Forbidden, decision.Reason);
            }
        }

        private static void RecordAuthzDenial(
            WatsonWebserver.Core.HttpContextBase ctx,
            SharpAI.Security.RequestContext context,
            string resourceType,
            SharpAI.Security.OperationTypeEnum operation,
            SharpAI.Security.AuthorizationDecision decision)
        {
            try
            {
                string path = ctx.Request.Url != null ? ctx.Request.Url.RawWithQuery : null;
                if (!String.IsNullOrEmpty(path))
                {
                    int q = path.IndexOf('?');
                    if (q >= 0) path = path.Substring(0, q);
                }

                SharpAI.Security.AuditLogEntry entry = new SharpAI.Security.AuditLogEntry
                {
                    TenantGuid = context.TenantGuid,
                    EventType = "AuthorizationDenied",
                    PrincipalType = context.PrincipalType,
                    PrincipalGuid = context.PrincipalGuid,
                    Method = ctx.Request.Method.ToString(),
                    Path = path,
                    IpAddress = ctx.Request.Source != null ? ctx.Request.Source.IpAddress : null,
                    AuthResult = true,
                    AuthzResult = false,
                    DenialReason = resourceType + ":" + operation + " — " + decision.Reason,
                    StatusCode = 403
                };
                _Database.Audit.Create(entry);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "unable to record authorization denial: " + e.Message);
            }
        }

        private static void AuthorizeInspection(
            WatsonWebserver.Core.ApiRequest req,
            string tenantGuid,
            SharpAI.Security.PrincipalTypeEnum principalType,
            string principalGuid)
        {
            SharpAI.Security.RequestContext context = req.Http.Metadata as SharpAI.Security.RequestContext;

            if (context == null)
            {
                if (!_Settings.Auth.Enabled) return;
                throw new WebserverException(ApiResultEnum.Forbidden, "Authorization context is unavailable.");
            }

            if (context.IsAdmin) return;

            // A principal may always read its own effective permissions.
            if (context.PrincipalType == principalType
                && !String.IsNullOrEmpty(principalGuid)
                && String.Equals(context.PrincipalGuid, principalGuid, StringComparison.Ordinal)
                && String.Equals(context.TenantGuid, tenantGuid, StringComparison.Ordinal))
            {
                return;
            }

            // Otherwise this is an administrative inspection and requires Admin on the Admin resource
            // (IsTenantAdmin satisfies this via the RBAC bypass).
            Authorize(req, SharpAI.Security.ResourceTypes.Admin, SharpAI.Security.OperationTypeEnum.Admin, null);
        }

        private static object EffectivePermissionsResponse(
            SharpAI.Security.PrincipalTypeEnum principalType,
            string principalGuid,
            string tenantGuid)
        {
            System.Collections.Generic.List<SharpAI.Security.EffectivePermission> effective =
                _RbacEngine.GetEffectivePermissions(principalType, principalGuid, tenantGuid);

            System.Collections.Generic.List<object> grants = new System.Collections.Generic.List<object>();
            foreach (SharpAI.Security.EffectivePermission permission in effective)
            {
                grants.Add(new
                {
                    resourceType = permission.ResourceType,
                    operation = permission.Operation.ToString(),
                    effect = permission.Effect.ToString(),
                    scope = permission.ResourceScope.ToString(),
                    resourceGuid = permission.ResourceGuid,
                    inheritsToChildren = permission.InheritsToChildren
                });
            }

            return new
            {
                tenantId = tenantGuid,
                principalType = principalType.ToString(),
                principalId = principalGuid,
                count = grants.Count,
                permissions = grants
            };
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
