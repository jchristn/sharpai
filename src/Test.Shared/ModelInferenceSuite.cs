namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using SharpAI.Engines;
    using SharpAI.Prompts;

    using Touchstone.Core;

    /// <summary>
    /// Model-dependent (integration) suite that exercises real inference against a GGUF located by
    /// <see cref="ModelFixture"/>. When no model is available every case is skipped, so the suite is safe
    /// to run in CI without a model. When a model is present it verifies engine initialization, embedded
    /// chat-template rendering (W1), small-<c>max_tokens</c> generation (W1.T3), concurrent generation
    /// (W2), and embeddings — the runtime half of the reliability coverage.
    /// </summary>
    public static class ModelInferenceSuite
    {
        #region Private-Members

        private static LlamaSharpEngine? _Engine = null;

        #endregion

        #region Public-Methods

        /// <summary>
        /// Build the model inference suite.
        /// </summary>
        /// <returns>Model inference suite.</returns>
        public static TestSuiteDescriptor Build()
        {
            bool skip = !ModelFixture.IsAvailable;
            string reason = ModelFixture.SkipReason;

            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>
            {
                new TestCaseDescriptor("ModelInference", "Init", "Engine initializes from the fixture model",
                    ExecuteInit, Array.Empty<string>(), skip, reason),

                new TestCaseDescriptor("ModelInference", "ChatTemplate", "Chat prompt renders (embedded or fallback)",
                    ExecuteChatTemplate, Array.Empty<string>(), skip, reason),

                new TestCaseDescriptor("ModelInference", "MaxTokens", "Small max_tokens produces a short completion",
                    ExecuteMaxTokens, Array.Empty<string>(), skip, reason),

                new TestCaseDescriptor("ModelInference", "Concurrent", "Two concurrent generations both complete",
                    ExecuteConcurrent, Array.Empty<string>(), skip, reason),

                new TestCaseDescriptor("ModelInference", "Embeddings", "Embeddings produce a non-empty vector when supported",
                    ExecuteEmbeddings, Array.Empty<string>(), skip, reason)
            };

            return new TestSuiteDescriptor(
                "ModelInference",
                "Model inference (fixture-gated)",
                cases,
                BeforeSuiteAsync,
                AfterSuiteAsync);
        }

        #endregion

        #region Private-Methods

        private static async ValueTask BeforeSuiteAsync(CancellationToken token)
        {
            if (!ModelFixture.IsAvailable) return;

            _Engine = new LlamaSharpEngine();
            await _Engine.InitializeAsync(ModelFixture.Path!).ConfigureAwait(false);
        }

        private static ValueTask AfterSuiteAsync(CancellationToken token)
        {
            _Engine?.Dispose();
            _Engine = null;
            return new ValueTask();
        }

        private static Task ExecuteInit(CancellationToken token)
        {
            TestAssert.True(_Engine != null && _Engine.IsInitialized, "engine should be initialized");
            return Task.CompletedTask;
        }

        private static Task ExecuteChatTemplate(CancellationToken token)
        {
            LlamaSharpEngine engine = _Engine!;
            List<ChatMessage> messages = new List<ChatMessage>
            {
                new ChatMessage { Role = "user", Content = "Say hello." }
            };

            ChatTemplateResult result = ChatTemplateResolver.Resolve(engine, engine.Architecture, messages);
            TestAssert.True(!string.IsNullOrEmpty(result.Prompt), "rendered chat prompt should not be empty");
            return Task.CompletedTask;
        }

        private static async Task ExecuteMaxTokens(CancellationToken token)
        {
            LlamaSharpEngine engine = _Engine!;
            string output = await engine.GenerateTextAsync("Count: 1 2 3", 8, 0.2f, null, token).ConfigureAwait(false);
            TestAssert.True(output != null, "generation should return a (possibly empty) string");
            // A max of 8 tokens cannot decode into a very long string; guards against the old clamp-to-100 bug.
            TestAssert.True(output!.Length < 400, "small max_tokens should produce a short completion, got length " + output.Length);
        }

        private static async Task ExecuteConcurrent(CancellationToken token)
        {
            LlamaSharpEngine engine = _Engine!;
            Task<string> a = engine.GenerateTextAsync("A:", 8, 0.2f, null, token);
            Task<string> b = engine.GenerateTextAsync("B:", 8, 0.2f, null, token);
            string[] results = await Task.WhenAll(a, b).ConfigureAwait(false);
            TestAssert.True(results[0] != null && results[1] != null, "both concurrent generations should complete");
        }

        private static async Task ExecuteEmbeddings(CancellationToken token)
        {
            LlamaSharpEngine engine = _Engine!;
            if (!engine.SupportsEmbeddings) return; // model does not produce embeddings; nothing to assert

            float[] vector = await engine.GenerateEmbeddingsAsync("hello world", token).ConfigureAwait(false);
            TestAssert.True(vector != null && vector.Length > 0, "embedding vector should be non-empty");
        }

        #endregion
    }
}
