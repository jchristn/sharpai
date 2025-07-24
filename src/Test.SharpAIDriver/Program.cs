namespace Test.SharpAIDriver
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using GetSomeInput;
    using SharpAI;
    using SharpAI.Engines;
    using SharpAI.Models;
    using SyslogLogging;

    public static class Program
    {
        static bool _RunForever = true;
        static bool _Debug = false;
        static AIDriver _AIDriver = null;
        static LoggingModule _Logging = null;
        static string _DatabaseFilename = "./sharpai.db";
        static string _HuggingFaceApiKey = null;
        static string _ModelDirectory = "./models/";
        static string _CurrentModel = null;

        public static async Task Main(string[] args)
        {
            InitializeDriver();

            while (_RunForever)
            {
                string userInput = Inputty.GetString("Command [? for help]:", null, false);

                if (userInput.Equals("?")) Menu();
                else if (userInput.Equals("q")) _RunForever = false;
                else if (userInput.Equals("cls")) Console.Clear();
                else if (userInput.Equals("debug")) ToggleDebug();
                else if (userInput.Equals("model")) SetCurrentModel();
                else
                {
                    string[] parts = userInput.Split(new char[] { ' ' }, 2);

                    if (parts.Length >= 1)
                    {
                        if (parts[0].Equals("models"))
                        {
                            if (parts.Length == 2)
                            {
                                if (parts[1].Equals("add")) await AddModel();
                                else if (parts[1].Equals("list")) ListModels();
                                else if (parts[1].Equals("delete")) DeleteModel();
                                else if (parts[1].Equals("info")) ModelInfo();
                            }
                            else
                            {
                                Console.WriteLine("Please specify a models command: add, list, delete, info");
                            }
                        }
                        else if (parts[0].Equals("embeddings"))
                        {
                            if (parts.Length == 2)
                            {
                                if (parts[1].Equals("generate")) await GenerateEmbeddings();
                                else if (parts[1].Equals("batch")) await GenerateEmbeddingsBatch();
                            }
                            else
                            {
                                Console.WriteLine("Please specify an embeddings command: generate, batch");
                            }
                        }
                        else if (parts[0].Equals("completion"))
                        {
                            if (parts.Length == 2)
                            {
                                if (parts[1].Equals("generate")) await GenerateCompletion();
                                else if (parts[1].Equals("stream")) await GenerateCompletionStream();
                            }
                            else
                            {
                                Console.WriteLine("Please specify a completion command: generate, stream");
                            }
                        }
                        else if (parts[0].Equals("chat"))
                        {
                            if (parts.Length == 2)
                            {
                                if (parts[1].Equals("generate")) await GenerateChatCompletion();
                                else if (parts[1].Equals("stream")) await GenerateChatCompletionStream();
                            }
                            else
                            {
                                Console.WriteLine("Please specify a chat command: generate, stream, interactive");
                            }
                        }
                        else if (parts[0].Equals("test"))
                        {
                            if (parts.Length == 2)
                            {
                                if (parts[1].Equals("all")) await TestAll();
                                else if (parts[1].Equals("embeddings")) await TestEmbeddings();
                                else if (parts[1].Equals("completion")) await TestCompletion();
                                else if (parts[1].Equals("chat")) await TestChat();
                            }
                            else
                            {
                                Console.WriteLine("Please specify a test command: all, embeddings, completion, chat");
                            }
                        }
                    }
                }
            }
        }

        static void InitializeDriver()
        {
            Console.WriteLine("");
            Console.WriteLine("SharpAI Driver Test Program");
            Console.WriteLine("===========================");
            Console.WriteLine("");

            _DatabaseFilename = Inputty.GetString("Database filename:", _DatabaseFilename, false);
            _HuggingFaceApiKey = Inputty.GetString("HuggingFace API key:", _HuggingFaceApiKey, false);
            _ModelDirectory = Inputty.GetString("Model directory:", _ModelDirectory, false);
            _Debug = Inputty.GetBoolean("Enable debug logging:", _Debug);

            _Logging = new LoggingModule();
            _Logging.Settings.EnableConsole = true;

            Console.WriteLine("");
            Console.WriteLine("Initializing AI Driver...");

            try
            {
                _AIDriver = new AIDriver(
                    _Logging,
                    _DatabaseFilename,
                    _HuggingFaceApiKey,
                    _ModelDirectory);

                Console.WriteLine("AI Driver initialized successfully!");
                Console.WriteLine("");

                // Check for existing models
                var models = _AIDriver.Models.All();
                if (models.Count > 0)
                {
                    Console.WriteLine($"Found {models.Count} existing model(s):");
                    foreach (var model in models)
                    {
                        Console.WriteLine($"  - {model.Name} ({model.ContentLength / 1024 / 1024} MB)");
                    }
                    Console.WriteLine("");

                    if (models.Count == 1)
                    {
                        _CurrentModel = models.First().Name;
                        Console.WriteLine($"Auto-selected model: {_CurrentModel}");
                    }
                    else
                    {
                        Console.WriteLine("Use 'model' command to select a model");
                    }
                }
                else
                {
                    Console.WriteLine("No models found. Use 'models add' to download a model.");
                    Console.WriteLine();
                    Console.WriteLine("Two recommended models:");
                    Console.WriteLine("- For embeddings  : leliuga/all-MiniLM-L6-v2-GGUF");
                    Console.WriteLine("- For completions : QuantFactory/Qwen2.5-3B-GGUF");
                    Console.WriteLine();
                }
                Console.WriteLine("");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing AI Driver:{Environment.NewLine}{ex.ToString()}");
                Environment.Exit(1);
            }
        }

        static void Menu()
        {
            Console.WriteLine("");
            Console.WriteLine("Available commands:");
            Console.WriteLine("  ?               help, this menu");
            Console.WriteLine("  q               quit");
            Console.WriteLine("  cls             clear the screen");
            Console.WriteLine("  debug           enable or disable debug (enabled: " + _Debug + ")");
            Console.WriteLine("  model           set the current model (currently: " + (_CurrentModel ?? "none") + ")");
            Console.WriteLine("");
            Console.WriteLine("Model Management:");
            Console.WriteLine("  models add      download and add a model from HuggingFace");
            Console.WriteLine("  models list     list all available models");
            Console.WriteLine("  models delete   delete a model");
            Console.WriteLine("  models info     show detailed information about a model");
            Console.WriteLine("");
            Console.WriteLine("Embeddings:");
            Console.WriteLine("  embeddings generate   generate embeddings for a single input");
            Console.WriteLine("  embeddings batch      generate embeddings for multiple inputs");
            Console.WriteLine("");
            Console.WriteLine("Completions:");
            Console.WriteLine("  completion generate   generate a completion (non-streaming)");
            Console.WriteLine("  completion stream     generate a completion (streaming)");
            Console.WriteLine("");
            Console.WriteLine("Chat:");
            Console.WriteLine("  chat generate    generate a chat completion (non-streaming)");
            Console.WriteLine("  chat stream      generate a chat completion (streaming)");
            Console.WriteLine("");
            Console.WriteLine("Tests:");
            Console.WriteLine("  test all         run all tests");
            Console.WriteLine("  test embeddings  test embeddings generation");
            Console.WriteLine("  test completion  test completion generation");
            Console.WriteLine("  test chat        test chat completion generation");
            Console.WriteLine("");
        }

        static void ToggleDebug()
        {
            _Debug = !_Debug;
            _Logging.Settings.EnableConsole = !_Logging.Settings.EnableConsole;
            Console.WriteLine($"Debug logging: {(_Debug ? "enabled" : "disabled")}");
        }

        static void SetCurrentModel()
        {
            var models = _AIDriver.Models.All();
            if (models.Count == 0)
            {
                Console.WriteLine("No models available. Use 'models add' to download a model.");
                return;
            }

            Console.WriteLine("Available models:");
            for (int i = 0; i < models.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {models[i].Name}");
            }

            int selection = Inputty.GetInteger("Select model number:", 1, true, true);
            if (selection > 0)
            {
                _CurrentModel = models[selection - 1].Name;
                Console.WriteLine($"Current model set to: {_CurrentModel}");
            }
        }

        #region Model-Management

        static async Task AddModel()
        {
            string modelName = Inputty.GetString("Model name (e.g., 'TheBloke/Llama-2-7B-Chat-GGUF'):", null, false);

            Console.WriteLine($"Attempting to download model '{modelName}'...");
            Console.WriteLine("This may take several minutes depending on the model size and your internet connection.");

            try
            {
                var model = await _AIDriver.Models.Add(modelName);
                Console.WriteLine($"Successfully added model: {model.Name}");
                Console.WriteLine($"  GUID: {model.GUID}");
                Console.WriteLine($"  Size: {model.ContentLength / 1024 / 1024} MB");
                Console.WriteLine($"  Quantization: {model.Quantization}");
                Console.WriteLine($"  SHA256: {model.SHA256Hash}");

                if (String.IsNullOrEmpty(_CurrentModel))
                {
                    _CurrentModel = model.Name;
                    Console.WriteLine($"Auto-selected as current model: {_CurrentModel}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding model:{Environment.NewLine}{ex.ToString()}");
            }
        }

        static void ListModels()
        {
            var models = _AIDriver.Models.All();
            if (models.Count == 0)
            {
                Console.WriteLine("No models found.");
                return;
            }

            Console.WriteLine($"Found {models.Count} model(s):");
            Console.WriteLine("");
            foreach (var model in models)
            {
                Console.WriteLine($"Name: {model.Name}");
                Console.WriteLine($"  GUID: {model.GUID}");
                Console.WriteLine($"  Size: {model.ContentLength / 1024 / 1024} MB");
                Console.WriteLine($"  Quantization: {model.Quantization}");
                Console.WriteLine($"  Created: {model.CreatedUtc}");
                if (model.Name == _CurrentModel)
                {
                    Console.WriteLine("  ** CURRENT MODEL **");
                }
                Console.WriteLine("");
            }
        }

        static void DeleteModel()
        {
            var models = _AIDriver.Models.All();
            if (models.Count == 0)
            {
                Console.WriteLine("No models available to delete.");
                return;
            }

            Console.WriteLine("Available models:");
            for (int i = 0; i < models.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {models[i].Name}");
            }

            int selection = Inputty.GetInteger("Select model number to delete:", 1, true, true);
            if (selection > 0)
            {
                var modelToDelete = models[selection - 1];
                bool confirm = Inputty.GetBoolean($"Are you sure you want to delete '{modelToDelete.Name}'?", false);

                if (confirm)
                {
                    try
                    {
                        _AIDriver.Models.Delete(modelToDelete.Name);
                        Console.WriteLine($"Successfully deleted model: {modelToDelete.Name}");

                        if (_CurrentModel == modelToDelete.Name)
                        {
                            _CurrentModel = null;
                            Console.WriteLine("Current model selection cleared.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting model:{Environment.NewLine}{ex.ToString()}");
                    }
                }
            }
        }

        static void ModelInfo()
        {
            string modelName = _CurrentModel;
            if (String.IsNullOrEmpty(modelName))
            {
                modelName = Inputty.GetString("Model name:", null, false);
            }

            try
            {
                var model = _AIDriver.Models.GetByName(modelName);
                if (model == null)
                {
                    Console.WriteLine($"Model '{modelName}' not found.");
                    return;
                }

                Console.WriteLine($"Model Information for: {model.Name}");
                Console.WriteLine($"  GUID: {model.GUID}");
                Console.WriteLine($"  Format: {model.Format}");
                Console.WriteLine($"  Family: {model.Family}");
                Console.WriteLine($"  Parent Model: {model.ParentModel ?? "N/A"}");
                Console.WriteLine($"  Parameter Size: {model.ParameterSize ?? "Unknown"}");
                Console.WriteLine($"  Quantization: {model.Quantization}");
                Console.WriteLine($"  Size: {model.ContentLength / 1024 / 1024} MB ({model.ContentLength} bytes)");
                Console.WriteLine($"  MD5: {model.MD5Hash}");
                Console.WriteLine($"  SHA1: {model.SHA1Hash}");
                Console.WriteLine($"  SHA256: {model.SHA256Hash}");
                Console.WriteLine($"  Source URL: {model.SourceUrl}");
                Console.WriteLine($"  Model Creation: {model.ModelCreationUtc?.ToString() ?? "Unknown"}");
                Console.WriteLine($"  Downloaded: {model.CreatedUtc}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving model info:{Environment.NewLine}{ex.ToString()}");
            }
        }

        #endregion

        #region Embeddings

        static async Task GenerateEmbeddings()
        {
            if (!EnsureModelSelected()) return;

            string input = Inputty.GetString("Enter text to generate embeddings for:", null, false);

            Console.WriteLine($"Generating embeddings using model '{_CurrentModel}'...");

            try
            {
                var embeddings = await _AIDriver.Embeddings.Generate(_CurrentModel, input);

                Console.WriteLine($"Generated {embeddings.Length} dimensional embeddings:");
                Console.WriteLine("");

                // Show first 10 values
                int displayCount = Math.Min(10, embeddings.Length);
                for (int i = 0; i < displayCount; i++)
                {
                    Console.WriteLine($"  [{i}]: {embeddings[i]:F6}");
                }

                if (embeddings.Length > displayCount)
                {
                    Console.WriteLine($"  ... ({embeddings.Length - displayCount} more values)");
                }

                Console.WriteLine("");
                Console.WriteLine($"Embeddings summary:");
                Console.WriteLine($"  Dimensions: {embeddings.Length}");
                Console.WriteLine($"  Min value: {embeddings.Min():F6}");
                Console.WriteLine($"  Max value: {embeddings.Max():F6}");
                Console.WriteLine($"  Mean value: {embeddings.Average():F6}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating embeddings:{Environment.NewLine}{ex.ToString()}");
            }
        }

        static async Task GenerateEmbeddingsBatch()
        {
            if (!EnsureModelSelected()) return;

            Console.WriteLine("Enter texts to generate embeddings for (empty line to finish):");
            var inputs = new List<string>();

            while (true)
            {
                string input = Inputty.GetString($"Text {inputs.Count + 1}:", null, true);
                if (String.IsNullOrEmpty(input)) break;
                inputs.Add(input);
            }

            if (inputs.Count == 0)
            {
                Console.WriteLine("No inputs provided.");
                return;
            }

            Console.WriteLine($"Generating embeddings for {inputs.Count} texts using model '{_CurrentModel}'...");

            try
            {
                var embeddingsBatch = await _AIDriver.Embeddings.Generate(_CurrentModel, inputs.ToArray());

                Console.WriteLine($"Generated embeddings for {embeddingsBatch.Length} texts:");
                Console.WriteLine("");

                for (int i = 0; i < embeddingsBatch.Length; i++)
                {
                    Console.WriteLine($"Text {i + 1}: \"{inputs[i]}\"");
                    Console.WriteLine($"  Dimensions: {embeddingsBatch[i].Length}");
                    Console.WriteLine($"  First 5 values: [{string.Join(", ", embeddingsBatch[i].Take(5).Select(v => v.ToString("F4")))}]");
                    Console.WriteLine("");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating embeddings batch:{Environment.NewLine}{ex.ToString()}");
            }
        }

        #endregion

        #region Completions

        static async Task GenerateCompletion()
        {
            if (!EnsureModelSelected()) return;

            string prompt = Inputty.GetString("Enter prompt:", null, false);
            int maxTokens = Inputty.GetInteger("Max tokens:", 512, true, false);
            float temperature = (float)Inputty.GetDecimal("Temperature (0.0-1.0):", 0.7m, true, true);

            Console.WriteLine($"Generating completion using model '{_CurrentModel}'...");
            Console.WriteLine("");

            try
            {
                var completion = await _AIDriver.Completion.GenerateCompletion(
                    _CurrentModel,
                    prompt,
                    maxTokens,
                    temperature);

                Console.WriteLine("Completion:");
                Console.WriteLine("===========");
                Console.WriteLine(completion);
                Console.WriteLine("===========");
                Console.WriteLine($"Length: {completion.Length} characters");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating completion:{Environment.NewLine}{ex.ToString()}");
            }
        }

        static async Task GenerateCompletionStream()
        {
            if (!EnsureModelSelected()) return;

            string prompt = Inputty.GetString("Enter prompt:", null, false);
            int maxTokens = Inputty.GetInteger("Max tokens:", 512, true, false);
            float temperature = (float)Inputty.GetDecimal("Temperature (0.0-1.0):", 0.7m, true, true);

            Console.WriteLine($"Generating completion using model '{_CurrentModel}'...");
            Console.WriteLine("");
            Console.WriteLine("Completion:");
            Console.WriteLine("===========");

            try
            {
                int charCount = 0;
                await foreach (var chunk in _AIDriver.Completion.GenerateCompletionStreaming(
                    _CurrentModel,
                    prompt,
                    maxTokens,
                    temperature))
                {
                    Console.Write(chunk);
                    charCount += chunk.Length;
                }

                Console.WriteLine("");
                Console.WriteLine("============");
                Console.WriteLine($"Length: {charCount} characters");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating completion stream:{Environment.NewLine}{ex.ToString()}");
            }
        }

        #endregion

        #region Chat

        static async Task GenerateChatCompletion()
        {
            if (!EnsureModelSelected()) return;

            Console.WriteLine();
            Console.WriteLine("Be sure to construct your prompt using a format understandable by the selected model.");
            string prompt = Inputty.GetString("Prompt:", null, true);
            if (String.IsNullOrEmpty(prompt)) return;

            int maxTokens = Inputty.GetInteger("Max tokens:", 512, true, false);
            float temperature = (float)Inputty.GetDecimal("Temperature (0.0-1.0):", 0.7m, true, true);

            Console.WriteLine($"Generating chat completion using model '{_CurrentModel}'...");
            Console.WriteLine("");

            try
            {
                var completion = await _AIDriver.Chat.GenerateCompletion(
                    _CurrentModel,
                    prompt,
                    maxTokens,
                    temperature);

                Console.WriteLine("Assistant Response:");
                Console.WriteLine("==================");
                Console.WriteLine(completion);
                Console.WriteLine("==================");
                Console.WriteLine($"Length: {completion.Length} characters");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating chat completion:{Environment.NewLine}{ex.ToString()}");
            }
        }

        static async Task GenerateChatCompletionStream()
        {
            if (!EnsureModelSelected()) return;

            Console.WriteLine();
            Console.WriteLine("Be sure to construct your prompt using a format understandable by the selected model.");
            string prompt = Inputty.GetString("Prompt:", null, true);
            if (String.IsNullOrEmpty(prompt)) return;

            int maxTokens = Inputty.GetInteger("Max tokens:", 512, true, false);
            float temperature = (float)Inputty.GetDecimal("Temperature (0.0-1.0):", 0.7m, true, true);

            Console.WriteLine($"Generating chat completion using model '{_CurrentModel}'...");
            Console.WriteLine("");
            Console.WriteLine("Assistant Response:");
            Console.WriteLine("==================");

            try
            {
                int charCount = 0;
                await foreach (var chunk in _AIDriver.Chat.GenerateCompletionStreaming(
                    _CurrentModel,
                    prompt,
                    maxTokens,
                    temperature))
                {
                    Console.Write(chunk);
                    charCount += chunk.Length;
                }

                Console.WriteLine("");
                Console.WriteLine("==================");
                Console.WriteLine($"Length: {charCount} characters");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating chat completion stream:{Environment.NewLine}{ex.ToString()}");
            }
        }

        #endregion

        #region Tests

        static async Task TestAll()
        {
            await TestEmbeddings();
            await TestCompletion();
            await TestChat();
        }

        static async Task TestEmbeddings()
        {
            if (!EnsureModelSelected()) return;

            Console.WriteLine("=== Testing Embeddings ===");

            try
            {
                // Test single embedding
                Console.WriteLine("Test 1: Single text embedding");
                var embedding = await _AIDriver.Embeddings.Generate(_CurrentModel, "Hello, world!");
                Console.WriteLine($"✓ Generated embedding with {embedding.Length} dimensions");

                // Test batch embeddings
                Console.WriteLine("\nTest 2: Batch embeddings");
                var batch = await _AIDriver.Embeddings.Generate(_CurrentModel, new[] { "First text", "Second text", "Third text" });
                Console.WriteLine($"✓ Generated {batch.Length} embeddings");

                // Test similarity
                Console.WriteLine("\nTest 3: Semantic similarity");
                var similar1 = await _AIDriver.Embeddings.Generate(_CurrentModel, "The cat sat on the mat");
                var similar2 = await _AIDriver.Embeddings.Generate(_CurrentModel, "A feline rested on the rug");
                var different = await _AIDriver.Embeddings.Generate(_CurrentModel, "The stock market crashed today");

                double sim1 = CosineSimilarity(similar1, similar2);
                double sim2 = CosineSimilarity(similar1, different);

                Console.WriteLine($"✓ Similarity (similar texts): {sim1:F4}");
                Console.WriteLine($"✓ Similarity (different texts): {sim2:F4}");

                Console.WriteLine("\n✓ All embedding tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Embedding test failed:{Environment.NewLine}{ex.ToString()}");
            }
        }

        static async Task TestCompletion()
        {
            if (!EnsureModelSelected()) return;

            Console.WriteLine("\n=== Testing Completions ===");

            try
            {
                // Test non-streaming
                Console.WriteLine("Test 1: Non-streaming completion");
                var completion = await _AIDriver.Completion.GenerateCompletion(
                    _CurrentModel,
                    "The capital of France is",
                    100,
                    0.1f);
                Console.WriteLine($"✓ Generated completion: {completion.Trim()}");

                // Test streaming
                Console.WriteLine("\nTest 2: Streaming completion");
                Console.Write("✓ Streaming response: ");
                await foreach (var chunk in _AIDriver.Completion.GenerateCompletionStreaming(
                    _CurrentModel,
                    "List three colors:",
                    100,
                    0.5f))
                {
                    Console.Write(".");
                }
                Console.WriteLine(" Complete!");

                Console.WriteLine("\n✓ All completion tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Completion test failed:{Environment.NewLine}{ex.ToString()}");
            }
        }

        static async Task TestChat()
        {
            if (!EnsureModelSelected()) return;

            Console.WriteLine("\n=== Testing Chat ===");

            try
            {
                string prompt = """
                    system: You are a helpful assistant. Introduce yourself as 'Steve' when you respond.
                    user: What is 2+2?
                    assistant: 
                    """;

                // Test non-streaming
                Console.WriteLine("Test 1: Non-streaming chat");
                var response = await _AIDriver.Chat.GenerateCompletion(
                    _CurrentModel,
                    prompt,
                    100,
                    0.1f);
                Console.WriteLine($"✓ Generated response: {response.Trim()}");

                // Test streaming
                Console.WriteLine("\nTest 2: Streaming chat");
                prompt = $"""
                    system: You are a helpful assistant.  Introduce yourself as 'Steve'.
                    user: What is 2+2?
                    assistant: {response}
                    user: Could you explain why?
                    assistant: 
                    """;

                Console.Write("✓ Streaming response: ");
                await foreach (var chunk in _AIDriver.Chat.GenerateCompletionStreaming(
                    _CurrentModel,
                    prompt,
                    200,
                    0.5f))
                {
                    Console.Write(".");
                }
                Console.WriteLine(" Complete!");

                Console.WriteLine("\n✓ All chat tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Chat test failed:{Environment.NewLine}{ex.ToString()}");
            }
        }

        #endregion

        #region Helpers

        static bool EnsureModelSelected()
        {
            if (String.IsNullOrEmpty(_CurrentModel))
            {
                Console.WriteLine("No model selected. Use 'model' command to select a model.");
                return false;
            }
            return true;
        }

        static double CosineSimilarity(float[] vector1, float[] vector2)
        {
            if (vector1.Length != vector2.Length)
                throw new ArgumentException("Vectors must have the same length");

            double dotProduct = 0;
            double magnitude1 = 0;
            double magnitude2 = 0;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += vector1[i] * vector1[i];
                magnitude2 += vector2[i] * vector2[i];
            }

            magnitude1 = Math.Sqrt(magnitude1);
            magnitude2 = Math.Sqrt(magnitude2);

            if (magnitude1 == 0 || magnitude2 == 0)
                return 0;

            return dotProduct / (magnitude1 * magnitude2);
        }

        #endregion
    }
}