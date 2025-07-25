﻿namespace SharpAI.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using RestWrapper;
    using SyslogLogging;

    /// <summary>
    /// Client for interacting with HuggingFace model repositories to retrieve file information and download files.
    /// </summary>
    public class HuggingFaceClient
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.

        #region Public-Members

        #endregion

        #region Private-Members

        private readonly string _Header = "[HuggingFace] ";
        private readonly string _ApiKey;
        private readonly LoggingModule _Logging;
        private readonly JsonSerializerOptions _JsonOptions;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the HuggingFace client.
        /// </summary>
        /// <param name="logging">LoggingModule instance for logging operations.</param>
        /// <param name="apiKey">HuggingFace API key for authentication.</param>
        public HuggingFaceClient(LoggingModule logging, string apiKey)
        {
            if (String.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(nameof(apiKey));

            _ApiKey = apiKey;
            _Logging = logging ?? new LoggingModule();

            _JsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            _Logging.Debug(_Header + "initialized");
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Retrieves all files from a HuggingFace model repository.
        /// </summary>
        /// <param name="modelName">The name of the model (e.g., "microsoft/DialoGPT-medium").</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A list of HuggingFaceModelFile objects representing all files in the repository.</returns>
        /// <exception cref="Exception">Thrown when API request fails or JSON parsing fails.</exception>
        public async Task<List<HuggingFaceModelFile>> GetModelFilesAsync(string modelName, CancellationToken token = default)
        {
            try
            {
                _Logging.Debug(_Header + $"retrieving model files for {modelName}");

                string url = $"https://huggingface.co/api/models/{modelName}/tree/main";

                using (RestRequest request = new RestRequest(url, HttpMethod.Get))
                {
                    request.Headers.Add("Authorization", $"Bearer {_ApiKey}");
                    request.Headers.Add("User-Agent", "SharpAI");

                    using (RestResponse response = await request.SendAsync(token).ConfigureAwait(false))
                    {
                        if (response.StatusCode < 200 || response.StatusCode >= 300)
                        {
                            _Logging.Error(_Header + $"request failed with status {response.StatusCode}" + (!String.IsNullOrEmpty(response.DataAsString) ? ":" + Environment.NewLine + response.DataAsString : ""));
                            throw new Exception("Request failed with status " + response.StatusCode + (!String.IsNullOrEmpty(response.DataAsString) ? ":" + Environment.NewLine + response.DataAsString : ""));
                        }

                        JsonDocument jsonDocument = JsonDocument.Parse(response.DataAsString);
                        List<HuggingFaceModelFile> modelFiles = new List<HuggingFaceModelFile>();

                        foreach (JsonElement element in jsonDocument.RootElement.EnumerateArray())
                        {
                            HuggingFaceModelFile file = new HuggingFaceModelFile
                            {
                                Path = GetStringValue(element, "path"),
                                Type = GetStringValue(element, "type"),
                                Size = GetLongValue(element, "size"),
                                LastModified = GetDateTimeValue(element, "lastModified"),
                                Oid = GetStringValue(element, "oid"),
                                Lfs = GetStringValue(element, "lfs"),
                                SecurityStatus = GetStringValue(element, "securityStatus")
                            };

                            modelFiles.Add(file);
                        }

                        _Logging.Info(_Header + $"retrieved {modelFiles.Count} files for model {modelName}");
                        return modelFiles;
                    }
                }
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "exception retrieving model files:" + Environment.NewLine + ex.ToString());
                throw new Exception($"Error retrieving model files:{Environment.NewLine}{ex.ToString()}", ex);
            }
        }

        /// <summary>
        /// Retrieves only GGUF files from a HuggingFace model repository.
        /// </summary>
        /// <param name="modelName">The name of the model to search for GGUF files.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A list of GgufFileInfo objects containing only .gguf files with enhanced metadata.</returns>
        public async Task<List<GgufFileInfo>> GetGgufFilesAsync(string modelName, CancellationToken token = default)
        {
            _Logging.Debug(_Header + $"filtering GGUF files for model {modelName}");

            List<HuggingFaceModelFile> allFiles = await GetModelFilesAsync(modelName, token).ConfigureAwait(false);

            List<GgufFileInfo> ggufFiles = allFiles.Where(f =>
                f.Type?.Equals("file", StringComparison.OrdinalIgnoreCase) == true &&
                f.Path?.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase) == true
            ).Select(file => new GgufFileInfo
            {
                Path = file.Path,
                Type = file.Type,
                Size = file.Size,
                LastModified = file.LastModified,
                Oid = file.Oid,
                Lfs = file.Lfs,
                SecurityStatus = file.SecurityStatus,
                SizeFormatted = FormatFileSize(file.Size),
                QuantizationType = ExtractQuantizationType(file.Path),
                IsMainModel = IsMainModelFile(file.Path)
            }).OrderByDescending(f => f.Size ?? 0).ToList();

            _Logging.Info(_Header + $"found {ggufFiles.Count} GGUF files for model {modelName}");
            return ggufFiles;
        }

        /// <summary>
        /// Retrieves GGUF files filtered by a specific quantization type.
        /// </summary>
        /// <param name="modelName">The name of the model to search.</param>
        /// <param name="quantizationType">The quantization type to filter by (e.g., "Q4_K_M", "Q5_K_S").</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A list of GgufFileInfo objects matching the specified quantization type.</returns>
        public async Task<List<GgufFileInfo>> GetGgufFilesByQuantizationAsync(string modelName, string quantizationType, CancellationToken token = default)
        {
            _Logging.Debug(_Header + $"getting GGUF files with quantization {quantizationType} for model: {modelName}");

            List<GgufFileInfo> allGgufFiles = await GetGgufFilesAsync(modelName, token).ConfigureAwait(false);

            List<GgufFileInfo> filteredFiles = allGgufFiles.Where(f =>
                f.QuantizationType?.Equals(quantizationType, StringComparison.OrdinalIgnoreCase) == true
            ).ToList();

            _Logging.Info(_Header + $"found {filteredFiles.Count} files with {quantizationType} quantization for model {modelName}");
            return filteredFiles;
        }

        /// <summary>
        /// Retrieves all available quantization types for GGUF files in a model repository.
        /// </summary>
        /// <param name="modelName">The name of the model to analyze.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A sorted list of unique quantization type strings.</returns>
        public async Task<List<string>> GetAvailableQuantizationTypesAsync(string modelName, CancellationToken token = default)
        {
            _Logging.Debug(_Header + $"getting available quantization types for model: {modelName}");

            List<GgufFileInfo> ggufFiles = await GetGgufFilesAsync(modelName, token).ConfigureAwait(false);

            List<string> quantTypes = ggufFiles
                .Where(f => !string.IsNullOrEmpty(f.QuantizationType))
                .Select(f => f.QuantizationType)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(q => q)
                .ToList();

            _Logging.Info(_Header + $"found {quantTypes.Count} quantization types for model {modelName} {string.Join(", ", quantTypes)}");
            return quantTypes;
        }

        /// <summary>
        /// Gets the proper download URLs for a HuggingFace file, prioritizing the most reliable options.
        /// </summary>
        /// <param name="modelName">The model name (e.g., "microsoft/phi-2").</param>
        /// <param name="file">The HuggingFaceModelFile containing path and OID.</param>
        /// <returns>List of URLs to try in order of preference.</returns>
        public List<string> GetDownloadUrls(string modelName, HuggingFaceModelFile file)
        {
            string normalizedPath = file.Path.Replace("\\", "/");
            if (normalizedPath.StartsWith("/"))
                normalizedPath = normalizedPath.Substring(1);

            List<string> urls = new List<string>();

            // If we have an OID (commit hash), use it for the most reliable download
            if (!string.IsNullOrEmpty(file.Oid))
            {
                urls.Add($"https://huggingface.co/{modelName}/resolve/{file.Oid}/{normalizedPath}");
            }

            // Fallback URLs
            urls.Add($"https://huggingface.co/{modelName}/resolve/main/{normalizedPath}");
            urls.Add($"https://huggingface.co/{modelName}/resolve/HEAD/{normalizedPath}");

            // For GGUF files, also try /raw/ but it's less reliable for large files
            if (file.Path.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
            {
                urls.Add($"https://huggingface.co/{modelName}/raw/main/{normalizedPath}");
            }

            return urls;
        }

        /// <summary>
        /// Tests if a download URL is accessible.
        /// </summary>
        /// <param name="url">The URL to test.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the URL is accessible, false otherwise.</returns>
        public async Task<bool> TestDownloadUrlAsync(string url, CancellationToken token = default)
        {
            try
            {
                using (RestRequest request = new RestRequest(url, HttpMethod.Head))
                {
                    request.AllowAutoRedirect = true;

                    using (RestResponse response = await request.SendAsync(token).ConfigureAwait(false))
                    {
                        bool isAccessible = (response.StatusCode >= 200 && response.StatusCode < 400);
                        _Logging.Debug(_Header + $"URL test for {url}: {response.StatusCode} - {(isAccessible ? "accessible" : "not accessible")}");
                        return isAccessible;
                    }
                }
            }
            catch (Exception ex)
            {
                _Logging.Debug(_Header + $"URL test failed for {url}:{Environment.NewLine}{ex.ToString()}");
                return false;
            }
        }

        /// <summary>
        /// Try to download a file from HuggingFace to the specified destination.
        /// </summary>
        /// <param name="sourceUrl">The HuggingFace URL of the file to download.</param>
        /// <param name="destinationFilename">The full path where the file should be saved.</param>
        /// <param name="progressCallback">Progress callback.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if successful.</returns>
        public async Task<bool> TryDownloadFileAsync(string sourceUrl, string destinationFilename, Action<string, long, decimal> progressCallback, CancellationToken token = default)
        {
            try
            {
                await DownloadFileAsync(sourceUrl, destinationFilename, progressCallback, token).ConfigureAwait(false);
                return true;
            }
            catch (Exception e)
            {
                _Logging.Warn($"{_Header}exception downloading {sourceUrl} to {destinationFilename}: {e.Message}");
                return false;
            }
        }
        /// <summary>
        /// Downloads a file from HuggingFace to the specified destination.
        /// </summary>
        /// <param name="sourceUrl">The HuggingFace URL of the file to download.</param>
        /// <param name="destinationFilename">The full path where the file should be saved.</param>
        /// <param name="progressCallback">Progress callback (URL, bytes downloaded, percentage 0-1).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task representing the asynchronous download operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when destinationFilename or sourceUrl is null or empty.</exception>
        /// <exception cref="Exception">Thrown when download fails or file operations fail.</exception>
        public async Task DownloadFileAsync(string sourceUrl, string destinationFilename, Action<string, long, decimal> progressCallback, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(destinationFilename))
                throw new ArgumentNullException(nameof(destinationFilename));

            if (string.IsNullOrWhiteSpace(sourceUrl))
                throw new ArgumentNullException(nameof(sourceUrl));

            try
            {
                _Logging.Debug(_Header + $"starting download from {sourceUrl} to {destinationFilename}");

                string destinationDirectory = Path.GetDirectoryName(destinationFilename);
                if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                    _Logging.Debug(_Header + $"created destination directory {destinationDirectory}");
                }

                // First, get the metadata to find the actual download URL for LFS files
                string actualDownloadUrl = await GetActualDownloadUrlAsync(sourceUrl, token).ConfigureAwait(false);

                using (var httpClient = new HttpClient())
                {
                    // Get response with headers first to check content length
                    using (var response = await httpClient.GetAsync(actualDownloadUrl, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();

                        var contentLength = response.Content.Headers.ContentLength;
                        var isChunked = response.Headers.TransferEncodingChunked ?? false;

                        _Logging.Debug(_Header + $"content length: {contentLength?.ToString() ?? "unknown"}, chunked: {isChunked}");

                        using (var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        using (var fileStream = new FileStream(destinationFilename, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 65536, useAsync: true))
                        {
                            var buffer = new byte[65536]; // 64KB buffer
                            long totalBytesRead = 0;
                            int bytesRead;
                            DateTime lastProgressUpdate = DateTime.UtcNow;

                            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead, token).ConfigureAwait(false);
                                totalBytesRead += bytesRead;

                                // Report progress
                                if (progressCallback != null)
                                {
                                    var now = DateTime.UtcNow;
                                    // Update progress at most every 100ms to avoid overwhelming the callback
                                    if ((now - lastProgressUpdate).TotalMilliseconds >= 100)
                                    {
                                        decimal progress;
                                        if (contentLength.HasValue && contentLength.Value > 0)
                                        {
                                            // We know the total size, calculate actual percentage
                                            progress = (decimal)totalBytesRead / contentLength.Value;
                                        }
                                        else
                                        {
                                            // Chunked transfer or unknown size
                                            // Report bytes downloaded as a very small decimal to indicate progress
                                            // This will be between 0 and 1, but won't represent actual percentage
                                            progress = Math.Min(0.99m, totalBytesRead / (decimal)int.MaxValue);
                                        }

                                        progressCallback(sourceUrl, totalBytesRead, progress);
                                        lastProgressUpdate = now;
                                    }
                                }
                            }

                            // Final progress callback at 100%
                            progressCallback?.Invoke(sourceUrl, totalBytesRead, 1.0m);
                        }

                        var fileInfo = new FileInfo(destinationFilename);
                        _Logging.Info(_Header + $"successfully downloaded {fileInfo.Length} bytes to {destinationFilename}");
                    }
                }
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "exception in download:" + Environment.NewLine + ex.ToString());

                // Report failure through callback
                progressCallback?.Invoke(sourceUrl, 0, -1);

                // Clean up partial file if it exists
                try
                {
                    if (File.Exists(destinationFilename))
                    {
                        File.Delete(destinationFilename);
                        _Logging.Debug(_Header + "cleaned up partial download file");
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }

                string errorMsg = $"error downloading file:{Environment.NewLine}{ex.ToString()}";
                throw new Exception(errorMsg, ex);
            }
        }

        /// <summary>
        /// Downloads multiple files from HuggingFace with progress tracking and error handling.
        /// </summary>
        /// <param name="modelName">The name of the model.</param>
        /// <param name="files">List of files to download.</param>
        /// <param name="downloadDirectory">Directory to save files to.</param>
        /// <param name="progressCallback">Progress callback (URL, bytes downloaded, percentage 0-1).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Number of successfully downloaded files.</returns>
        public async Task<int> DownloadFilesAsync(
            string modelName, 
            List<HuggingFaceModelFile> files, 
            string downloadDirectory, 
            Action<string, long, decimal> progressCallback = null, 
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(downloadDirectory))
                throw new ArgumentNullException(nameof(downloadDirectory));

            if (files == null || files.Count == 0)
                return 0;

            _Logging.Info(_Header + $"starting batch download of {files.Count} files to {downloadDirectory}");

            // Ensure download directory exists
            if (!Directory.Exists(downloadDirectory))
            {
                Directory.CreateDirectory(downloadDirectory);
                _Logging.Debug(_Header + $"created download directory: {downloadDirectory}");
            }

            int successCount = 0;

            foreach (var file in files)
            {
                try
                {
                    string fileName = Path.GetFileName(file.Path);
                    string destinationPath = Path.Combine(downloadDirectory, fileName);

                    _Logging.Debug(_Header + $"downloading {fileName}");

                    List<string> urls = GetDownloadUrls(modelName, file);
                    bool downloadSuccessful = false;
                    string lastError = null;

                    foreach (string url in urls)
                    {
                        try
                        {
                            await DownloadFileAsync(url, destinationPath, progressCallback, token).ConfigureAwait(false);
                            downloadSuccessful = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            lastError = ex.Message;
                            _Logging.Debug(_Header + $"download attempt failed for {url}:{Environment.NewLine}{ex.ToString()}");
                        }
                    }

                    if (downloadSuccessful)
                    {
                        successCount++;
                        _Logging.Info(_Header + $"successfully downloaded {fileName}");
                    }
                    else
                    {
                        _Logging.Error(_Header + $"failed to download {fileName}: {lastError}");
                    }
                }
                catch (Exception ex)
                {
                    string fileName = Path.GetFileName(file.Path);
                    _Logging.Error(_Header + $"download failed for {fileName}:{Environment.NewLine}{ex.ToString()}");
                }
            }

            _Logging.Info(_Header + $"batch download completed, {successCount}/{files.Count} files successful");
            return successCount;
        }

        #endregion

        #region Private-Methods

        private async Task<string> GetActualDownloadUrlAsync(string sourceUrl, CancellationToken token = default)
        {
            try
            {
                using (RestRequest request = new RestRequest(sourceUrl, HttpMethod.Head))
                {
                    request.Headers.Add("Authorization", $"Bearer {_ApiKey}");
                    request.AllowAutoRedirect = false; // Don't follow redirects, we want to capture them

                    using (RestResponse response = await request.SendAsync(token).ConfigureAwait(false))
                    {
                        if (response.StatusCode >= 300 && response.StatusCode < 400)
                        {
                            string redirectUrl = response.Headers["Location"];
                            if (!string.IsNullOrEmpty(redirectUrl))
                            {
                                _Logging.Debug(_Header + $"LFS redirect detected {sourceUrl} to {redirectUrl}");
                                return redirectUrl;
                            }
                        }

                        return sourceUrl;
                    }
                }
            }
            catch (Exception ex)
            {
                _Logging.Debug(_Header + $"failed to get metadata for {sourceUrl}:{Environment.NewLine}{ex.ToString()}");
                return sourceUrl;
            }
        }

        private string ExtractQuantizationType(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;

            string fileName = Path.GetFileNameWithoutExtension(filePath);

            // Common GGUF quantization patterns
            string[] quantizationPatterns = new[]
            {
                "Q2_K", "Q3_K_S", "Q3_K_M", "Q3_K_L", "Q4_0", "Q4_1", "Q4_K_S", "Q4_K_M",
                "Q5_0", "Q5_1", "Q5_K_S", "Q5_K_M", "Q6_K", "Q8_0", "F16", "F32"
            };

            foreach (string pattern in quantizationPatterns)
            {
                if (fileName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    // _Logging.Debug(_Header + $"detected quantization type {pattern} in file: {filePath}");
                    return pattern;
                }
            }

            _Logging.Debug(_Header + $"unknown quantization type for file: {filePath}");
            return "Unknown";
        }

        private bool IsMainModelFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            string fileName = Path.GetFileName(filePath).ToLowerInvariant();

            // Check if it's likely the main model file (not a shard)
            bool isMain = !fileName.Contains("-of-") &&
                          !fileName.Contains("shard") &&
                          !fileName.Contains("part");

            _Logging.Debug(_Header + $"file {filePath} is {(isMain ? "main model" : "shard/part")}");
            return isMain;
        }

        private string FormatFileSize(long? bytes)
        {
            if (!bytes.HasValue)
                return "Unknown";

            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (bytes >= GB)
                return $"{bytes.Value / (double)GB:F2} GB";
            if (bytes >= MB)
                return $"{bytes.Value / (double)MB:F2} MB";
            if (bytes >= KB)
                return $"{bytes.Value / (double)KB:F2} KB";

            return $"{bytes.Value} bytes";
        }

        private string GetStringValue(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement prop))
                return null;

            try
            {
                switch (prop.ValueKind)
                {
                    case JsonValueKind.String:
                        return prop.GetString();
                    case JsonValueKind.Number:
                        return prop.GetDecimal().ToString();
                    case JsonValueKind.True:
                        return "true";
                    case JsonValueKind.False:
                        return "false";
                    case JsonValueKind.Null:
                        return null;
                    case JsonValueKind.Object:
                        if (propertyName == "lfs" && prop.TryGetProperty("oid", out JsonElement lfsOid))
                        {
                            return lfsOid.GetString();
                        }
                        return prop.GetRawText();
                    case JsonValueKind.Array:
                        return prop.GetRawText();
                    default:
                        return prop.GetRawText();
                }
            }
            catch (Exception ex)
            {
                _Logging.Debug(_Header + $"failed to extract string value for property '{propertyName}':{Environment.NewLine}{ex.ToString()}");
                return null;
            }
        }

        private long? GetLongValue(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement prop))
                return null;

            try
            {
                if (prop.ValueKind == JsonValueKind.Number)
                {
                    return prop.GetInt64();
                }
                else if (prop.ValueKind == JsonValueKind.String)
                {
                    string strValue = prop.GetString();
                    if (long.TryParse(strValue, out long result))
                    {
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                _Logging.Debug(_Header + $"failed to extract long value for property '{propertyName}':{Environment.NewLine}{ex.ToString()}");
            }

            return null;
        }

        private DateTime? GetDateTimeValue(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement prop))
                return null;

            try
            {
                if (prop.ValueKind == JsonValueKind.String)
                {
                    string dateStr = prop.GetString();
                    if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out DateTime result))
                    {
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                _Logging.Debug(_Header + $"failed to extract DateTime value for property '{propertyName}':{Environment.NewLine}{ex.ToString()}");
            }

            return null;
        }

        #endregion

#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}