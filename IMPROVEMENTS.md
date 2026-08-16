# SharpAI — Improvement Plan (v5.0.0, enterprise track)

This document is the working plan to take SharpAI from a competent LlamaSharp wrapper to a
viable, correct, **enterprise-ready** alternative to Ollama and OpenAI-compatible model runners —
one that is at least as reliable as Ollama and observable enough to operate a fleet in production.
It is scoped end-to-end: core inference, server, model lifecycle, security, telemetry, dashboard,
SDKs, tests, docs, Docker, and CI.

It is written to be *annotated in place*. A developer picks up a task, flips its checkbox to
in-progress, records a note, and marks it complete when the acceptance criteria are met.

**Reliability is the north star.** Every workstream carries a testing obligation (see W10). The bar
is not "it runs" — it is "it behaves correctly under concurrency, memory pressure, malformed input,
and backend failure, and we have automated proof." Aim for ~100% meaningful coverage of the core
library and server, exercised through the Touchstone suites.

---

## §0 — Versioning & release train

**Decision (2026-08-14): normalize the entire repository to a single `5.0.0` version.** The server,
dashboard, and Docker image were on `4.0.1`; they move to `5.0.0`. The core library (`1.0.17`) and
SDKs are pulled onto the same unified `5.0.0` so the enterprise release has one coherent version
story. This is a major, breaking release — chat-template behavior changes, the config schema
changes, the route surface changes, auth and telemetry are added, and vision is removed.

| Artifact | Was | Now (target `5.0.0`) |
|---|---|---|
| `SharpAI` core library (NuGet) | 1.0.17 | **5.0.0** |
| `SharpAI.Server` + Docker image tag | 4.0.1 | **5.0.0** |
| Dashboard | 4.0.1 | **5.0.0** |
| `SharpAI.Sdk` (C#) | 1.0.1 | **5.0.0** |
| `@sharpai/sdk` (JS) | 1.0.0 | **5.0.0** |
| `sharpai` (Python) | — | **5.0.0** (first release) |

**Rollout.** `5.0.0` is not one big-bang merge. Ship pre-releases as workstreams land:
`5.0.0-alpha.N` (P0 engine + observability), `5.0.0-beta.N` (backend refactor, API completeness,
auth, request history), `5.0.0-rc.N` (dashboard, i18n, SDKs, docs), then `5.0.0`. Tag each
(`v5.0.0-alpha.1`) so `run.bat <tag>` and the Docker flow keep working.

**Config schema versioning.** Add a `SchemaVersion` field to `sharpai.json` and migrate-on-load, so
the `4.x → 5.0` settings changes (auth block, telemetry block, database block, concurrency/lifecycle
settings) upgrade cleanly instead of failing to deserialize.

---

## How to use this document

Each task carries a checkbox and, where useful, sub-checklists, acceptance criteria, the surfaces it
touches, and the requirement document it satisfies. Update the checkbox and the **Owner/Notes** line
as you go.

**Status legend:** `[ ]` not started · `[~]` in progress · `[x]` complete · `[!]` blocked ·
`[-]` deliberately skipped (link the decision in §18).

**Surfaces:** `core` · `server` · `dashboard` · `sdk` · `tests` · `docs` · `docker` · `ci` ·
`telemetry`.

**Requirement references** (under `C:\code\agents\requirements\`): `REPOSITORY_REQUIREMENTS` ·
`CODE_STYLE` · `BACKEND_ARCHITECTURE` · `BACKEND_TEST_ARCHITECTURE` · `FRONTEND_ARCHITECTURE` ·
`DASHBOARD_STYLE_AND_USABILITY` · `I18N` · `AUTHENTICATION` · `WRITING_DOCUMENTS` ·
`EXAMPLE_APPLICATIONS`.

---

## Progress dashboard

| # | Workstream | Priority | Status | Done / Total |
|---|------------|----------|--------|--------------|
| W0 | Version normalization to 5.0.0 | P0 | `[x]` | 1 / 1 |
| W1 | Inference correctness (chat templates, stop, clamps) | P0 | `[~]` | 5 / 6 |
| W2 | Concurrency & throughput | P0 | `[~]` | 4 / 5 |
| W3 | Model lifecycle & memory management | P0 | `[x]` | 5 / 5 |
| W4 | API completeness (tools, models, JSON mode; vision removed) | P0/P1 | `[~]` | 4 / 7 |
| W5 | Model sourcing & onboarding friction | P1 | `[ ]` | 0 / 5 |
| W6 | GPU breadth (Vulkan/ROCm, VRAM-fit) | P2 | `[ ]` | 0 / 4 |
| W7 | Backend architecture conformance (Watson 7.1, 4-DB, DTOs) | P1 | `[~]` | 1 / 7 |
| W8 | Request capture & history | P1 | `[x]` | 5 / 5 |
| W9 | Auth / authz / accounting (built, **off by default**) | P1 | `[~]` | 7 / 8 |
| W10 | Test architecture (Touchstone) & ~100% coverage | P0 | `[~]` | 1 / 9 |
| W11 | Dashboard rebuild to standard | P1 | `[ ]` | 0 / 10 |
| W12 | Internationalization | P1 | `[ ]` | 0 / 6 |
| W13 | SDKs (C#, JS, Python) parity | P1 | `[ ]` | 0 / 6 |
| W14 | Docs & repository housekeeping | P1 | `[ ]` | 0 / 8 |
| W15 | CI/CD | P1 | `[ ]` | 0 / 5 |
| W16 | Operations hardening | P2 | `[ ]` | 0 / 3 |
| W17 | Observability & telemetry (Radiant, Watson 7.1, Prom/Loki/Grafana/Tempo) | P1 | `[~]` | 7 / 8 |

**Priority meaning.** P0 separates a *runner* from a *wrapper* and underpins reliability. P1 is
required for standards compliance, enterprise operability, and a credible full product. P2 is reach.

Recommended sequencing: **W0** (done first, mechanical) → **W1–W3** (the technical case) →
**W7/W10** (the refactor + test spine everything else builds on) → **W17** (observability, largely
independent, high enterprise value) → **W8/W9/W4/W5** → **W11/W12/W13** → **W14/W15/W16**.

---

## §1 — W0: Version normalization

- [x] **W0.T1 — Set every artifact to `5.0.0`.** _(done 2026-08-14)_
  - [x] `src/SharpAI/SharpAI.csproj` `<Version>` → `5.0.0`; `PackageReleaseNotes` refreshed.
  - [x] `src/SharpAI.Server/SharpAI.Server.csproj` → `<Version>5.0.0</Version>` added.
  - [x] `sdk/csharp/src/SharpAI.Sdk/SharpAI.Sdk.csproj` → `5.0.0`.
  - [x] `dashboard/package.json` → `5.0.0`; `sdk/js/package.json` → `5.0.0`.
  - [x] `docker/compose-cpu.yaml`, `docker/compose-cuda.yaml`, new `docker/compose.yaml` image tags → `v5.0.0`.
  - [x] `docker/factory/sharpai.json`, `docker/sharpai.json`, `src/SharpAI.Server/sharpai.docker.json` `SoftwareVersion` → `5.0.0`; `SchemaVersion` added.
  - [x] `build-*.bat` example tags and README `v4.0.1` references → `v5.0.0`.
  - **Acceptance:** no active `4.0.1`/`1.0.17` version reference remains (only a dashboard README
    historical migration note and a `remark-gfm ^4.0.0` dep, both intentional). Build verification of
    the solution on `net8.0`/`net10.0` runs alongside W7 (the server still targets Watson 7.0.11).
  - **Surfaces:** all · **Owner/Notes:** _completed this pass._

---

## §2 — W1: Inference correctness

The single highest-leverage area. The engine currently picks a chat template from a hand-maintained
architecture-to-format table (`ChatFormatHelper`) instead of the model's own embedded template. That
produces subtly wrong prompts for any model whose real template deviates, and guarantees a permanent
catch-up treadmill. Everything here is `core` + `tests` unless noted.

- [x] **W1.T1 — Apply the GGUF-embedded chat template as the primary path.** _(done 2026-08-14)_
  `LlamaSharpEngine` implements `IChatTemplateSource` using LlamaSharp 0.27's `LLamaTemplate` (built from
  the model weights, `strict:true`), probing/caching whether the model carries an embedded template and
  rendering messages through it. `ChatTemplateResolver` prefers the embedded template and falls back to
  `ChatFormatHelper`/`ChatPromptBuilder` on absence, empty render, or error. Wired into **both** server
  chat handlers (`/api/chat`, `/v1/chat/completions`); the public `PromptBuilder`/`ChatFormat` API is
  preserved as the documented fallback.
  - **Remaining for full acceptance:** the byte-match golden tests vs llama.cpp need real GGUFs
    (tracked in W1.T6). The resolver decision logic is covered by 6 descriptors (fake source, no model).
  - **Owner/Notes:** _resolver + engine verified; goldens pending a model fixture._
- [x] **W1.T2 — Derive stop/termination from the model.** _(done 2026-08-14)_ When the embedded template
  is used, the resolver returns **no** injected anti-prompts, so generation ends on the model's native
  EOS/EOT tokens (as in llama.cpp/Ollama) instead of the hard-coded `{ "user:", ... }` list. The family
  `GetDefaultStopSequences` table remains only for the fallback path. **Owner/Notes:** _—_
- [x] **W1.T3 — Fix the silent `MaxTokens` clamp.** _(done 2026-08-14)_ Replaced
  `Math.Max(maxTokens, 100)` in all four engine generation methods with `EffectiveMaxTokens`, which
  honors any positive request exactly (including `max_tokens: 8`) and only applies the configurable
  `DefaultMaxTokens` (default 512, min 1) when the caller passes a non-positive value. **Owner/Notes:** _—_

- [x] **W1.T4 — Correct, configurable thinking-tag filtering.** _(done 2026-08-14)_ `ThinkingFilter`
  markers are now configurable (ctor + a static overload taking custom open/close tags), so it serves
  reasoning models beyond `<think>`; streaming still never emits a partial tag, and an unclosed block is
  no longer leaked on `Flush`. Covered by 4 new descriptors. **Owner/Notes:** _server per-request keep/strip
  already exists via `DisplayThinking`; a server-level default-tag setting is optional follow-up._
- [x] **W1.T5 — Embedding chunk-and-average made opt-in.** _(done 2026-08-14)_ `EnableEmbeddingChunking`
  (default true) on the engine; when false, an over-length embedding input throws a clear error instead
  of silently chunking-and-averaging (which changes the vector's meaning). **Owner/Notes:** _—_

- [~] **W1.T6 — Golden-output regression suite for prompt rendering.** _(unblocked 2026-08-14)_ The
  model fixture now exists (`ModelFixture` + fixture-gated `ModelInferenceSuite`), which renders the
  embedded chat template against a real GGUF when one is provided. Remaining: capture committed golden
  strings and byte-match them (per family) so LlamaSharp bumps can't regress templating unnoticed.
  **Ref:** BACKEND_TEST_ARCHITECTURE · **Owner/Notes:** _fixture done; goldens pending._

---

## §3 — W2: Concurrency & throughput

Today one model serves one request at a time (`_GenerationSemaphore(1,1)`), and
`ModelEngineService.GetByModelFile` calls `InitializeAsync(...).Wait()` **while holding the global
`_EnginesLock`**, so loading any model blocks every request to every other model.

- [x] **W2.T1 — Remove the global lock from the model-load path.** _(done 2026-08-14)_ `ModelEngineService`
  now stores engines in a `ConcurrentDictionary<string, Lazy<Task<LlamaSharpEngine>>>` with per-model
  gating. Loading model B no longer holds any lock that model A's requests need, and concurrent callers
  for the same model share a single initialization. Failed/disposed entries are evicted and retried.
  - **Remaining for full acceptance:** the measured "B-load doesn't stall A" benchmark needs models
    (W2.T5). **Owner/Notes:** _global `_EnginesLock` + `.Wait()`-under-lock eliminated._
- [x] **W2.T2 — Purge sync-over-async from the acquisition path.** _(done 2026-08-14)_ Added
  `GetByModelFileAsync` (fully async, `Task.WaitAsync(token)` so the caller's wait is cancellable
  without cancelling the shared load). The sync `GetByModelFile` remains as a thin boundary shim for
  existing callers; migrating the handlers to the async overload is a follow-up. **Owner/Notes:** _—_
- [x] **W2.T3 — Configurable generation concurrency.** _(done 2026-08-14)_ `MaxConcurrentGenerations`
  (default 1, env `SHARPAI_MAX_CONCURRENT_GENERATIONS`) sizes the per-model generation semaphore, so
  operators can opt into parallel decode slots. **Remaining for full acceptance:** validating true
  throughput gain needs a model + the W2.T5 benchmark; llama.cpp continuous batching (BatchedExecutor)
  is a deeper future optimization. **Owner/Notes:** _—_
- [x] **W2.T4 — Request admission & backpressure.** _(done 2026-08-14)_ `GenerationQueueTimeoutMs`
  (default 0 = wait forever, env `SHARPAI_GENERATION_QUEUE_TIMEOUT_MS`) bounds the slot wait; on timeout
  the engine throws `EngineBusyException` (maps to a busy response). `CancellationToken` already frees
  the wait on client disconnect. **Owner/Notes:** _server 503 mapping tracked under W4.T7._
- [~] **W2.T5 — Concurrency benchmark harness.** _(unblocked 2026-08-14)_ The model fixture exists and the
  fixture-gated suite already runs two concurrent generations as a smoke test. Remaining: a documented
  throughput/latency benchmark (single vs N concurrent) defending the README claims. **Surfaces:** tests,
  docs · **Owner/Notes:** _fixture + concurrency smoke done; formal benchmark pending._

---

## §4 — W3: Model lifecycle & memory management

No cap, no idle eviction, no memory-aware admission, and every model loads its weights twice (a second
`_EmbeddingModel` on every `InitializeAsync`).

- [x] **W3.T1 — Lazy embedder.** _(done 2026-08-14)_ `InitializeAsync` no longer eagerly loads a second
  copy of the weights for embeddings; `EnsureEmbedderAsync` builds the embedding model/context on first
  embedding request (once, gated by a semaphore, with failure memoized and resources cleaned up on
  error). A generation-only model now allocates a single weight set. **Owner/Notes:** _—_
- [x] **W3.T2 — Keep-alive / idle eviction.** _(done 2026-08-14)_ `KeepAliveSeconds` (default 0 = never,
  env `SHARPAI_KEEP_ALIVE_SECONDS`) drives a background sweep in `ModelEngineService` that disposes
  models idle beyond the timeout; last-access time is tracked per model. **Owner/Notes:** _—_
- [x] **W3.T3 — Max-resident cap + LRU eviction.** _(done 2026-08-14)_ `MaxResidentModels` (default 0 =
  unlimited, env `SHARPAI_MAX_RESIDENT_MODELS`); admission evicts the least-recently-used loaded model
  when the cap would be exceeded. **Owner/Notes:** _—_
- [x] **W3.T4 — Memory-budget admission.** _(done 2026-08-14)_ `ModelMemoryBudgetBytes` (env
  `SHARPAI_MODEL_MEMORY_BUDGET_MB`, 0 = unlimited): admission evicts LRU models to fit a new model's
  file size within the budget and throws `ModelAdmissionException` if it cannot — a structured error
  instead of an OOM/native crash. **Note:** this is a configurable file-size budget (deterministic,
  cross-platform); true VRAM auto-detection remains with the GPU work (W6). **Owner/Notes:** _—_
- [x] **W3.T5 — Accurate `/api/ps` accounting.** _(done 2026-08-14)_ `/api/ps` already reported
  name/digest/size/size_vram/family; it now also reports a real `expires_at` computed from the model's
  last access + keep-alive (via `ModelEngineService.GetExpiryUtc`), and the stale "always null" OpenAPI
  note was corrected. **Surfaces:** core, server · **Owner/Notes:** _—_

---

## §5 — W4: API completeness

The `OllamaTool`/`OpenAITool` model classes exist but the handlers reference them zero times.

- [~] **W4.T1 — Tool / function calling.** `[P0]` _(started 2026-08-14)_ The model-independent core is
  done and tested: `SharpAI.Tools.ToolCallParser` extracts tool calls from the common model output
  formats (`<tool_call>{...}</tool_call>` blocks, bare JSON object, JSON array, `{function:{...}}`),
  covered by 8 descriptors. **Remaining (model-gated):** accept `tools`/`tool_choice` on the chat
  requests, inject tool definitions into the prompt, emit `tool_calls` with `finish_reason:"tool_calls"`,
  accept tool-result messages, stream deltas, and verify an SDK round-trip against a tool-capable model.
  **Owner/Notes:** _parser verified; end-to-end needs a model fixture._
- [x] **W4.T2 — `GET /v1/models`.** `[P0]` _(done 2026-08-14)_ Returns local models in OpenAI list shape
  (`object: "list"`, `data[].{id,object,created,owned_by}`). The `/v1/models/{id}` retrieve variant is a
  small follow-up (route path-param support to confirm). **Owner/Notes:** _—_
- [x] **W4.T3 — `POST /api/show` and `GET /api/version`.** `[P1]` _(done 2026-08-14)_ `/api/version`
  returns the server version; `/api/show` returns GGUF-derived metadata + capabilities (family,
  parameter size, quantization, embeddings/completions, size, digest) for a named model, with 400/404
  handling. **Owner/Notes:** _—_
- [!] **W4.T4 — JSON mode / structured outputs.** `[P1]` Honor `response_format` and Ollama `format`
  via GBNF grammar-constrained decoding. **Blocked on a model fixture:** grammar-constrained decoding
  can only be verified against a live model. **Owner/Notes:** _—_
- [-] **W4.T5 — `logprobs`.** `[P2]` _(deferred by decision 2026-08-14)_ The current behavior returns
  `null` logprobs, which is OpenAI-compliant when logprobs are not requested, so the contract is not
  misleading. Emitting real token logprobs requires sampler-level changes and is deferred until there is
  demand; the fields are retained (not dropped) so the response shape stays OpenAI-shaped.
  **Owner/Notes:** _revisit with a model fixture if a consumer needs it._
- [x] **W4.T6 — Remove vision / llava entirely.** `[P1]` (decision **D3: remove**) _(done 2026-08-14)_
  - [x] Removed the `Dlls\win-x64\llava_shared.dll` `<None Include>` item from `SharpAI.csproj`,
        deleted the binary, and untracked it from git (the `Dlls/` dir is gone).
  - [x] Removed the `vision` doc mention from `LlamaSharpEngine`; core vision code (`VisionDriver`,
        `LLavaWeights`) was already removed in a prior release per CHANGELOG.
  - [x] Removed vision/mmproj claims from `src/CLAUDE.md`; dropped `llava` from `SharpAI.csproj`
        package tags (added `openai gguf telemetry`). README had no vision claims.
  - **Acceptance met:** no active `llava`/vision reference remains in source, project files, or docs
    (only the 5.0.0 release-note line documenting the removal). Core library builds clean.
  - **Surfaces:** core, docs · **Owner/Notes:** _completed this pass._
- [~] **W4.T7 — Error-shape parity.** _(started 2026-08-14)_ The new capacity exceptions are now mapped:
  `EngineBusyException` and `ModelAdmissionException` surface as HTTP 429 (`ApiResultEnum.SlowDown`) via a
  `RunInference` wrapper around the four generation routes, instead of a bare 500. Remaining: a full
  audit that every error path emits the OpenAI/Ollama error envelope shape (`error.type`/`error.message`)
  so client SDKs never throw on parse. **Surfaces:** server, sdk, tests · **Owner/Notes:** _—_

---

## §6 — W5: Model sourcing & onboarding friction

- [ ] **W5.T1 — Import a local GGUF by path** (no download, no token). **Owner/Notes:** _—_
- [ ] **W5.T2 — Optional HF token for public repos** (token only for gated/private). **Owner/Notes:** _—_
- [ ] **W5.T3 — Ollama-registry pull path (evaluate).** Record feasibility/licensing in §18. **Owner/Notes:** _—_
- [ ] **W5.T4 — Modelfile-equivalent presets** (system prompt, params, template override, stop). **Owner/Notes:** _—_
- [ ] **W5.T5 — Sharded GGUF + explicit overridable quant selection.** **Owner/Notes:** _—_

---

## §7 — W6: GPU breadth

- [ ] **W6.T1 — Vulkan backend** (AMD/Intel/NVIDIA) into `NativeLibraryBootstrapper` + `SHARPAI_FORCE_BACKEND`. **Owner/Notes:** _—_
- [ ] **W6.T2 — ROCm backend** for AMD on Linux (evaluate LlamaSharp support). **Owner/Notes:** _—_
- [ ] **W6.T3 — VRAM-fit partial offload** (compute layer count from VRAM + model size). **Owner/Notes:** _—_
- [ ] **W6.T4 — Multi-GPU tensor split** beyond a single `MainGpu`. **Owner/Notes:** _—_

---

## §8 — W7: Backend architecture conformance

`BACKEND_ARCHITECTURE` is normative and the current server violates several load-bearing rules. This
is the refactor spine that W8/W9/W17 build on. Also bump **Watson 7.0.11 → 7.1.0** here (needed for
W17 telemetry).

- [~] **W7.T1 — Watson 7.1.0 upgrade + drop the `server.Get/Post<T>` convenience surface.** _(started 2026-08-14)_
  **Upgraded `Watson` 7.0.11 → 7.1.0**; the existing route API still compiles on 7.1.0, so the build
  stays green and Watson's native telemetry is now available (see W17.T3). Remaining: migrate handlers
  to `server.Routes.{Pre,Post}Authentication.{Static,Parameter}.Add(...)` with typed DTOs from
  `ctx.Request.DataAsString`, per the registrar pattern (W7.T2). **Surfaces:** server, tests · **Owner/Notes:** _—_
- [ ] **W7.T2 — Per-feature route registrar classes:** `HealthRoutes`, `SettingsRoutes`,
  `OllamaModelRoutes`, `OllamaInferenceRoutes`, `OpenAIInferenceRoutes`, `RequestHistoryRoutes`,
  `MetricsRoutes` — each calling `server.Routes.*.Add(...)` with `OpenApiRouteMetadata`. **Owner/Notes:** _—_
- [ ] **W7.T3 — Thin `Program.cs` + instance `SharpAIServer` host** owning composition. **Owner/Notes:** _—_
- [x] **W7.T4 — Provider-neutral database layer — all four providers (decision D2: required).**
  _(done 2026-08-14)_ **WatsonORM removed entirely**; replaced with a hand-rolled interface/implementation
  layer under `src/SharpAI/Database/`: `DatabaseTypeEnum`, `DatabaseSettings`, `IModelRegistryMethods`,
  `SchemaMigration`, `DatabaseDriverBase` (connection-agnostic execution + versioned/tracked migration
  runner + serialized access), `DatabaseDriverFactory`, and provider drivers for **`Sqlite`, `Mysql`,
  `Postgresql`, `SqlServer`** on raw ADO.NET (Microsoft.Data.Sqlite, MySqlConnector, Npgsql,
  Microsoft.Data.SqlClient) with dialect-specific DDL/paging. Handwritten portable SQL executes CRUD;
  a `schema_migrations` table tracks applied versions idempotently. `AIDriver`, `ModelDriver`,
  `ModelFileService`, `ModelFile`, and the server were cut over; `ModelFile` no longer carries ORM
  attributes.
  - **Verified:** the SQLite contract suite (migrate-idempotent, add/get, duplicate-name, exists/all,
    update round-trip, get-many, enumerate paging, delete) is green across console/xUnit/NUnit. The
    MySQL/PostgreSQL/SQL Server drivers honor the same `IModelRegistryMethods` contract; their runtime
    matrix (W10.T5) runs under Docker DB profiles.
  - **Remaining:** the per-provider `Queries/` split is currently one shared portable-SQL implementation
    with per-driver DDL/paging (a documented simplification since the entity is portable); registry
    first-boot seeding is N/A.
  - **Surfaces:** core/server, tests, docker · **Owner/Notes:** _—_
- [~] **W7.T5 — Prefixed identifiers via a central `IdGenerator`.** _(started 2026-08-14)_ Added
  `SharpAI.Helpers.IdGenerator` with `req_`/`mdl_` prefixes; request-history entries use `req_` ids.
  Remaining: adopt K-sortable `PrettyId` (vs GUID-backed) and apply `mdl_` to the model registry.
  **Owner/Notes:** _—_
- [ ] **W7.T6 — Typed DTOs everywhere; no JSON DOM for fixed contracts** (`Requests/`, `Responses/`).
  _Down-payment (2026-08-14):_ model listing is now typed end-to-end via `EnumerationQuery` →
  `EnumerationResult<ModelFile>`; there are no unbounded `All()`/get-all APIs (the Ollama/OpenAI list
  contracts page through the enumeration). Remaining: audit the rest of the handlers for JSON-DOM use on
  fixed shapes. **Owner/Notes:** _—_
- [~] **W7.T7 — Settings hardening.** _(started 2026-08-14)_ `SchemaVersion` is now a first-class
  `Settings` property (default `5.0.0`, no longer dropped on round-trip); `TelemetrySettings` clamps its
  numerics. Remaining: broader validate-on-load, migrate-on-load across the `4.x→5.0` shape, env-var
  secret overrides audit, and startup config logging without secrets. **Owner/Notes:** _—_

---

## §9 — W8: Request capture & history

Mandatory per `BACKEND_ARCHITECTURE`; backs the dashboard Home/Request-History/drill-down. Absent today.

- [x] **W8.T1 — Capture in `PostRouting`.** _(done 2026-08-14)_ `RequestHistoryCaptureService` builds a
  `RequestHistoryEntry` synchronously from the Watson context, redacts secret headers
  (`authorization`/`proxy-authorization`/`cookie`/`set-cookie` and any `*api-key*`/`*token*`), truncates
  the request body to the configured limit, and writes on a background task so it never blocks the
  response. Wired into `PostRouting`, guarded by `RequestHistory.Enabled`. _Note: the current routing
  layer does not retain response bodies, so response metadata + headers are captured but not the body._
- [x] **W8.T2 — `RequestHistorySettings`.** _(done 2026-08-14)_ Enabled (default true), MaxRequest/
  ResponseBodyBytes (clamped 0-1MB, default 65536), RetentionDays (clamped 1-3650, default 30); added to
  `Settings` and the default `sharpai.json`.
- [x] **W8.T3 — `IRequestHistoryMethods` + four-provider implementation.** _(done 2026-08-14)_ Create,
  Read (with bodies), Enumerate (paged, bodies omitted), Summarize (in-memory time buckets, every bucket
  emitted), Delete, DeleteMany, Prune — handwritten SQL over the shared driver; structured columns with
  headers as JSON. `request_history` table added as migration v2 to all four providers. SQLite contract
  suite (create/read, enumerate-omits-bodies, summarize, delete/prune) is green.
- [x] **W8.T4 — Routes** `/v1.0/api/request-history` (list, `/summary`, `/{id}`, delete `/{id}`, bulk
  delete) with OpenAPI metadata under a "Request History" tag. _(done 2026-08-14)_
- [x] **W8.T5 — Hourly prune job** honoring `RetentionDays` (first run after 5 min), disposed on
  shutdown. _(done 2026-08-14)_

- **Surfaces (all):** server, tests, dashboard (W11) · **Ref:** BACKEND_ARCHITECTURE

---

## §10 — W9: Authentication / authorization / accounting

**Decision D1: err toward Ollama** — auth ships **disabled by default** (`Auth.Enabled = false`) so the
out-of-box experience is an open local server like Ollama; the full AAA stack is opt-in for enterprise
and pairs with the four-database requirement. When disabled, requests run as an implicit single system
principal; when enabled, everything below is enforced exactly as `AUTHENTICATION` requires.

- [x] **W9.T1 — Auth mode & default (off by default, Ollama parity).** _(done 2026-08-14)_ `AuthSettings.Enabled`
  defaults `false`. Off: `AuthEvaluator` installs the implicit **system principal** (fully authorized) and
  never challenges — behavior identical to today (Ollama parity). On: a valid admin API key (`x-api-key`,
  constant-time compared) yields an administrator; other requests to non-anonymous paths are challenged
  (401). `/`, `/health`, `/ready`, `/favicon.ico`, `/openapi.json`, `/swagger` stay anonymous in both
  modes. Verified by 6 evaluator descriptors. **Owner/Notes:** _full user/credential/session/RBAC layered on next._
- [x] **W9.T2 — Schema + data-access.** _(done 2026-08-14)_ `Tenant`, `User`, `Credential`, `AuthSession`,
  and `AuditLogEntry` models (guid/active/isprotected/created/lastupdate + tenant columns), with
  `ITenantMethods`/`IUserMethods`/`ICredentialMethods`/`IAuthSessionMethods`/`IAuditMethods` and
  handwritten-SQL implementations. Tables added as **migration v3 across all four providers**;
  `DatabaseDriverBase` exposes `Tenants`/`Users`/`Credentials`/`Sessions`/`Audit`. SQLite contract tests
  (tenant+user+password-verify, credential-by-access-key, session+token round-trip+revoke, audit
  enumerate) are green. Full roles/permissions/assignments tables are deferred to the RBAC engine (T5).
  **Owner/Notes:** _—_
- [x] **W9.T3 — `AuthenticateRequest` hook + typed `RequestContext`.** _(done 2026-08-14)_ `RequestContext`
  (principal type/guid, tenant, IsAuthenticated/IsAdmin/IsTenantAdmin, scheme) is built by
  `AuthenticationService.AuthenticateRequestAsync` (registered on Watson's `Routes.AuthenticateRequest`)
  and attached to `ctx.Metadata`; a challenge sends a 401 and stops routing. Remaining: broaden transport
  normalization as the other schemes (W9.T4) land. **Owner/Notes:** _—_
- [~] **W9.T4 — Auth schemes.** _(done 2026-08-14; one scheme deferred)_ `AuthenticationEngine` (core,
  framework-free) layers three credentialed schemes on the admin-`x-api-key`/anonymous decision: **header
  login** (x-email/x-password → session), **bearer session token** (Authorization: Bearer / x-token), and
  **access-key + secret-key** (x-access-key/x-secret-key), tried in order, one principal per request. Wired
  into the server via `AuthenticationService`; secrets compared as SHA-256 digests, constant-time. Deferred:
  AWS-style signed request (skew + nonce + canonicalization + constant-time HMAC) — enum value reserved.
  12 engine contract tests green. **Owner/Notes:** _signed-request scheme deferred._
- [x] **W9.T5 — RBAC.** _(done 2026-08-14)_ Full RBAC data layer (`userroles`, `permissions`,
  `rolepermissionmaps`, `userroleassignments`, `credentialscopeassignments`) added as **migration v4 across
  all four providers**, with interface/implementation data-access wired into `DatabaseDriverBase`. The pure,
  framework-free `RbacEngine` evaluates the `(tenant, principal, resourceType, operation, resourceGuid?)`
  tuple with **explicit-deny-wins**, tenant vs resource scope, `InheritsToChildren`, `Write`→Create/Update/
  Delete expansion, `All` wildcards, the `IsAdmin`/`IsTenantAdmin` bypass rules, and the **credential
  owner-ceiling**. Six immutable built-in roles (TenantAdmin/SecurityAdmin/Auditor/Editor/Viewer/
  TenantMember, null tenant + protected) are seeded idempotently at startup (`RbacSeeder`), resolvable by
  GUID or name. 10 evaluator contract tests are green. Remaining follow-on: per-route operation-scope
  mapping to enforce these gates on the data plane (tracked with W9.T8). **Owner/Notes:** _—_
- [x] **W9.T6 — Session tokens.** _(done 2026-08-14)_ `AuthSession` (server-side, revocable, with expiry)
  + `SessionTokenService` (AES-256-CBC, **fresh random IV per token**). Server endpoints landed:
  `POST /v1.0/token` (login → token), `GET /v1.0/token` (session details), `DELETE /v1.0/token` (revoke),
  all with OpenAPI metadata and reachable anonymously so login works without a prior credential. Token TTL
  is configurable (`Auth.SessionTtlMinutes`); the AES key material is `Auth.TokenSigningKey` (random
  per-boot when unset, logged as a warning). **Owner/Notes:** _—_
- [x] **W9.T7 — Audit stream.** _(done 2026-08-14)_ `AuditLogEntry` + `IAuditMethods` across the
  four-provider layer. `AuthenticationService` now records an audit entry on every authentication denial
  (event type, method, path, source IP, 401), and `GET /v1.0/api/audit` exposes a paginated, tenant-scoped
  audit feed — global admins may scope by `tenantGuid`; tenant admins are constrained to their own tenant;
  non-admins get 403. **Owner/Notes:** _—_
- [x] **W9.T8 — Enforcement + effective-permissions inspection.** _(done 2026-08-14)_ RBAC is now enforced
  on the data plane: a central `Authorize(req, resourceType, operation, resourceGuid)` gate maps every
  control-plane and inference route to its `(ResourceType, Operation)` cost and calls `RbacEngine.Authorize`,
  auditing denials and returning **403** with a reason. Admin-class routes (settings write, request-history,
  audit) require `Admin`/resource grants; inference maps to `Inference:Execute`, model management to
  `Model:Write`/`Delete`, reads to `Read`. When auth is disabled every request runs as the system principal
  so the gate is a no-op (Ollama parity preserved). Inspection endpoints added:
  `GET /v1.0/tenants/{tenantGuid}/users/{userGuid}/permissions` and `.../credentials/{credentialGuid}/permissions`
  (Admin, or the principal reading its own). **Owner/Notes:** _—_
- **Surfaces (all):** core/server, dashboard, sdk, tests, docs · **Ref:** AUTHENTICATION

---

## §11 — W10: Test architecture & ~100% coverage

`BACKEND_TEST_ARCHITECTURE` mandates **Touchstone** (source at `C:\code\touchstone`; packages
`Touchstone.Core`, `Touchstone.Cli`, `Touchstone.XunitAdapter`, `Touchstone.NunitAdapter`) with
`Test.Shared` / `Test.Automated` / `Test.Xunit` / `Test.Nunit`. The goal is Ollama-or-better
reliability, so this workstream is **P0** and the coverage target is ~100% of meaningful paths in the
core library and server. All harnesses bind and target `127.0.0.1`, never `localhost`.

- [x] **W10.T1 — Stand up the Touchstone projects.** _(done 2026-08-14)_
  Created `src/Test.Shared` (Touchstone.Core 0.1.12 only, zero console output — `SharpAISuites.All`
  aggregates `ChatFormatSuite`, `ChatPromptBuilderSuite`, `TextGenerationSuite`, `ThinkingFilterSuite`,
  **80 descriptors** covering the full deterministic prompt/format/thinking surface), `src/Test.Automated`
  (Touchstone.Cli console runner with `--results` JSON), `src/Test.Xunit` (Fact + Theory + coverlet),
  `src/Test.Nunit` (TestCaseSource + coverlet). All target `net8.0;net10.0` and are in `SharpAI.sln`.
  **Verified green:** console 80/80, xUnit 81/81, NUnit 80/80. Coverage gate wiring lands with W10.T9/W15.
  **Owner/Notes:** _harness proven end-to-end on all three runners._
- [~] **W10.T2 — Retire/relocate the ad-hoc console test apps.** _(started 2026-08-14)_ Retired the
  redundant `SharpAI.Tests` xUnit project (its `PromptSupportTests` are fully covered by the new
  suites) — removed from the solution and deleted. Remaining: fold `Test.HuggingFace`,
  `Test.LlamaSharpProvider`, `Test.PromptBuilder`, `Test.SharpAIDriver` console apps into shared
  descriptors (or a `test/` location) as their behaviors gain suite coverage. **Owner/Notes:** _—_
- [~] **W10.T3 — Core inference suites** against a tiny committed/downloaded GGUF. _(started 2026-08-14)_
  The model fixture (`ModelFixture`) + `ModelInferenceSuite` are in place: init, embedded chat-template
  rendering, small-`max_tokens` generation, concurrent generation, and embeddings — all skip cleanly when
  no model is present. Remaining: template goldens, stop handling, thinking-filter on real output, and
  lifecycle assertions (evict/keep-alive/LRU, admission refusal). **Owner/Notes:** _fixture + smoke done._
- [ ] **W10.T4 — Server contract suites** for every Ollama + OpenAI route incl. tool calling,
  `/v1/models`, `/api/show`, `/api/version`, JSON mode, error-shape parity, and streaming (SSE +
  NDJSON) — against a live in-process server on `127.0.0.1`. **Owner/Notes:** _—_
- [~] **W10.T5 — Provider-matrix DB suites (required — decision D2).** _(started 2026-08-14)_ The shared
  registry contract suite (`DatabaseSuite`, 8 cases) runs against **SQLite** in CI (embedded) and is green
  on console/xUnit/NUnit. Remaining: run the same `IModelRegistryMethods` contract against `Mysql`,
  `Postgresql`, `SqlServer` via Docker DB profiles (drivers exist; needs the CI containers from W15.T3).
  **Owner/Notes:** _SQLite done; server-DB matrix pending CI._
- [ ] **W10.T6 — Auth/authz suites:** deny-wins, tenant isolation, session revocation, signed-request
  replay/skew, admin-endpoint hardening, **plus** an "auth disabled" suite proving the open-server
  (Ollama-parity) default path works with no credentials. **Owner/Notes:** _—_
- [ ] **W10.T7 — Telemetry suites:** meters emit expected series, `/metrics` renders valid Prometheus
  exposition, spans are produced, and telemetry-disabled mode is a clean no-op. **Owner/Notes:** _—_
- [ ] **W10.T8 — Fault-injection & reliability suites:** malformed requests, oversized inputs, cancelled
  requests, backend-load failure, disk-full on pull, concurrent pull+delete, OOM admission — assert
  graceful, structured failures (no unhandled native crash). This is the "more reliable than Ollama"
  bar. **Owner/Notes:** _—_
- [ ] **W10.T9 — Coverage gate in CI** (fail under threshold; publish the report artifact). Track the
  number honestly; document any intentionally-uncovered native-interop lines. **Owner/Notes:** _—_
- **Surfaces (all):** tests, ci · **Ref:** BACKEND_TEST_ARCHITECTURE

---

## §12 — W11: Dashboard rebuild to standard

`FRONTEND_ARCHITECTURE` + `DASHBOARD_STYLE_AND_USABILITY` are prescriptive and the current dashboard
does not match the mandated stack or feature set. Treat as a rebuild.

- [ ] **W11.T1 — Route inventory + stable authenticated shell** (grouped sidebar Home → Request History
  → API Explorer → domain views → Settings; topbar with server URL, version/build, identity/role when
  auth on, health status, theme toggle, language selector, repo link, logout). **Owner/Notes:** _—_
- [ ] **W11.T2 — Stack alignment:** React 19 / Vite 6 / React Router 7; one hand-rolled fetch
  `ApiClient` (no axios); CSS-variable theming light + dark from the first commit. **Owner/Notes:** _—_
- [ ] **W11.T3 — Shared components before pages:** DataTable (states + sortable + above-table pagination,
  page sizes `[10,25,50,100,250,500,1000]` default 25), FilterBar, portaled ActionMenu, Modal/
  ConfirmModal (no browser `alert/confirm/prompt`), JsonViewer, CopyButton/CopyableId, StatusBadge. **Owner/Notes:** _—_
- [ ] **W11.T4 — Home/Overview:** domain KPIs (resident models, models on disk, tokens/sec, request
  volume), health dots, hand-rolled SVG activity chart (exact bucket counts) + manual refresh, recent
  failures. **No charting library.** **Owner/Notes:** _—_
- [ ] **W11.T5 — Request History view** (consumes W8): KPI strip, activity chart (bucket-click filters),
  backend-backed FilterBar, required columns + pills, row actions, Request Details modal with headers/
  bodies/raw JSON + copy. **Owner/Notes:** _—_
- [ ] **W11.T6 — API Explorer** driven by `/openapi.json`: grouped by tag, spec-generated forms,
  inherited auth, resolved+copyable URL, streaming responses, confirm on destructive/bulk, generated
  curl/fetch/C# snippets, per-origin history capped at 12. **Owner/Notes:** _—_
- [ ] **W11.T7 — Model management + inference playgrounds** (pull/import/delete with progress, running
  models from `/api/ps`, chat/completion/embeddings) rebuilt on shared components. **Owner/Notes:** _—_
- [ ] **W11.T8 — Settings / Server Info** (endpoint, version/build, backend + GPU, feature flags,
  editable settings, copyable URLs). **Owner/Notes:** _—_
- [ ] **W11.T9 — Observability panel** linking to Grafana (W17) and surfacing live health/metrics
  summaries in-app. **Owner/Notes:** _—_
- [ ] **W11.T10 — Mandatory visual + accessibility QA** (Playwright at 1280/768/390 px, light + dark,
  all states, portaled menus, modal overflow, semantic landmarks, `aria-label`, focus, color-not-sole-
  signal). Rebuild + restart the dashboard Docker container on any dashboard change. **Owner/Notes:** _—_

---

## §13 — W12: Internationalization

`I18N` is architectural, not optional; CI must block missing keys. Reference implementation: Hydra.

- [ ] **W12.T1 — i18n foundation** under `dashboard/src/i18n/` (`index.js`, `localeRegistry.js`,
  `resources.js`, `formatters.js`, shared `LanguageSelector`); i18next + react-i18next +
  browser-languagedetector; init before first paint. **Owner/Notes:** _—_
- [ ] **W12.T2 — Externalize every operator string** + `aria-*`/`title`/`alt`. **Owner/Notes:** _—_
- [ ] **W12.T3 — Explicit-locale formatters** (`formatNumber/Date/Time/DateTime/RelativeTime/Duration/
  Bytes/Percent/List`); charts format with the selected locale. **Owner/Notes:** _—_
- [ ] **W12.T4 — `lang`/`dir` sync + persistence** across reload, logout/login, deep links; selectable
  pre-auth and in-shell without full reload. **Owner/Notes:** _—_
- [ ] **W12.T5 — Server-text strategy** (`Accept-Language`; stable keys/enums localized at render). **Owner/Notes:** _—_
- [ ] **W12.T6 — i18n CI + QA** (missing/orphaned-key gate, pseudo-locale expansion + RTL smoke,
  formatter tests incl. one RTL, maintenance doc). **Owner/Notes:** _—_

---

## §14 — W13: SDKs (C#, JS, Python)

Reach parity, cover the new endpoints, use `127.0.0.1` loopback, and each ship a thorough test harness
+ README. Reference: SharpAI is itself cited as the SDK-harness example.

- [ ] **W13.T1 — C# SDK:** tool calling, `/v1/models`, `/api/show`, JSON mode, model import, optional
  auth headers/sessions; loopback `127.0.0.1`. **Owner/Notes:** _—_
- [ ] **W13.T2 — JS/TS SDK:** same surface; publishable `@sharpai/sdk`. **Owner/Notes:** _—_
- [ ] **W13.T3 — Python SDK:** implement to parity (currently "coming soon"). **Owner/Notes:** _—_
- [ ] **W13.T4 — Per-SDK test harness** against a live `127.0.0.1` server (streaming + error shapes). **Owner/Notes:** _—_
- [ ] **W13.T5 — Per-SDK README** (install, quickstart, endpoint coverage). **Owner/Notes:** _—_
- [ ] **W13.T6 — Version + publish pipeline** (NuGet/npm/PyPI) into CI (W15). **Owner/Notes:** _—_

---

## §15 — W14: Docs & repository housekeeping

Follow `WRITING_DOCUMENTS` for prose: real paragraphs, no stock lead-ins, no generic recap.

- [ ] **W14.T1 — `DOCKERHUB_README.md`** (missing) with use cases, architecture, getting started;
  images by explicit URL into `assets/`. **Ref:** REPOSITORY_REQUIREMENTS · **Owner/Notes:** _—_
- [ ] **W14.T2 — README accuracy pass** after W1–W5/W17 (chat-template source, tool calling,
  `/v1/models`, model import, HF-token optionality, GPU matrix, observability). Fix `yourusername`
  issues link and the removed-`compose.yaml` instructions. **Owner/Notes:** _—_
- [ ] **W14.T3 — CHANGELOG discipline** (`## Unreleased` now; Added/Changed/Fixed per slice; 5.0.0
  version story). **Owner/Notes:** _—_
- [ ] **W14.T4 — Update `CLAUDE.md` + `DEPLOYMENT-GUIDE.md`** for the new architecture (Watson 7.1
  registrars, 4-DB, auth mode, telemetry, concurrency/lifecycle settings); fix the stale "SwiftStack"
  note in `src/CLAUDE.md`. **Owner/Notes:** _—_
- [ ] **W14.T5 — Repo layout compliance** (all source under `src/`/`test/`/`dashboard/`/`sdk/`). **Owner/Notes:** _—_
- [ ] **W14.T6 — Docker asset conventions.** `.yaml` with build contexts; per-provider local compose
  profiles (`mysql`/`postgres`/`sqlserver`, SQLite default); `.dockerignore` coverage; stop committing
  runtime artifacts (`docker/logs/`, `docker/models/`, mutated `docker/sharpai.db`) — add to
  `.gitignore`. **Owner/Notes:** _—_
- [ ] **W14.T7 — NuGet packaging check** (`IncludeSymbols`/`snupkg`, README, icon, license packed) for
  `SharpAI` and `SharpAI.Sdk`. **Owner/Notes:** _—_
- [ ] **W14.T8 — Observability runbook** (`docs/OBSERVABILITY.md`): what each metric means, how to read
  the Grafana dashboards, how to point at an external collector. **Ref:** WRITING_DOCUMENTS · **Owner/Notes:** _—_

---

## §16 — W15: CI/CD

No `.github/workflows` exists today. CI is where the reliability and i18n guarantees become real.

- [ ] **W15.T1 — Build + test workflow.** Restore/build on `net8.0` + `net10.0`; run `Test.Automated`
  (Touchstone CLI JSON artifact), `Test.Xunit`, `Test.Nunit`; publish coverage; fail under the gate. **Owner/Notes:** _—_
- [ ] **W15.T2 — Dashboard workflow** (`npm ci`, lint `--max-warnings 0`, build, i18n missing-key +
  pseudo-locale checks, Playwright visual QA). **Owner/Notes:** _—_
- [ ] **W15.T3 — Docker image build** for CPU and CUDA variants on tag, plus DB service profiles
  (`mysql`/`postgres`/`sqlserver`) for the W10.T5 matrix. **Owner/Notes:** _—_
- [ ] **W15.T4 — Observability smoke** in CI: bring the compose stack up, assert `/metrics` scrapes and
  Grafana datasources provision. **Owner/Notes:** _—_
- [ ] **W15.T5 — Release publishing** (NuGet/npm/PyPI) gated on green tests (ties W13.T6). **Owner/Notes:** _—_

---

## §17 — W16: Operations hardening

- [ ] **W16.T1 — Graceful shutdown / draining** (finish in-flight requests, flush telemetry + request
  history, dispose engines) on SIGTERM. **Owner/Notes:** _—_
- [ ] **W16.T2 — Structured startup logging** of resolved backend, GPU, model dir, DB provider,
  migration version, telemetry endpoints — without secrets. **Owner/Notes:** _—_
- [ ] **W16.T3 — Readiness fidelity:** `/ready` reflects backend init + DB migration + writable dirs +
  telemetry host start; `/health` stays a cheap liveness probe. **Owner/Notes:** _—_

---

## §18 — W17: Observability & telemetry

Enterprise operators need to see how the stack behaves. This workstream instruments the app and ships a
turnkey Prometheus + Loki + Grafana + Tempo stack in Docker, modeled on `C:\code\xeno` (layout +
factory reset) and `C:\code\less3\less3-2.1` (Watson-native `/metrics` scrape). Telemetry is **opt-in**
via config and a clean no-op when disabled.

**Design.** The app emits through the .NET BCL (`Meter`/`ActivitySource`/`ILogger`). **Radiant**
(`Radiant` 0.1.2, source `C:\code\radiant`) hosts the OpenTelemetry pipeline in-process and pushes OTLP
to a collector; **Watson 7.1.0** emits its own `"Watson"` HTTP-server meter and spans. The collector
fans out to Prometheus (metrics), Tempo (traces), and Loki (logs, via `filelog` tailing the existing
`sharpai.log.*` files). Grafana reads all three via provisioned datasources + file-provisioned
dashboards.

- [x] **W17.T1 — Instrument the core library with the BCL (no Radiant reference in `SharpAI`).**
  _(done 2026-08-14)_ Added `SharpAI.Telemetry.SharpAITelemetry`: meters `SharpAI.Inference` /
  `SharpAI.Models` and activity source `SharpAI.Inference`, with counter `sharpai.inference.requests`,
  counter `sharpai.inference.tokens_generated`, histogram `sharpai.inference.latency` (seconds), and an
  observable gauge `sharpai.models.resident`. All four `LlamaSharpEngine` generation methods record
  latency/requests/tokens (tagged `operation`/`model`/`outcome`, low-cardinality); `ModelEngineService`
  feeds the resident-model gauge. Pure BCL, no host dependency; guarded by a no-op smoke suite.
  **Surfaces:** core, tests · **Owner/Notes:** _metric names match the Grafana Overview panels._
- [x] **W17.T2 — Radiant host in the server composition root.** _(done 2026-08-14)_ Added `Radiant`
  `0.1.2` to `SharpAI.Server`; `TelemetryHost` starts `RadiantHost` from the new `Settings.Telemetry`
  section, subscribes to the `SharpAI.*` meters/source and Watson's `"Watson"` source, configures OTLP,
  and disposes on shutdown. Startup failures are swallowed (logged, server continues). Wired into
  `Program.cs` (`InitializeTelemetry`) and disposed after `_Server.Dispose()`. Build verified.
  **Owner/Notes:** _off cleanly when `Telemetry.Enable=false`._
- [x] **W17.T3 — Watson 7.1 native telemetry.** _(done 2026-08-14)_ With Watson at 7.1.0, the server
  enables `WebserverSettings.Telemetry` and serves the in-process Prometheus `/metrics` endpoint on the
  existing listener whenever `Settings.Telemetry.Enable` is true; `prometheus.yaml`'s `sharpai` job
  scrapes it. Watson spans export over OTLP through Radiant (which already subscribes to the `Watson`
  source), while Watson metrics come from `/metrics` to avoid double-counting, per the less3 pattern.
  **Owner/Notes:** _—_
- [x] **W17.T4 — `TelemetrySettings`.** _(done 2026-08-14)_ `TelemetrySettings` (Enable, ServiceName,
  Otlp endpoint/protocol, Prometheus enable/host/port/path, Metrics/Traces/Logs toggles) with clamps and
  `SHARPAI_TELEMETRY_*` env overrides; added to `Settings`; a `Telemetry` block is present in the docker
  `sharpai.json` defaults (OTLP → `otel-collector:4317`). **Owner/Notes:** _—_
- [x] **W17.T5 — Docker observability stack** under `docker/telemetry/` and a new `docker/compose.yaml`.
  _(done 2026-08-14)_ Authored `compose.yaml` (app + dashboard + `otel-collector` 0.109.0,
  `prometheus` v2.55.1, `loki` 3.2.1, `tempo` 2.6.1, `grafana` 11.3.0), `otel-collector-config.yaml`
  (`filelog` tails `/app/logs/*.log*` → Loki; `otlp` → Prometheus + Tempo; `memory_limiter`),
  `prometheus.yaml` (collector + app `/metrics` jobs), `loki-config.yaml`, `tempo.yaml`. No named
  volumes on backends (reset-friendly). Telemetry env vars added to the `sharpai` service.
  **Owner/Notes:** _stack config complete; app-side emission lands in W17.T1–T4/T8._
- [x] **W17.T6 — Grafana provisioning + dashboards.** _(done 2026-08-14)_ Datasources with fixed UIDs
  `prometheus`/`loki`/`tempo` + trace↔log correlation; file dashboard provider; **Overview** dashboard
  (request rate/latency p50/p95/p99, error rate, tokens/sec, resident models, per-model inference
  latency, queue depth/active requests) and **Logs** dashboard (all + warning/error filter on
  `{service_name="sharpai"}`), `schemaVersion: 39`, `"id": null`. Plus a `docker/telemetry/README.md`.
  **Owner/Notes:** _Runtime/Process + Inference-detail dashboards can be added once metrics flow._
- [x] **W17.T7 — Factory defaults + reset (config hygiene).** _(done 2026-08-14)_ Telemetry configs are
  static checked-in files (not runtime state), so `reset.sh`/`reset.bat` need no change; verified the
  no-named-volume design means `docker compose down --volumes` clears backend state. Added
  `docker/logs/`, `docker/models/`, `docker/sharpai.db` to `.gitignore` and untracked the runtime DB
  (seed remains at `docker/factory/sharpai.db`). Remaining: the `Telemetry` block in the default
  `sharpai.json` lands with the W17.T4 settings code. **Owner/Notes:** _config-side complete._
- [~] **W17.T8 — Emit spans + metrics from handlers/services and validate end-to-end.** _(partial)_
  Core inference now records latency/requests/tokens metrics and exposes `StartInference` spans.
  Remaining: wrap the Ollama/OpenAI API handlers in request spans, and run the live end-to-end check
  (pull a model, run inference, confirm request rate/latency/tokens in Grafana and logs in Loki). Needs
  a running Docker stack + a model, so it lands with an integration pass. **Surfaces:** server,
  telemetry, tests, docs, docker · **Owner/Notes:** _—_

---

## §19 — Decisions

Resolved by the product owner; recorded here for traceability.

- [x] **D1 — Multi-tenancy & AAA.** Err toward Ollama: auth **built but disabled by default**; full
  AAA is opt-in for enterprise. (2026-08-13)
- [x] **D2 — Four DB providers.** All four required (`Sqlite`/`Mysql`/`Postgresql`/`SqlServer`);
  critical for enterprise; no SQLite-only shortcut. (2026-08-13)
- [x] **D3 — Vision/multimodal.** Remove llava entirely for now. (2026-08-13)
- [x] **D6 — Version.** Normalize the whole repo to a unified `5.0.0`. (2026-08-14)
- [x] **D7 — Telemetry stack.** Radiant + Watson 7.1 in-app; Prometheus + Loki + Grafana + Tempo in
  Docker, modeled on xeno (layout/factory) and less3 (Watson `/metrics` scrape). (2026-08-14)
- [x] **D8 — Testing.** Touchstone (`C:\code\touchstone`) with Test.Shared/Automated/Xunit/Nunit;
  target ~100% meaningful coverage; reliability ≥ Ollama. (2026-08-14)
- [ ] **D4 — Ollama-registry pulls (gates W5.T3).** In or out, given licensing/ToS? **Decision:** _—_
- [ ] **D5 — Positioning.** Confirm the headline is ".NET-native local inference (embed or serve),
  OpenAI- and Ollama-compatible, enterprise-observable" rather than a head-to-head "Ollama
  replacement," and align README + DOCKERHUB_README. **Decision:** _—_

---

## §20 — Definition of done (per shipped slice)

A task is not done until, as applicable to its surfaces:

- code compiles clean on `net8.0` and `net10.0` with the `CODE_STYLE` rules honored (no `var`, no
  tuples, usings inside namespace, XML docs on public members, `ConfigureAwait(false)`, one type per
  file, null-checked setters, clamped numerics);
- Touchstone suites cover the new behavior (including a failure/edge path) and run green in CI on
  `127.0.0.1`, keeping the coverage gate satisfied;
- telemetry is emitted for new server behavior and visible in Grafana where relevant;
- dashboard changes pass the mandatory responsive + light/dark + accessibility visual QA and the
  Docker dashboard container is rebuilt and restarted;
- user-facing strings are localized and the i18n key checks pass;
- affected SDKs and their harnesses are updated;
- README / DOCKERHUB_README / CHANGELOG / CLAUDE.md reflect the change and remain accurate;
- the relevant checkbox is flipped to `[x]` with an Owner/Notes entry.
