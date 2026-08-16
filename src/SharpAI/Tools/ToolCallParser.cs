namespace SharpAI.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Extracts tool/function calls from a model's raw text output. Handles the common formats emitted by
    /// tool-capable GGUF models: one or more <c>&lt;tool_call&gt;{...}&lt;/tool_call&gt;</c> blocks
    /// (Qwen/Hermes style), a bare JSON object, or a JSON array of calls. Each call may use either
    /// <c>{"name","arguments"}</c> or a nested <c>{"function":{"name",...}}</c> shape. Output that
    /// contains no recognizable tool call yields an empty list, so ordinary text responses are unaffected.
    /// </summary>
    public static class ToolCallParser
    {
        #region Private-Members

        private static readonly Regex _ToolCallBlock = new Regex(
            @"<tool_call>\s*([\s\S]*?)\s*</tool_call>",
            RegexOptions.Compiled);

        #endregion

        #region Public-Methods

        /// <summary>
        /// Parse tool calls from model output.
        /// </summary>
        /// <param name="modelOutput">The raw text produced by the model.</param>
        /// <returns>The tool calls found, in order. Empty when the output is not a tool call.</returns>
        public static List<ParsedToolCall> Parse(string modelOutput)
        {
            List<ParsedToolCall> results = new List<ParsedToolCall>();
            if (String.IsNullOrWhiteSpace(modelOutput)) return results;

            MatchCollection matches = _ToolCallBlock.Matches(modelOutput);
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    ParsedToolCall call = TryParseObject(match.Groups[1].Value);
                    if (call != null) results.Add(call);
                }
                return results;
            }

            string trimmed = modelOutput.Trim();
            if (trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                TryParseArray(trimmed, results);
                return results;
            }

            if (trimmed.StartsWith("{", StringComparison.Ordinal))
            {
                ParsedToolCall call = TryParseObject(trimmed);
                if (call != null) results.Add(call);
            }

            return results;
        }

        #endregion

        #region Private-Methods

        private static void TryParseArray(string json, List<ParsedToolCall> results)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement element in doc.RootElement.EnumerateArray())
                        {
                            ParsedToolCall call = FromElement(element);
                            if (call != null) results.Add(call);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Not valid JSON; treat as no tool calls.
            }
        }

        private static ParsedToolCall TryParseObject(string json)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    return FromElement(doc.RootElement);
                }
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static ParsedToolCall FromElement(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object) return null;

            string name = null;

            if (element.TryGetProperty("name", out JsonElement nameElement) && nameElement.ValueKind == JsonValueKind.String)
            {
                name = nameElement.GetString();
            }
            else if (element.TryGetProperty("function", out JsonElement functionElement)
                && functionElement.ValueKind == JsonValueKind.Object
                && functionElement.TryGetProperty("name", out JsonElement functionName)
                && functionName.ValueKind == JsonValueKind.String)
            {
                name = functionName.GetString();

                if (functionElement.TryGetProperty("arguments", out JsonElement functionArgs))
                {
                    return new ParsedToolCall
                    {
                        Name = name,
                        ArgumentsJson = functionArgs.ValueKind == JsonValueKind.String ? functionArgs.GetString() : functionArgs.GetRawText()
                    };
                }
            }

            if (String.IsNullOrEmpty(name)) return null;

            string argumentsJson = "{}";
            if (element.TryGetProperty("arguments", out JsonElement argsElement))
            {
                argumentsJson = argsElement.ValueKind == JsonValueKind.String ? argsElement.GetString() : argsElement.GetRawText();
            }
            else if (element.TryGetProperty("parameters", out JsonElement parametersElement))
            {
                argumentsJson = parametersElement.GetRawText();
            }

            return new ParsedToolCall { Name = name, ArgumentsJson = argumentsJson };
        }

        #endregion
    }
}
