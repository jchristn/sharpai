<div align="center">
  <img src="https://github.com/jchristn/sharpai/blob/main/assets/logo.png" width="256" height="256">
</div>

# SharpAI

**Transform your .NET applications into AI powerhouses - embed models directly or deploy as an Ollama-compatible API server. No cloud dependencies, no limits, just pure local inference.**

<p align="center">
  <img src="https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white" />
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" />
  <img src="https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge" />
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/SharpAI/">
    <img src="https://img.shields.io/nuget/v/SharpAI.svg?style=flat" alt="NuGet Version">
  </a>
  &nbsp;
  <a href="https://www.nuget.org/packages/SharpAI">
    <img src="https://img.shields.io/nuget/dt/SharpAI.svg" alt="NuGet Downloads">
  </a>
</p>

<p align="center">
  <strong>A .NET library for local AI model inference with Ollama-compatible REST API</strong>
</p>

<p align="center">
  Embeddings • Completions • Chat • Built on LlamaSharp • GGUF Models Only
</p>

---

## 🚀 Features

- **Ollama-Compatible REST API Server** - Provides endpoints compatible with Ollama's API
- **Model Management** - Download and manage GGUF models from HuggingFace
- **Multiple Inference Types**:
  - Text embeddings generation
  - Text completions
  - Chat completions
- **Prompt Engineering Tools** - Built-in helpers for formatting prompts for different model types
- **GPU Acceleration** - Automatic CUDA detection when available
- **Streaming Support** - Real-time token streaming for completions
- **SQLite Model Registry** - Tracks model metadata and file information

## 📋 Table of Contents

- [Installation](#-installation)
- [Core Components](#-core-components)
- [Model Management](#-model-management)
- [Generating Embeddings](#-generating-embeddings)
- [Text Completions](#-text-completions)
- [Chat Completions](#-chat-completions)
- [Prompt Formatting](#-prompt-formatting)
- [API Server](#-api-server)
- [Requirements](#-requirements)
- [Version History](#-version-history)
- [License](#-license)
- [Acknowledgments](#-acknowledgments)

## 📦 Installation

Install SharpAI via NuGet:

```bash
dotnet add package SharpAI
```

Or via Package Manager Console:

```powershell
Install-Package SharpAI
```

## 📖 Core Components

### AIDriver

The main entry point that provides access to all functionality:

```csharp
using SharpAI;
using SyslogLogging;

// Initialize the AI driver
var ai = new AIDriver(
    logging: new LoggingModule(), 
    databaseFilename: "./sharpai.db",     
    huggingFaceApiKey: "hf_xxxxxxxxxxxx", 
    modelDirectory: "./models/"           
);

// Download a model from HuggingFace (GGUF format)
await ai.Models.Add("microsoft/phi-2");

// Generate a completion
string response = await ai.Completion.GenerateCompletion(
    model: "microsoft/phi-2",
    prompt: "Once upon a time",
    maxTokens: 512,
    temperature: 0.7f
);
```

The AIDriver provides access to APIs via:
- `ai.Models` - Model management operations
- `ai.Embeddings` - Embedding generation
- `ai.Completion` - Text completion generation
- `ai.Chat` - Chat completion generation

### ModelDriver

Manages model downloads and lifecycle:

```csharp
// List all downloaded models
List<ModelFile> models = ai.Models.All();

// Get a specific model
ModelFile model = ai.Models.GetByName("microsoft/phi-2");

// Download a new model from HuggingFace
ModelFile downloaded = await ai.Models.Add("meta-llama/Llama-2-7b-chat-hf");

// Delete a model
ai.Models.Delete("microsoft/phi-2");

// Get the filesystem path for a model
string modelPath = ai.Models.GetFilename("microsoft/phi-2");
```

## 🗄️ Model Management

SharpAI automatically handles downloading GGUF files from HuggingFace. Only GGUF format models are supported.

- Queries available GGUF files for a model
- Selects appropriate quantization based on file naming conventions
- Downloads and stores models with metadata
- Tracks model information in local Sqlite model registry

Model metadata includes:

- Model name and GUID
- File size and hashes (MD5, SHA1, SHA256)
- Quantization type
- Source URL
- Creation timestamps

## 🔢 Generating Embeddings

Generate vector embeddings for text:

```csharp
// Single text embedding
float[] embedding = await ai.Embeddings.Generate(
    model: "microsoft/phi-2",
    input: "This is a sample text"
);

// Multiple text embeddings
string[] texts = { "First text", "Second text", "Third text" };
float[][] embeddings = await ai.Embeddings.Generate(
    model: "microsoft/phi-2",
    inputs: texts
);
```

## 📝 Text Completions

> *Note*: for best results, structure your prompt in a manner appropriate for the model you are using.  See the prompt formatting section below.

Generate text continuations:

```csharp
// Non-streaming completion
string completion = await ai.Completion.GenerateCompletion(
    model: "microsoft/phi-2",
    prompt: "The meaning of life is",
    maxTokens: 512,
    temperature: 0.7f
);

// Streaming completion
await foreach (string token in ai.Completion.GenerateCompletionStreaming(
    model: "microsoft/phi-2",
    prompt: "Write a poem about",
    maxTokens: 512,
    temperature: 0.8f))
{
    Console.Write(token);
}
```

## 💬 Chat Completions

> *Note*: for best results, structure your prompt in a manner appropriate for the model you are using.  See the prompt formatting section below.

Generate conversational responses:

```csharp
// Non-streaming chat
string response = await ai.Chat.GenerateCompletion(
    model: "microsoft/phi-2",
    prompt: chatFormattedPrompt,  // Prompt should be formatted for chat
    maxTokens: 512,
    temperature: 0.7f
);

// Streaming chat
await foreach (string token in ai.Chat.GenerateCompletionStreaming(
    model: "microsoft/phi-2",
    prompt: chatFormattedPrompt,
    maxTokens: 512,
    temperature: 0.7f))
{
    Console.Write(token);
}
```

## 🛠️ Prompt Formatting

SharpAI includes prompt builders to format conversations for different model types:

### Chat Message Formatting

```csharp
using SharpAI.Prompts;

var messages = new List<ChatMessage>
{
    new ChatMessage { Role = "system", Content = "You are a helpful assistant." },
    new ChatMessage { Role = "user", Content = "What is the capital of France?" },
    new ChatMessage { Role = "assistant", Content = "The capital of France is Paris." },
    new ChatMessage { Role = "user", Content = "What is its population?" }
};

// Format for different model types
string chatMLPrompt = PromptBuilder.Build(ChatFormat.ChatML, messages);
/* Output:
<|im_start|>system
You are a helpful assistant.<|im_end|>
<|im_start|>user
What is the capital of France?<|im_end|>
<|im_start|>assistant
The capital of France is Paris.<|im_end|>
<|im_start|>user
What is its population?<|im_end|>
<|im_start|>assistant
*/

string llama2Prompt = PromptBuilder.Build(ChatFormat.Llama2, messages);
/* Output:
<s>[INST] <<SYS>>
You are a helpful assistant.
<</SYS>>

What is the capital of France? [/INST] The capital of France is Paris. </s><s>[INST] What is its population? [/INST] 
*/

string simplePrompt = PromptBuilder.Build(ChatFormat.Simple, messages);
/* Output:
system: You are a helpful assistant.
user: What is the capital of France?
assistant: The capital of France is Paris.
user: What is its population?
assistant:
*/
```

Supported chat formats:
- `Simple` - Basic role: content format (generic models, base models)
- `ChatML` - OpenAI ChatML format (GPT models, models fine-tuned with ChatML) including Qwen
- `Llama2` - Llama 2 instruction format (Llama-2-Chat models)
- `Llama3` - Llama 3 format (Llama-3-Instruct models)
- `Alpaca` - Alpaca instruction format (Alpaca, Vicuna, WizardLM, and many Llama-based fine-tunes)
- `Mistral` - Mistral instruction format (Mistral-Instruct, Mixtral-Instruct models)
- `HumanAssistant` - Human/Assistant format (Anthropic Claude-style training, some chat models)
- `Zephyr` - Zephyr model format (Zephyr beta/alpha models)
- `Phi` - Microsoft Phi format (Phi-2, Phi-3 models)
- `DeepSeek` - DeepSeek format (DeepSeek-Coder, DeepSeek-LLM models)

If you are unsure which your model supports, choose `Simple`.

### Text Generation Formatting

```csharp
using SharpAI.Prompts;

// Simple instruction
string instructionPrompt = TextPromptBuilder.Build(
    TextGenerationFormat.Instruction,
    "Write a haiku about programming"
);
/* Output:
### Instruction:
Write a haiku about programming

### Response:
*/

// Code generation with context
var context = new Dictionary<string, string>
{
    ["language"] = "python",
    ["requirements"] = "Include error handling"
};

string codePrompt = TextPromptBuilder.Build(
    TextGenerationFormat.CodeGeneration,
    "Write a function to parse JSON",
    context
);
/* Output:
Language: python
Task: Write a function to parse JSON
Requirements: Include error handling

```python
*/

// Question-answer format
string qaPrompt = TextPromptBuilder.Build(
    TextGenerationFormat.QuestionAnswer,
    "What causes rain?"
);
/* Output:
Question: What causes rain?

Answer:
*/

// Few-shot examples
var examples = new List<(string input, string output)>
{
    ("2+2", "4"),
    ("5*3", "15")
};

string fewShotPrompt = TextPromptBuilder.BuildWithExamples(
    TextGenerationFormat.QuestionAnswer,
    "7-3",
    examples
);
/* Output:
Examples:

Question: 2+2

Answer:
4

---

Question: 5*3

Answer:
15

---

Now complete the following:

Question: 7-3

Answer:
*/
```

Supported text generation formats:
- `Raw` - No formatting
- `Completion` - Continuation format
- `Instruction` - Instruction/response format
- `QuestionAnswer` - Q&A format
- `CreativeWriting` - Story/creative format
- `CodeGeneration` - Code generation format
- `Academic` - Academic writing format
- `ListGeneration` - List creation format
- `TemplateFilling` - Template completion
- `Dialogue` - Dialogue generation

## 🌐 API Server

SharpAI includes a fully-functional REST API server through the **SharpAI.Server** project, which provides Ollama-compatible endpoints. The server acts and behaves like Ollama (with minor gaps), allowing you to use existing Ollama clients and integrations with SharpAI.

Key endpoints include:
- `/api/generate` - Text generation
- `/api/chat` - Chat completions
- `/api/embeddings` - Generate embeddings
- `/api/tags` - List available models
- `/api/pull` - Download models from HuggingFace

Refer to the SharpAI.Server documentation for deployment and configuration details.

## ⚙️ Requirements

- .NET 8.0 or higher
- Windows, Linux, or macOS
- HuggingFace API key (for downloading models)
- (Optional) GPU for acceleration (see GPU Support section)

### Tested Platforms

SharpAI has been tested on:
- Windows 11
- macOS Sequoia
- Ubuntu 24.04

## 📊 Model Information

When models are downloaded, the following information is tracked:

- Model name and unique GUID
- File size
- MD5, SHA1, and SHA256 hashes
- Quantization type (e.g., Q4_K_M, Q5_K_S)
- Source URL from HuggingFace
- Download and creation timestamps

## 🔧 Configuration

### Directory Structure

Models are stored in the specified `modelDirectory` with files named by their GUID. Model metadata is stored in the SQLite database specified by `databaseFilename`.

### GPU Support

The library automatically detects CUDA availability and optimizes layer allocation. The `LlamaSharpEngine` determines optimal GPU layers based on available hardware.

LlamaSharp supports multiple GPU backends through LlamaSharp and llama.cpp:
- **NVIDIA GPUs** - via CUDA
- **AMD GPUs** - via ROCm (Linux) or Vulkan
- **Apple Silicon** - via Metal (M1, M2, M3, etc.)
- **Intel GPUs** - via SYCL or Vulkan

Note: The actual GPU support depends on the LlamaSharp build and backend availability on your system. CUDA support is most mature, while other backends may require specific LlamaSharp builds or additional setup.
## 🐳 Running in Docker

SharpAI.Server is available as a Docker image, providing an easy way to deploy the Ollama-compatible API server without local installation.

### Quick Start

#### Using Docker Run

For Windows:
```batch
run.bat v1.0.0
```

For Linux/macOS:
```bash
./run.sh v1.0.0
```

#### Using Docker Compose

For Windows:
```batch
compose-up.bat
```

For Linux/macOS:
```bash
./compose-up.sh
```

### Prerequisites

Before running the Docker container, ensure you have:

1. **Configuration file**: Create a `sharpai.json` configuration file in your working directory
2. **Directory structure**: The container expects the following directories to exist:
   - `./logs/` - For application logs
   - `./models/` - For storing downloaded GGUF models

### Docker Image

The official Docker image is available at: [`jchristn/sharpai`](https://hub.docker.com/r/jchristn/sharpai).  Refer to the `docker` directory for assets useful for running in Docker and Docker Compose.

### Volume Mappings

The container uses several volume mappings for persistence:

| Host Path | Container Path | Description |
|-----------|---------------|-------------|
| `./sharpai.json` | `/app/sharpai.json` | Configuration file |
| `./sharpai.db` | `/app/sharpai.db` | SQLite database for model registry |
| `./logs/` | `/app/logs/` | Application logs |
| `./models/` | `/app/models/` | Downloaded GGUF model files |

### Configuration

Modify the `sharpai.json` file to supply your configuration.

### Networking

The container exposes port 8000 by default. You can access the API at:
- `http://localhost:8000/api/tags` - List available models
- `http://localhost:8000/api/generate` - Generate text
- `http://localhost:8000/api/chat` - Chat completions
- `http://localhost:8000/api/embeddings` - Generate embeddings

### Example Usage

1. Create the required directory structure:
   ```bash
   mkdir logs models
   ```

2. Create your `sharpai.json` configuration file

3. Run the container:
   ```bash
   # Windows
   run.bat v1.0.0
   
   # Linux/macOS
   ./run.sh v1.0.0
   ```

4. Download a model using the API:
   ```bash
   curl http://localhost:8000/api/pull \
     -d '{"name":"microsoft/phi-2"}'
   ```

5. Generate text:
   ```bash
   curl http://localhost:8000/api/generate \
     -d '{
       "model": "microsoft/phi-2",
       "prompt": "Why is the sky blue?",
       "stream": false
     }'
   ```

### Docker Compose

For production deployments, you can use Docker Compose. Create a `compose.yaml` file:

```yaml
version: '3.8'

services:
  sharpai:
    image: jchristn/sharpai:v3.1.0
    ports:
      - "8000:8000"
    volumes:
      - ./sharpai.json:/app/sharpai.json
      - ./sharpai.db:/app/sharpai.db
      - ./logs:/app/logs
      - ./models:/app/models
    environment:
      - TERM=xterm-256color
    restart: unless-stopped
```

Then run:
```bash
docker compose up -d
```

### GPU Support in Docker

To enable GPU acceleration in Docker:

#### NVIDIA GPUs
Install the [NVIDIA Container Toolkit](https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html) and modify your run command:

```bash
docker run --gpus all \
  -p 8000:8000 \
  -v ./sharpai.json:/app/sharpai.json \
  -v ./sharpai.db:/app/sharpai.db \
  -v ./logs:/app/logs \
  -v ./models:/app/models \
  jchristn/sharpai:v3.1.0
```

For Docker Compose, add:
```yaml
services:
  sharpai:
    # ... other configuration ...
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: all
              capabilities: [gpu]
```

### Troubleshooting

- **Container exits immediately**: Check that `sharpai.json` exists and is valid JSON
- **Models not persisting**: Ensure the `./models/` directory has proper write permissions
- **Cannot connect to API**: Verify port 8000 is not already in use and firewall rules allow access
- **Out of memory errors**: Large models may require significant RAM. Consider using quantized models or adjusting Docker memory limits
## 📚 Version History

Please see the [CHANGELOG.md](CHANGELOG.md) file for detailed version history and release notes.

## 🗺️ Roadmap

The following features are planned for future releases:

- **Progress Events on Model Download** - Model downloads currently do not report progress; adding real-time download progress callbacks
- **Enriching Model Metadata Locally** - Enhanced local tracking of model capabilities, performance metrics, and usage statistics
- **Classifications for Models** - Automatic categorization of models by their primary use case (embeddings vs generation)
- **Native SharpAI API** - Additional functionality beyond Ollama compatibility for advanced use cases

Have a feature request or idea? Please [file an issue](https://github.com/yourusername/sharpai/issues) on our GitHub repository. We welcome community input on our roadmap!

## 📄 License

This project is licensed under the MIT License.

## 🙏 Acknowledgments

- Built on [LlamaSharp](https://github.com/SciSharp/LLamaSharp) for GGUF model inference
- Model hosting by [HuggingFace](https://huggingface.co/)
- Inspired by (and forever grateful to) [Ollama](https://ollama.ai/) for API compatibility
- Special thanks to the community of developers that helped build, test, and refine SharpAI