# Changelog

## Unreleased — v5.0.0 (in progress)

The 5.0.0 line is a unified, breaking, enterprise-focused release. The full scope and task-by-task
status live in [IMPROVEMENTS.md](IMPROVEMENTS.md); entries here are added as slices land.

### Added

- **RBAC enforcement + inspection.** Control-plane and inference routes are now gated by a central
  authorization check that maps each route to its `(ResourceType, Operation)` cost, evaluates it against the
  caller's effective permissions, audits denials, and returns 403. Two effective-permissions inspection
  endpoints (`GET /v1.0/tenants/{tenantGuid}/users/{userGuid}/permissions` and the credential equivalent)
  let dashboards render permission gates. With authentication disabled these gates are no-ops (open server).
- **RBAC authorization.** A tenant-scoped role/permission model — roles, permissions, role↔permission maps,
  user role assignments, and credential scope assignments (migration v4 across all four providers) — plus a
  framework-free `RbacEngine` that evaluates access with explicit-deny-wins, tenant vs resource scope,
  `InheritsToChildren`, `Write`→Create/Update/Delete expansion, `All` wildcards, `IsAdmin`/`IsTenantAdmin`
  bypass, and a credential owner-ceiling. Six immutable built-in roles (TenantAdmin, SecurityAdmin, Auditor,
  Editor, Viewer, TenantMember) are seeded at startup and resolvable by GUID or name. Covered by 10 contract
  tests.
- **Authentication (functional, off by default).** Three credentialed schemes over the admin-key baseline:
  header login (`POST /v1.0/token` with `x-email`/`x-password`/optional `x-tenant-guid`, minting a revocable
  session token), bearer session tokens (`Authorization: Bearer` or `x-token`, with `GET`/`DELETE /v1.0/token`
  to inspect and revoke), and access-key/secret-key (`x-access-key`/`x-secret-key`). Every authentication
  denial is written to the security audit log, exposed (admin/tenant-admin only) at `GET /v1.0/api/audit`.
  On first boot a protected `default` tenant and an `admin@sharpai.local` administrator are seeded, with the
  one-time initial password logged. New settings: `Auth.TokenSigningKey`, `Auth.SessionTtlMinutes`. The
  auth decision lives in a framework-free `AuthenticationEngine` covered by 12 contract tests.
- **Authentication data layer.** Tenants, users, credentials, sessions, and a security audit log —
  models, `ITenant/IUser/ICredential/IAuthSession/IAuditMethods` data-access, and handwritten-SQL
  implementations added as migration v3 across all four providers. Passwords/secrets are stored only as
  SHA-256 digests (constant-time verified); session bearer tokens are AES-256-CBC with a fresh random IV
  per token; access/secret keys are high-entropy prefixed values.
- **Authentication (off by default).** A new `Auth` settings block (`Enabled` default false) with an
  `AuthenticationService` on Watson's `AuthenticateRequest` hook that establishes a typed `RequestContext`
  in `ctx.Metadata`. Disabled (the default) runs every request as an implicit, fully-authorized system
  principal — identical to Ollama's open server. Enabled requires an admin API key (`x-api-key`,
  constant-time compared) for non-anonymous endpoints and returns 401 otherwise; `/openapi.json`,
  `/swagger`, `/health`, and `/ready` stay anonymous. Full user/credential/session/RBAC support builds on
  this foundation.
- **Request capture & history.** Every HTTP request is captured (method, path, status, duration, source
  IP, headers with secrets redacted, and a truncated request body) on a non-blocking background write,
  persisted through a four-provider `request_history` table (migration v2). New endpoints under
  `/v1.0/api/request-history` (paginated list, `/summary` time-bucketed for charts, `/{id}` detail,
  single and bulk delete), an hourly retention prune (`RequestHistory.RetentionDays`), and a
  `RequestHistory` settings block. Capture is enabled by default and can be turned off.
- **`GET /v1/models`** (OpenAI-compatible model list), **`GET /api/version`**, and **`POST /api/show`**
  (Ollama-compatible model metadata + capabilities) endpoints.
- **Tool-call output parser** (`SharpAI.Tools.ToolCallParser`) that extracts tool/function calls from the
  common model output formats — the model-independent foundation for full tool calling.
- **Fixture-gated integration tests.** A `ModelFixture` locates a GGUF via `SHARPAI_TEST_MODEL_GGUF` or a
  `test-models/` directory, and a `ModelInferenceSuite` runs real inference (init, chat-template
  rendering, small-`max_tokens` generation, concurrent generation, embeddings) when a model is present,
  skipping cleanly otherwise.
- **`/api/ps` now reports `expires_at`** when keep-alive eviction is enabled.
- **Capacity backpressure maps to HTTP 429**: generation requests rejected for busy slots or model-memory
  budget return a "slow down" response instead of a 500.
- **Configurable generation concurrency and admission:** `SHARPAI_MAX_CONCURRENT_GENERATIONS` (parallel
  decode slots) and `SHARPAI_GENERATION_QUEUE_TIMEOUT_MS` (reject-when-busy backpressure).
- **Model lifecycle controls:** `SHARPAI_KEEP_ALIVE_SECONDS` (idle eviction), `SHARPAI_MAX_RESIDENT_MODELS`
  (LRU cap), and `SHARPAI_MODEL_MEMORY_BUDGET_MB` (memory-budget admission that evicts LRU models or
  rejects with a structured error instead of risking an OOM).
- **Configurable thinking-tag markers** on `ThinkingFilter`, so reasoning models that don't use
  `<think>`/`</think>` are handled; unclosed thinking blocks are no longer leaked on flush.
- **In-app telemetry.** The core library emits inference metrics and spans via `SharpAI.Telemetry`
  (BCL `Meter`/`ActivitySource`, no host dependency): request counts, tokens generated, inference
  latency, and a resident-models gauge. The server hosts the OpenTelemetry pipeline with
  [Radiant](https://www.nuget.org/packages/Radiant) 0.1.2 through a new `Telemetry` settings section
  (OTLP endpoint/protocol, optional in-process Prometheus endpoint, `SHARPAI_TELEMETRY_*` env
  overrides), starting on boot and flushing on shutdown. Disabled cleanly via `Telemetry.Enable=false`.
- **Turnkey observability stack** in `docker/compose.yaml`: OpenTelemetry Collector, Prometheus,
  Loki, Tempo, and Grafana with provisioned datasources and Overview/Logs dashboards. Logs are
  tailed from the server's rolling files; SharpAI metrics/traces flow over OTLP. See
  `docker/telemetry/README.md`.
- **Touchstone test suites.** New `Test.Shared` / `Test.Automated` / `Test.Xunit` / `Test.Nunit`
  projects (Touchstone 0.1.12) covering the prompt-format, chat-template, thinking-filter, and
  telemetry-safety surface (84 descriptors, green on all three runners).
- `SchemaVersion` field in `sharpai.json` to support settings migration across major versions.

### Changed

- **No unbounded "get all" list APIs.** Model listing now flows exclusively through an
  `EnumerationQuery` (page number/size, order, created-before/after, and name/family/quantization/format
  filters) returning an `EnumerationResult<ModelFile>` (totals, records-remaining, continuation token).
  The `All()` methods on the registry, `ModelFileService`, and `ModelDriver` were removed; the
  Ollama `/api/tags` and OpenAI `/v1/models` contracts page through the enumeration internally.
- **Database layer rewritten; WatsonORM removed.** SharpAI now ships a hand-rolled, provider-neutral data
  layer (`DatabaseDriverBase`/`DatabaseDriverFactory` + `IModelRegistryMethods`) with handwritten,
  dialect-aware SQL and versioned/tracked migrations over raw ADO.NET, supporting **SQLite, MySQL,
  PostgreSQL, and SQL Server** — selectable via `Database.Type`. SQLite remains the zero-config local
  default; server databases are configured with `Hostname`/`Port`/`DatabaseName`/`Username`/`Password`.
- **OpenAPI/Swagger hardening.** Every REST route carries OpenAPI metadata; the generation routes now
  document their 429 (busy/at-capacity) response. The OpenAPI document (`/openapi.json`) and Swagger UI
  (`/swagger`) are explicitly enabled and served without authentication so tooling and the dashboard's
  API Explorer can introspect the surface (and must remain anonymous when auth lands in W9).
- **Chat prompts now use the model's embedded GGUF template.** `/api/chat` and `/v1/chat/completions`
  render prompts with the model's own `tokenizer.chat_template` (via LlamaSharp `LLamaTemplate`), falling
  back to the family-based template only when a model has none. Generation ends on the model's native
  end-of-turn tokens instead of injected anti-prompts, fixing subtle mis-formatting and leaked/`runaway`
  output for models whose real template differs from the family guess.
- **`max_tokens` is honored exactly.** Small values such as `max_tokens: 8` are no longer silently
  raised to 100; a configurable `DefaultMaxTokens` applies only when no positive value is requested.
- **Concurrent model loading.** Loading one model no longer blocks requests to other loaded models: the
  engine cache uses per-model gating (`ConcurrentDictionary<…, Lazy<Task<…>>>`) instead of a global lock
  held across initialization, and a fully async `GetByModelFileAsync` replaces the sync-over-async load.
- **Lazy embeddings.** A generation-only model no longer loads a second copy of its weights up front; the
  embedding context is created on first embedding request.
- **Watson upgraded to 7.1.0**, enabling native HTTP-server telemetry: the server exposes a Prometheus
  `/metrics` endpoint on its listener and emits request spans (exported via Radiant) when telemetry is on.
- **Version normalized to `5.0.0`** across the core library, server, dashboard, C#/JS SDKs, Docker
  images, and default configuration (previously split across 1.x/4.0.1/1.0.x tracks).
- `.gitignore` now excludes Docker runtime artifacts (`docker/logs/`, `docker/models/`,
  `docker/sharpai.db`), which are seeded from `docker/factory/`.

### Removed

- **Vision / llava** support removed entirely: deleted the bundled `llava_shared.dll`, its
  `SharpAI.csproj` wiring, the `llava` package tag, and remaining vision doc references. (Core vision
  code was retired in an earlier release; this completes the removal.)

---

## v4.0.1

### Added

- Automated regression tests for `ChatFormatHelper`, `ChatPromptBuilder`, and `ThinkingFilter` covering the new `Qwen3.5` and `Gemma4` family aliases

### Changed

- **Upgraded LLamaSharp from v0.26.0 to v0.27.0** to pick up upstream Qwen3.5 and Gemma4 support
- Expanded `ChatFormatHelper` family aliases so `qwen3.5`, `qwen-3.5`, `gemma4`, `gemma-4`, and `google-gemma-4` resolve to the correct chat templates
- Bumped release patch versions: `SharpAI` package `1.0.17`, server/runtime `4.0.1`, and dashboard `4.0.1`

### Fixed

- **OpenAI streaming chat completions now use the standard response format** — The `/v1/chat/completions` streaming endpoint previously returned chunks using the text completion format (`choices[0].text`) instead of the chat completion format (`choices[0].delta.content`). This caused OpenAI-compatible clients (e.g. Mux) to silently drop streamed tokens. Streaming chunks now use `chat.completion.chunk` object type with `delta` payloads per the OpenAI specification.
- **Corrected Content-Type for streaming chat completions** — Changed from `application/x-ndjson` to `text/event-stream` (SSE) to match the OpenAI streaming protocol
- **Corrected object type for non-streaming chat completions** — Changed from `text_completion` to `chat.completion`
- **Dashboard streaming parser updated** to read from `delta.content` with fallback to `text` for backwards compatibility

### Added

- **Docker runtime hardening for CPU/CUDA deployments**
  - Added Docker/runtime environment controls for backend selection, CPU native variant selection, strict backend loading, native logging, generation threads, batch threads, GPU layer offload, main GPU selection, context/batch sizing, mmap/mlock, and flash attention
  - Added all-caps `DOTNET_GC_SERVER` Docker control, mapped by the container entrypoint to .NET's canonical `DOTNET_gcServer` runtime setting
  - Added `/health` and `/ready` operational endpoints; Docker health checks now use `/ready` so containers are marked healthy only after backend initialization, database initialization, and writable runtime directories are confirmed
  - Added a Docker entrypoint that selects the compatible x64 CPU native library variant and orders native library search paths for CPU and CUDA backends
  - Added a Docker-safe default `sharpai.json` inside the image so plain `docker run` starts with usable port, storage, logging, and database defaults
- **Apple Metal GPU acceleration** for macOS Apple Silicon (M1/M2/M3/M4)
  - Auto-detected on Apple Silicon Macs when `libggml-metal.dylib` is present
  - Configurable via `ForceBackend: "metal"` or `SHARPAI_FORCE_BACKEND=metal`
  - New `MetalBackendPath` setting for custom Metal library location
  - VRAM reporting in `/api/ps` and dashboard now works for Metal backend
  - Only available for bare-metal macOS installs, not Docker containers
- **Pure-C# GGUF metadata reader** (`GgufMetadataReader`) for lightweight capability detection
  - Reads model architecture and capabilities from GGUF file header without loading weights
  - Falls back to full engine initialization only if header read fails
  - Enables successful model registration even when llama.cpp doesn't support the architecture
- **Thinking token filter** for models like Qwen3 that emit `<think>...</think>` blocks
  - Strips thinking tokens from responses by default (both streaming and non-streaming)
  - New `display_thinking` option to show thinking tokens when desired
  - "Display Thinking" toggle in dashboard chat settings
- **Format-aware default stop sequences** derived from the model's chat template
  - ChatML models get `<|im_end|>`, Llama3 gets `<|eot_id|>`, etc.
  - Prevents completions from running indefinitely when client doesn't send stop sequences
- New `PreLoadMacOSDependencies()` in `NativeLibraryBootstrapper` for reliable macOS library loading
- Metal GPU readiness reporting in `start-mac.sh` and `diagnose-mac.sh`
- "Metal (Apple GPU)" option in dashboard Configuration page Force Backend dropdown
- Metal Backend Path configuration field in dashboard
- METAL.md implementation plan for Apple Metal GPU support

### Changed

- **Upgraded LLamaSharp from v0.25.0 to v0.26.0** with newer llama.cpp and broader architecture support
- `ForceBackend` now accepts `"metal"` in addition to `"cpu"` and `"cuda"`
- `GetOptimalGpuLayers()` now returns all-layer offload for both CUDA and Metal backends
- Updated platform support table: macOS Apple Silicon now shows GPU ✅ (Metal)
- Model pull error handling: orphaned files cleaned up on failure, full exceptions logged
- General exception catch in pull handler prevents silent 500 errors
- Embedding-only architecture list consolidated into `GgufMetadataReader.EmbeddingOnlyArchitectures` (single source of truth)
- Updated `ChatFormatHelper` model family mappings for Qwen3/3.5, Llama 3.3/4, Gemma 3, DeepSeek2, Phi-4, SmolLM
- Dashboard logo left-justified in sidebar to match nav item alignment
- Replaced `react-toggle-dark-mode` with inline SVG toggle (eliminates react-spring dependency tree warnings)

### Removed

- Vision/multimodal code (`VisionDriver`, `LLavaWeights` usage) — removed due to LLamaSharp 0.26.0 API change (LLaVA replaced by MTMD)
- Noisy "no models currently loaded" debug log on every `/api/ps` poll

---

## v4.0.0

### Server

- **Migrated from SwiftStack 0.3.3 to Watson 7.0.11** as the underlying HTTP framework, with FastAPI-style typed route handlers (`server.Get/Post/Put/Delete`) and `ApiRequest` / `WebserverException` types replacing their SwiftStack equivalents
- **Built-in OpenAPI / Swagger documentation** for every REST endpoint
  - `GET /openapi.json` — full OpenAPI 3.0 document
  - `GET /swagger` — interactive Swagger UI
  - Every Ollama, OpenAI, general, and settings route is tagged with summary, description, request body schema, and response schemas
- **New settings API**
  - `GET /api/settings` — returns the current in-memory `Settings` object
  - `PUT /api/settings` — overwrites in-memory settings and rewrites `sharpai.json` on disk; `CreatedUtc` and `SoftwareVersion` are preserved server-side
- **New capability detection** from GGUF file metadata
  - `LlamaSharpEngine` now exposes a `general.architecture` property and authoritative `SupportsEmbeddings` / `SupportsGeneration` checks derived from `general.architecture` and `general.pooling_type` GGUF keys instead of assuming every model supports both
  - A known-architecture whitelist (`bert`, `nomic-bert`, `nomic-bert-moe`, `jina-bert-v2`, `jina-bert-v3`, `t5encoder`, `gte`, `bge`, `gritlm`) marks embedding-only models; everything else is completion-capable
  - `ModelFile.Embeddings` and `ModelFile.Completions` are now set at pull time from the GGUF metadata, and `ModelFile.Family` is populated from `general.architecture` so the correct chat template is used for chat completions
  - A background startup task (`RedetectModelCapabilitiesAsync`) re-inspects every existing model on server start, corrects stale capability flags and family values in the database, and logs a detailed per-model summary with counts of inspected / updated / unchanged / skipped / failed
- **Capabilities exposed in `/api/tags`**: `ModelFile.ToOllamaModelDetails()` now includes a `capabilities: { embeddings, completions }` object alongside the Ollama-compatible fields
- **Stop sequences honored** — the Ollama and OpenAI completion / chat completion handlers now pass `gcr.Options.Stop` (or the normalized OpenAI `Stop` object) through to `LlamaSharpEngine.InferenceParams.AntiPrompts`; previously they were hard-coded to `null` and ignored
- **Model pull progress** now includes both `downloaded`, `completed`, `total`, and `percent` fields so clients can compute a progress bar regardless of which field name they expect
- **CORS preflight handler** registered at `_Server.Routes.Preflight` that responds to every OPTIONS request with `Access-Control-Allow-Origin: *`, `Access-Control-Allow-Methods`, `Access-Control-Allow-Headers`, and a 24-hour `Access-Control-Max-Age` so dashboard POST/PUT/DELETE calls from a different origin succeed
- **PostRouting debug logging** logs `METHOD URL STATUS (Nms)` for every served request
- Server console banner and `OpenApi.Info.Version` now read from `_Version = "4.0.0"`

### Dashboard

- **Migrated from Next.js 14 to Vite 5 + React Router 6**
  - Removed `next`, `@ant-design/nextjs-registry`, `eslint-config-next`, and the `next/jest` preset
  - Added `vite`, `@vitejs/plugin-react`, `vite-tsconfig-paths`, `react-router-dom@6`, and plain `@typescript-eslint` + `eslint-plugin-react-*` for linting
  - Deleted `src/app/` (Next.js App Router) and replaced it with `src/main.tsx` + `src/App.tsx` declaring routes via `<BrowserRouter>` / `<Routes>` / `<Route>` with nested dashboard layout via `<Outlet />`
  - Root `index.html` now lives in the project root with explicit `<head>` tags and a `<script type="module" src="/src/main.tsx">` entry point
  - All `"use client"` directives stripped (23 files)
  - `next/link` → `react-router-dom` `Link` (with `to=` instead of `href=`)
  - `usePathname()` → `useLocation().pathname`
  - `useRouter().push()` → `useNavigate()`
  - `next/font/google` Inter import → Google Fonts `<link>` in `index.html`
  - `AntdRegistry` SSR flicker wrapper removed — pure CSR has no SSR to hydrate
  - Dockerfile updated to multi-stage `vite build` → `vite preview` runtime
  - Dev server cold start: ~500ms (previously several seconds in Next.js)
- **New Configuration page** at `/dashboard/configuration`
  - Structured form with collapsible sections for Logging, Storage, Database, HuggingFace, REST server, Runtime, Debug, and Quantization Priority
  - Dynamic syslog server list via `Form.List`
  - HuggingFace API key rendered as a password input
  - Database, REST, and SSL sections flagged with a "Changes require server restart" warning
  - Save / Reset buttons with change tracking
  - Force Backend select exposes Auto / CPU / CUDA options
- **Running Models section** on the Models page
  - New polling query (`useGetRunningModelsQuery`, 5-second interval) against `/api/ps`
  - Table shows Name, Family, Quantization, Size, and VRAM columns
  - VRAM shows `—` with a tooltip explaining "zero when running on CPU" for CPU-only installs
- **Model pull UX overhauled**
  - Pull lifecycle moved into a dedicated `PullProgressProvider` that owns its own abort controllers and lives inside the Redux store, so navigation is never blocked
  - Dedicated "Pulling Models" table below the main models list with Model, Status, Progress bar, Size (downloaded / total), and Cancel columns
  - Cancel button aborts the underlying axios request immediately
  - Progress bar reads the `downloaded` / `completed` / `total` / `percent` fields from each NDJSON chunk, so it animates correctly while the stream is in flight
  - On completion, the local models list auto-refreshes via RTK Query cache invalidation
  - Pull Model modal shows "Cancel Pull" + "Close" buttons while a pull is in flight, and the input autofocuses after the modal's open animation settles
- **Robust NDJSON streaming parser** (`parseNdJson`) replaces the previous `replaceAll("}{", "},{")` hack. Parses each line independently so a single bad chunk no longer poisons the entire buffer and truncates displayed output.
- **Model-type filtering** on every inference page
  - Embeddings page only shows embedding-capable models
  - Completions and Chat Completion pages only show completion-capable models
  - Driven by the server-provided `capabilities` object
  - Auto-selects the only available model when exactly one qualifies
- **Completion defaults** bumped: `num_predict: 150 → 1024`, `num_ctx: 1024 → 4096`, stop list expanded to include `[INST]`, `[/INST]`, `<|im_end|>`, `<|endoftext|>`, `<|eot_id|>`, `</s>`, `user:`, `User:`, `USER:`
- **Chat improvements**
  - Animated "typing" indicator with three pulsing dots and a "Thinking…" label while waiting for the first streamed token
  - Focus automatically returns to the input after generation completes
  - "AI can make mistakes. Fact check all answers." disclaimer below every chat input
  - Response Details modal is now 1050px wide (up from 600), taller, with the Status Information and Headers sections laid out as proper 2-column grids; Headers are shown in a fixed-width font
- **New theme** based on a mid-purple (`#9333ea`) drawn from the logo gradient (replacing the previous Ant Design blue). WCAG AA compliant on both light and dark backgrounds.
- **Dark mode** uses near-black backgrounds (`#050505` base, `#101010` cards) for reduced eye strain
- **Sidebar restructure**
  - Dark (`#050505`) sidebar with the purple-tinted selected-item highlight
  - Collapse control and version string pinned to the bottom via `margin-top: auto`
  - Version display shows the dashboard's own `package.json` version, with a tooltip that reveals both the dashboard and server versions
- **Header controls**
  - GitHub icon, light/dark toggle, and Logout link, in that order, on the right
  - All three now share a fixed-height flex row with `line-height: 1` so their visual midlines align against the "Logout" text regardless of whether they're an `<a>`, a bare SVG, or an icon+text link
- **Page titles + subtitles** on every navigable page
  - `PageContainer` gained a `pageSubtitle` prop rendered beneath the main title in secondary text
  - Every navbar destination (Models, Embeddings, Completions, Chat Completion, Configuration) now has a one-paragraph description explaining what the page is for
- **Tooltips on every column, label, value, and input**
  - New centralized `src/constants/tooltips.ts` containing ~90 explanations across all pages
  - New `TooltipHeader` component renders a column label + `?` icon with hover tooltip
  - Every `Form.Item` in Configuration, Embeddings, the Pull Model modal, the Landing page, and the ChatSettings sidebar has a `tooltip=` prop
  - Every table column across the dashboard (models, running models, pulling models) has a `TooltipHeader`
  - Every standalone button (Pull models, Available Models, Clear Chat, Settings toggle, Save, Reset, Cancel Pull, etc.) is wrapped in `SharpTooltip`
  - The VRAM `—` placeholder in the Running Models table has its own tooltip so hovering the blank value explains "zero when running on CPU"

### Build, packaging, dependencies

- Dashboard `package.json` bumped to `4.0.0`
- Server `_Version` bumped to `4.0.0`; `docker/sharpai.json` and both `bin/Debug/net*/sharpai.json` files updated
- `SharpAI.csproj` bumped to `1.0.15`
- Vite `css.preprocessorOptions.scss.api = "modern-compiler"` with `silenceDeprecations: ["legacy-js-api", "import", "global-builtin"]` — silences the Sass legacy-API deprecation warnings triggered by Ant Design's internal SCSS without touching Ant Design itself

## Previous Versions

### v1.0.0

- Initial release
- Core AI inference engine based on LlamaSharp
- Support for GGUF model format exclusively
- Model management with automatic download from HuggingFace
  - Automatic GGUF file discovery and selection
  - Intelligent quantization selection based on Ollama preferences
  - SQLite-based model registry with metadata tracking
  - Model file hashing (MD5, SHA1, SHA256)
- Embedding generation capabilities
  - Single text embedding generation
  - Batch embedding generation for multiple texts
  - Automatic dimension detection
- Text completion support
  - Non-streaming completions with customizable parameters
  - Streaming completions with async enumerable support
  - Temperature and max token controls
- Chat completion functionality
  - Non-streaming chat responses
  - Streaming chat responses
  - Support for conversation history in prompts
- Comprehensive prompt formatting system
  - 10 different chat formats (Simple, ChatML, Llama2, Llama3, Alpaca, Mistral, HumanAssistant, Zephyr, Phi, DeepSeek)
  - 10 text generation formats (Raw, Completion, Instruction, QuestionAnswer, CreativeWriting, CodeGeneration, Academic, ListGeneration, TemplateFilling, Dialogue)
  - Few-shot learning support with examples
  - Context-aware prompt building
- GPU acceleration support via LlamaSharp
  - Automatic CUDA detection and optimization
  - Support for NVIDIA (CUDA), AMD (ROCm/Vulkan), Apple Silicon (Metal), Intel (SYCL/Vulkan)
  - Automatic GPU layer allocation
- Platform support
  - Tested on Windows 11, macOS Sequoia, Ubuntu 24.04
  - Minimum .NET 8.0 requirement
- SharpAI.Server project included
  - Ollama-compatible REST API endpoints
  - OpenAI-compatible REST API endpoints
- Dependencies
  - LlamaSharp for model inference
  - SyslogLogging for flexible logging
  - Watson.ORM.Sqlite for model registry
  - SwiftStack for the application platform
  - RestWrapper for HuggingFace API integration
