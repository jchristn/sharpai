namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using SharpAI.Prompts;

    using Touchstone.Core;

    /// <summary>
    /// Touchstone suite for <see cref="ChatTemplateResolver"/>. Verifies the decision logic — prefer the
    /// model's embedded template, fall back to the family template on absence, empty render, or error —
    /// using a fake <see cref="IChatTemplateSource"/> so no native model is required.
    /// </summary>
    public static class ChatTemplateResolverSuite
    {
        #region Public-Methods

        /// <summary>
        /// Build the resolver suite.
        /// </summary>
        /// <returns>Resolver suite.</returns>
        public static TestSuiteDescriptor Build()
        {
            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>
            {
                new TestCaseDescriptor("Resolver", "Embedded_Preferred", "Embedded template is used when available",
                    ct =>
                    {
                        ChatTemplateResult result = ChatTemplateResolver.Resolve(
                            new FakeTemplateSource(true, "RENDERED-BY-MODEL", false), "qwen", Sample());
                        TestAssert.True(result.UsedEmbeddedTemplate, "should use embedded template");
                        TestAssert.Equal("RENDERED-BY-MODEL", result.Prompt);
                        TestAssert.Equal(0, result.StopSequences.Length);
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Resolver", "Fallback_NoSupport", "Falls back to family template when unsupported",
                    ct =>
                    {
                        ChatTemplateResult result = ChatTemplateResolver.Resolve(
                            new FakeTemplateSource(false, "unused", false), "qwen", Sample());
                        TestAssert.True(!result.UsedEmbeddedTemplate, "should fall back");
                        TestAssert.Contains(result.Prompt, "<|im_start|>");
                        TestAssert.True(result.StopSequences.Length > 0, "family fallback should carry stop sequences");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Resolver", "Fallback_NullSource", "Falls back when source is null",
                    ct =>
                    {
                        ChatTemplateResult result = ChatTemplateResolver.Resolve(null, "gemma", Sample());
                        TestAssert.True(!result.UsedEmbeddedTemplate, "should fall back");
                        TestAssert.Contains(result.Prompt, "<start_of_turn>");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Resolver", "Fallback_OnThrow", "Falls back when embedded render throws",
                    ct =>
                    {
                        ChatTemplateResult result = ChatTemplateResolver.Resolve(
                            new FakeTemplateSource(true, null, true), "llama3", Sample());
                        TestAssert.True(!result.UsedEmbeddedTemplate, "should fall back on error");
                        TestAssert.Contains(result.Prompt, "<|begin_of_text|>");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Resolver", "Fallback_OnEmpty", "Falls back when embedded render is empty",
                    ct =>
                    {
                        ChatTemplateResult result = ChatTemplateResolver.Resolve(
                            new FakeTemplateSource(true, "", false), "qwen", Sample());
                        TestAssert.True(!result.UsedEmbeddedTemplate, "empty embedded render should fall back");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Resolver", "Null_Messages_Throws", "Null messages throws ArgumentNullException",
                    ct =>
                    {
                        bool threw = false;
                        try { ChatTemplateResolver.Resolve(null, "qwen", null); }
                        catch (ArgumentNullException) { threw = true; }
                        TestAssert.True(threw, "should throw ArgumentNullException for null messages");
                        return Task.CompletedTask;
                    })
            };

            return new TestSuiteDescriptor("Resolver", "Chat template resolver", cases);
        }

        #endregion

        #region Private-Methods

        private static List<ChatMessage> Sample()
        {
            return new List<ChatMessage>
            {
                new ChatMessage { Role = "user", Content = "Hello" }
            };
        }

        #endregion

        private sealed class FakeTemplateSource : IChatTemplateSource
        {
            private readonly bool _Supports;
            private readonly string? _Rendered;
            private readonly bool _Throw;

            public FakeTemplateSource(bool supports, string? rendered, bool throwOnRender)
            {
                _Supports = supports;
                _Rendered = rendered;
                _Throw = throwOnRender;
            }

            public bool SupportsEmbeddedChatTemplate
            {
                get { return _Supports; }
            }

            public string RenderEmbeddedChatPrompt(IReadOnlyList<ChatMessage> messages, bool addGenerationPrompt)
            {
                if (_Throw) throw new InvalidOperationException("render failure");
                return _Rendered ?? string.Empty;
            }
        }
    }
}
