# Changelog

## Current Version

v1.0.0

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

## Previous Versions

Notes from previous versions will be placed here.
