namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using SharpAI.Helpers;

    using Touchstone.Core;

    /// <summary>
    /// Touchstone suite for <see cref="ThinkingFilter"/>, covering both the whole-response path and the
    /// streaming token path (including partial-tag buffering, which must never leak a partial marker).
    /// </summary>
    public static class ThinkingFilterSuite
    {
        #region Public-Methods

        /// <summary>
        /// Build the thinking-filter suite.
        /// </summary>
        /// <returns>Thinking-filter suite.</returns>
        public static TestSuiteDescriptor Build()
        {
            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>
            {
                new TestCaseDescriptor("Thinking", "Remove_Single", "Removes a single thinking block",
                    ct =>
                    {
                        TestAssert.Equal("Visible answer",
                            ThinkingFilter.RemoveThinkingBlocks("<think>private reasoning</think>\nVisible answer"));
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Thinking", "Remove_Multiple", "Removes multiple thinking blocks",
                    ct =>
                    {
                        string filtered = ThinkingFilter.RemoveThinkingBlocks("<think>a</think>One <think>b</think>Two");
                        TestAssert.DoesNotContain(filtered, "<think>");
                        TestAssert.Contains(filtered, "One");
                        TestAssert.Contains(filtered, "Two");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Thinking", "Remove_Null", "Null passes through unchanged",
                    ct => { TestAssert.Equal(null, ThinkingFilter.RemoveThinkingBlocks(null)); return Task.CompletedTask; }),

                new TestCaseDescriptor("Thinking", "Remove_NoTag", "Text without a tag is unchanged",
                    ct => { TestAssert.Equal("plain text", ThinkingFilter.RemoveThinkingBlocks("plain text")); return Task.CompletedTask; }),

                new TestCaseDescriptor("Thinking", "Stream_Filters", "Streaming filter suppresses the thinking block and emits the rest",
                    ct =>
                    {
                        ThinkingFilter filter = new ThinkingFilter();
                        string[] tokens = new string[] { "<think>", "hidden ", "reasoning", "</think>", "Visible ", "answer" };
                        System.Text.StringBuilder outp = new System.Text.StringBuilder();
                        foreach (string t in tokens) outp.Append(filter.ProcessToken(t));
                        outp.Append(filter.Flush());
                        string result = outp.ToString();
                        TestAssert.DoesNotContain(result, "hidden");
                        TestAssert.DoesNotContain(result, "<think>");
                        TestAssert.Contains(result, "Visible");
                        TestAssert.Contains(result, "answer");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Thinking", "Stream_NoLeakPartial", "Streaming filter never leaks a partial open tag",
                    ct =>
                    {
                        ThinkingFilter filter = new ThinkingFilter();
                        System.Text.StringBuilder outp = new System.Text.StringBuilder();
                        // Feed the open tag one character at a time; nothing should be emitted mid-tag.
                        foreach (char c in "<think>secret</think>done")
                            outp.Append(filter.ProcessToken(c.ToString()));
                        outp.Append(filter.Flush());
                        string result = outp.ToString();
                        TestAssert.DoesNotContain(result, "<");
                        TestAssert.DoesNotContain(result, "secret");
                        TestAssert.Contains(result, "done");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Thinking", "Stream_PlainPassthrough", "Streaming filter passes plain text through",
                    ct =>
                    {
                        ThinkingFilter filter = new ThinkingFilter();
                        System.Text.StringBuilder outp = new System.Text.StringBuilder();
                        outp.Append(filter.ProcessToken("hello "));
                        outp.Append(filter.ProcessToken("world"));
                        outp.Append(filter.Flush());
                        TestAssert.Contains(outp.ToString(), "hello world");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Thinking", "Custom_Tags_Static", "Custom markers are removed by the static overload",
                    ct =>
                    {
                        string filtered = ThinkingFilter.RemoveThinkingBlocks("<reason>hidden</reason>Answer", "<reason>", "</reason>");
                        TestAssert.Equal("Answer", filtered);
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Thinking", "Custom_Tags_Stream", "Custom markers are filtered while streaming",
                    ct =>
                    {
                        ThinkingFilter filter = new ThinkingFilter("<reason>", "</reason>");
                        System.Text.StringBuilder outp = new System.Text.StringBuilder();
                        foreach (string t in new string[] { "<reason>", "why", "</reason>", "Visible" })
                            outp.Append(filter.ProcessToken(t));
                        outp.Append(filter.Flush());
                        string result = outp.ToString();
                        TestAssert.DoesNotContain(result, "why");
                        TestAssert.Contains(result, "Visible");
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Thinking", "Unclosed_Flush_Empty", "An unclosed thinking block is not leaked on flush",
                    ct =>
                    {
                        ThinkingFilter filter = new ThinkingFilter();
                        filter.ProcessToken("<think>still thinking");
                        string flushed = filter.Flush();
                        TestAssert.Equal("", flushed);
                        return Task.CompletedTask;
                    }),

                new TestCaseDescriptor("Thinking", "Null_Tag_Throws", "Null marker throws ArgumentNullException",
                    ct =>
                    {
                        bool threw = false;
                        try { ThinkingFilter unused = new ThinkingFilter(null!, "</think>"); }
                        catch (ArgumentNullException) { threw = true; }
                        TestAssert.True(threw, "null open tag should throw");
                        return Task.CompletedTask;
                    })
            };

            return new TestSuiteDescriptor("Thinking", "Thinking filter", cases);
        }

        #endregion
    }
}
