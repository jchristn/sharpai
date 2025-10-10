namespace SharpAI.Server.Classes.Runtime
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using LLama.Native;
    using SharpAI.Server.Classes.Settings;
    using SyslogLogging;

    /// <summary>
    /// Native library bootstrapper for LlamaSharp.
    /// Detects GPU availability and configures the appropriate native library before LlamaSharp static initialization.
    /// </summary>
    public static class NativeLibraryBootstrapper
    {
        #region Public-Members

        /// <summary>
        /// Gets a value indicating whether the bootstrapper has been initialized.
        /// </summary>
        public static bool IsInitialized
        {
            get
            {
                return _IsInitialized;
            }
        }

        /// <summary>
        /// Gets the backend that was selected during initialization.
        /// </summary>
        public static string SelectedBackend
        {
            get
            {
                return _SelectedBackend;
            }
        }

        #endregion

        #region Private-Members

        private static bool _IsInitialized = false;
        private static string _SelectedBackend = "unknown";
        private static readonly object _InitializationLock = new object();

        #endregion

        #region Public-Methods

        /// <summary>
        /// Initialize the native library configuration.
        /// This MUST be called before any LlamaSharp types are referenced.
        /// </summary>
        /// <param name="settings">Server settings containing backend configuration.</param>
        /// <param name="logging">Logging module.</param>
        public static void Initialize(Settings settings, LoggingModule logging)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            lock (_InitializationLock)
            {
                if (_IsInitialized)
                {
                    logging.Debug("[NativeLibraryBootstrapper] already initialized, skipping");
                    return;
                }

                string backend = DetermineBackend(settings, logging);
                _SelectedBackend = backend;

                string libraryPath = GetLibraryPath(backend, settings, logging);

                if (!String.IsNullOrEmpty(libraryPath))
                {
                    if (File.Exists(libraryPath))
                    {
                        logging.Info($"[NativeLibraryBootstrapper] configuring {backend} backend: {libraryPath}");

                        try
                        {
                            // On Linux, pre-load dependencies BEFORE configuring library
                            if (GetCurrentPlatform() == OSPlatform.Linux)
                            {
                                string libraryDir = Path.GetDirectoryName(libraryPath);
                                PreLoadLinuxDependencies(libraryDir, logging);
                            }

                            // CRITICAL: Configure library path BEFORE any LlamaSharp types are referenced
                            NativeLibraryConfig
                                .All
                                .WithLibrary(libraryPath, "llama");

                            logging.Info($"[NativeLibraryBootstrapper] successfully configured {backend} backend");

                            // Force NativeApi static constructor to run now (while we can catch errors)
                            // This ensures the library actually loads successfully
                            try
                            {
                                long deviceCount = NativeApi.llama_max_devices();
                                logging.Debug($"[NativeLibraryBootstrapper] library loaded successfully, {deviceCount} device(s) reported");
                            }
                            catch (Exception initEx)
                            {
                                throw new Exception($"Library configured but failed to load: {initEx.Message}" + Environment.NewLine + initEx.ToString(), initEx);
                            }

                            // Now safe to configure logging (after library is loaded)
                            ConfigureNativeLogging(settings, logging);
                        }
                        catch (Exception ex)
                        {
                            logging.Warn($"[NativeLibraryBootstrapper] failed to configure {backend} backend, will attempt fallback: {ex.Message}" + Environment.NewLine + ex.ToString());

                            // Try CPU fallback
                            if (!backend.Equals("cpu", StringComparison.OrdinalIgnoreCase))
                            {
                                string cpuPath = GetLibraryPath("cpu", settings, logging);
                                if (!String.IsNullOrEmpty(cpuPath) && File.Exists(cpuPath))
                                {
                                    logging.Info($"[NativeLibraryBootstrapper] attempting CPU fallback: {cpuPath}");

                                    try
                                    {
                                        // On Linux, pre-load dependencies BEFORE configuring library
                                        if (GetCurrentPlatform() == OSPlatform.Linux)
                                        {
                                            string cpuDir = Path.GetDirectoryName(cpuPath);
                                            PreLoadLinuxDependencies(cpuDir, logging);
                                        }

                                        NativeLibraryConfig
                                            .All
                                            .WithLibrary(cpuPath, "llama");

                                        _SelectedBackend = "cpu";
                                        logging.Info("[NativeLibraryBootstrapper] successfully configured CPU backend as fallback");

                                        // Force library load
                                        try
                                        {
                                            long deviceCount = NativeApi.llama_max_devices();
                                            logging.Debug($"[NativeLibraryBootstrapper] fallback library loaded successfully, {deviceCount} device(s) reported");
                                        }
                                        catch (Exception initEx)
                                        {
                                            throw new Exception($"Fallback library configured but failed to load: {initEx.Message}" + Environment.NewLine + initEx.ToString(), initEx);
                                        }

                                        // Configure logging after library is loaded
                                        ConfigureNativeLogging(settings, logging);
                                    }
                                    catch (Exception fallbackEx)
                                    {
                                        logging.Warn($"[NativeLibraryBootstrapper] CPU fallback also failed: {fallbackEx.Message}" + Environment.NewLine + fallbackEx.ToString());
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        logging.Warn($"[NativeLibraryBootstrapper] library file not found: {libraryPath}");
                    }
                }
                else
                {
                    logging.Debug("[NativeLibraryBootstrapper] no explicit library path configured, using default LlamaSharp library loading");
                }

                _IsInitialized = true;
            }
        }

        #endregion

        #region Private-Methods

        private static void PreLoadLinuxDependencies(string libraryDir, LoggingModule logging)
        {
            if (GetCurrentPlatform() != OSPlatform.Linux)
            {
                return;
            }

            logging.Debug($"[NativeLibraryBootstrapper] pre-loading Linux dependencies from: {libraryDir}");

            // List of dependencies in load order (base libraries first)
            string[] dependencies = new string[]
            {
                "libggml-base.so",
                "libggml-cpu.so",
                "libggml.so"
            };

            foreach (string dep in dependencies)
            {
                string depPath = Path.Combine(libraryDir, dep);
                if (File.Exists(depPath))
                {
                    try
                    {
                        IntPtr handle = NativeLibrary.Load(depPath);
                        logging.Debug($"[NativeLibraryBootstrapper] pre-loaded dependency: {dep}");
                    }
                    catch (Exception ex)
                    {
                        logging.Warn($"[NativeLibraryBootstrapper] failed to pre-load {dep}: {ex.Message}" + Environment.NewLine + ex.ToString());
                    }
                }
                else
                {
                    logging.Debug($"[NativeLibraryBootstrapper] dependency not found: {depPath}");
                }
            }
        }

        private static void ConfigureNativeLogging(Settings settings, LoggingModule logging)
        {
            bool enableLogging = settings.Runtime?.EnableNativeLogging ?? false;

            if (!enableLogging)
            {
                // Disable native logging by setting a no-op callback that discards all messages
                try
                {
                    NativeLogConfig.LLamaLogCallback noOpCallback = (LLamaLogLevel level, string message) =>
                    {
                        // Discard all log messages by doing nothing
                    };

                    NativeLogConfig.llama_log_set(noOpCallback);
                    logging.Debug("[NativeLibraryBootstrapper] native library logging disabled");
                }
                catch (Exception ex)
                {
                    logging.Debug($"[NativeLibraryBootstrapper] failed to disable native logging: {ex.Message}");
                }
            }
            else
            {
                logging.Debug("[NativeLibraryBootstrapper] native library logging enabled");
            }
        }

        private static string DetermineBackend(Settings settings, LoggingModule logging)
        {
            // Check for forced backend setting
            if (!String.IsNullOrEmpty(settings.Runtime?.ForceBackend))
            {
                string forced = settings.Runtime.ForceBackend.ToLowerInvariant();
                logging.Info($"[NativeLibraryBootstrapper] backend forced to: {forced}");
                return forced;
            }

            // Detect platform
            OSPlatform platform = GetCurrentPlatform();
            Architecture architecture = RuntimeInformation.ProcessArchitecture;

            logging.Debug($"[NativeLibraryBootstrapper] detected platform: {platform}, architecture: {architecture}");

            // Apple Silicon cannot use CUDA
            if (platform == OSPlatform.OSX && architecture == Architecture.Arm64)
            {
                logging.Debug("[NativeLibraryBootstrapper] Apple Silicon detected, GPU backend not supported, using CPU");
                return "cpu";
            }

            // Check for GPU availability
            bool gpuAvailable = DetectGpuAvailability(platform, logging);

            if (gpuAvailable)
            {
                logging.Info("[NativeLibraryBootstrapper] GPU detected, selecting CUDA backend");
                return "cuda";
            }
            else
            {
                logging.Info("[NativeLibraryBootstrapper] no GPU detected, selecting CPU backend");
                return "cpu";
            }
        }

        private static bool DetectGpuAvailability(OSPlatform platform, LoggingModule logging)
        {
            // Method 1: Check for NVIDIA driver file (Linux)
            if (platform == OSPlatform.Linux)
            {
                if (File.Exists("/proc/driver/nvidia/version"))
                {
                    logging.Debug("[NativeLibraryBootstrapper] NVIDIA driver detected via /proc/driver/nvidia/version");
                    return true;
                }
            }

            // Method 2: Check environment variable set by NVIDIA Docker runtime
            string nvidiaVisible = Environment.GetEnvironmentVariable("NVIDIA_VISIBLE_DEVICES");
            if (!String.IsNullOrEmpty(nvidiaVisible) && !nvidiaVisible.Equals("void", StringComparison.OrdinalIgnoreCase))
            {
                logging.Debug($"[NativeLibraryBootstrapper] NVIDIA_VISIBLE_DEVICES detected: {nvidiaVisible}");
                return true;
            }

            // Method 3: Try executing nvidia-smi
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = platform == OSPlatform.Windows ? "nvidia-smi.exe" : "nvidia-smi",
                    Arguments = "--query-gpu=name --format=csv,noheader",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit(5000);

                        if (process.ExitCode == 0 && !String.IsNullOrWhiteSpace(output))
                        {
                            logging.Debug($"[NativeLibraryBootstrapper] nvidia-smi detected GPU: {output.Trim()}");
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logging.Debug($"[NativeLibraryBootstrapper] nvidia-smi check failed: {ex.Message}");
            }

            // Method 4: Check for CUDA library files
            if (platform == OSPlatform.Linux)
            {
                string[] cudaPaths = new string[]
                {
                    "/usr/lib/x86_64-linux-gnu/libcuda.so.1",
                    "/usr/lib64/libcuda.so.1",
                    "/usr/local/cuda/lib64/libcuda.so.1"
                };

                foreach (string path in cudaPaths)
                {
                    if (File.Exists(path))
                    {
                        logging.Debug($"[NativeLibraryBootstrapper] CUDA library detected: {path}");
                        return true;
                    }
                }
            }
            else if (platform == OSPlatform.Windows)
            {
                string systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
                if (!String.IsNullOrEmpty(systemRoot))
                {
                    string cudaDll = Path.Combine(systemRoot, "System32", "nvcuda.dll");
                    if (File.Exists(cudaDll))
                    {
                        logging.Debug($"[NativeLibraryBootstrapper] CUDA library detected: {cudaDll}");
                        return true;
                    }
                }
            }

            logging.Debug("[NativeLibraryBootstrapper] no GPU detected via any method");
            return false;
        }

        private static string GetLibraryPath(string backend, Settings settings, LoggingModule logging)
        {
            // Check for explicit path in settings
            if (backend.Equals("cpu", StringComparison.OrdinalIgnoreCase))
            {
                if (!String.IsNullOrEmpty(settings.Runtime?.CpuBackendPath))
                {
                    string expandedPath = Environment.ExpandEnvironmentVariables(settings.Runtime.CpuBackendPath);
                    logging.Debug($"[NativeLibraryBootstrapper] using CPU backend path from settings: {expandedPath}");
                    return expandedPath;
                }
            }
            else if (backend.Equals("cuda", StringComparison.OrdinalIgnoreCase))
            {
                if (!String.IsNullOrEmpty(settings.Runtime?.GpuBackendPath))
                {
                    string expandedPath = Environment.ExpandEnvironmentVariables(settings.Runtime.GpuBackendPath);
                    logging.Debug($"[NativeLibraryBootstrapper] using GPU backend path from settings: {expandedPath}");
                    return expandedPath;
                }
            }

            OSPlatform platform = GetCurrentPlatform();
            string baseDirectory = AppContext.BaseDirectory;
            string libraryName = GetNativeLibraryName(platform);

            // Try custom Docker structure first (for containers)
            string customPath = Path.Combine(baseDirectory, "runtimes", backend, libraryName);
            if (File.Exists(customPath))
            {
                logging.Debug($"[NativeLibraryBootstrapper] found library at custom path: {customPath}");
                return customPath;
            }

            // Try standard NuGet runtime structure (for local development)
            string nugetPath = GetNuGetRuntimePath(backend, platform, baseDirectory, libraryName, logging);
            if (!String.IsNullOrEmpty(nugetPath) && File.Exists(nugetPath))
            {
                logging.Debug($"[NativeLibraryBootstrapper] found library at NuGet path: {nugetPath}");
                return nugetPath;
            }

            // Return custom path as fallback (will log file not found later)
            logging.Debug($"[NativeLibraryBootstrapper] library not found, returning default path: {customPath}");
            return customPath;
        }

        private static string GetNuGetRuntimePath(string backend, OSPlatform platform, string baseDirectory, string libraryName, LoggingModule logging)
        {
            string rid = GetRuntimeIdentifier(platform);
            Architecture arch = RuntimeInformation.ProcessArchitecture;

            if (backend.Equals("cuda", StringComparison.OrdinalIgnoreCase))
            {
                // CUDA backend: try cuda12 subdirectory first
                string cudaPath = Path.Combine(baseDirectory, "runtimes", rid, "native", "cuda12", libraryName);
                if (File.Exists(cudaPath))
                {
                    return cudaPath;
                }

                // Try without subdirectory
                cudaPath = Path.Combine(baseDirectory, "runtimes", rid, "native", libraryName);
                if (File.Exists(cudaPath))
                {
                    return cudaPath;
                }
            }
            else if (backend.Equals("cpu", StringComparison.OrdinalIgnoreCase))
            {
                // For ARM64 (Apple Silicon, ARM Linux), no AVX variants exist
                if (arch == Architecture.Arm64)
                {
                    // Try direct path first
                    string armPath = Path.Combine(baseDirectory, "runtimes", rid, "native", libraryName);
                    if (File.Exists(armPath))
                    {
                        logging.Debug("[NativeLibraryBootstrapper] using ARM64 CPU backend");
                        return armPath;
                    }
                }
                else
                {
                    // For x64, try AVX variants in order of preference: avx2, avx512, avx, noavx
                    string[] avxVariants = new string[] { "avx2", "avx512", "avx", "noavx" };
                    foreach (string variant in avxVariants)
                    {
                        string avxPath = Path.Combine(baseDirectory, "runtimes", rid, "native", variant, libraryName);
                        if (File.Exists(avxPath))
                        {
                            logging.Debug($"[NativeLibraryBootstrapper] using {variant} CPU variant");
                            return avxPath;
                        }
                    }

                    // Try without subdirectory as fallback
                    string fallbackPath = Path.Combine(baseDirectory, "runtimes", rid, "native", libraryName);
                    if (File.Exists(fallbackPath))
                    {
                        return fallbackPath;
                    }
                }
            }

            return null;
        }

        private static string GetRuntimeIdentifier(OSPlatform platform)
        {
            Architecture arch = RuntimeInformation.ProcessArchitecture;

            if (platform == OSPlatform.Windows)
            {
                return arch == Architecture.X64 ? "win-x64" : "win-arm64";
            }
            else if (platform == OSPlatform.Linux)
            {
                return arch == Architecture.X64 ? "linux-x64" : "linux-arm64";
            }
            else if (platform == OSPlatform.OSX)
            {
                return arch == Architecture.X64 ? "osx-x64" : "osx-arm64";
            }

            return "unknown";
        }

        private static string GetNativeLibraryName(OSPlatform platform)
        {
            if (platform == OSPlatform.Windows)
            {
                return "llama.dll";
            }
            else if (platform == OSPlatform.Linux)
            {
                return "libllama.so";
            }
            else if (platform == OSPlatform.OSX)
            {
                return "libllama.dylib";
            }
            else
            {
                return "libllama.so"; // fallback
            }
        }

        private static OSPlatform GetCurrentPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OSPlatform.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return OSPlatform.Linux;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OSPlatform.OSX;
            }
            else
            {
                return OSPlatform.Linux; // fallback
            }
        }

        #endregion
    }
}
