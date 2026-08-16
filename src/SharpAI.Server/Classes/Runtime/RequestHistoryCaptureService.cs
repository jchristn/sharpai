namespace SharpAI.Server.Classes.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Text;
    using System.Threading.Tasks;

    using SharpAI.Database;
    using SharpAI.Models;
    using SharpAI.Server.Classes.Settings;

    using SyslogLogging;

    using WatsonWebserver.Core;

    /// <summary>
    /// Captures a durable record of each HTTP request and its response. The entry is built synchronously
    /// from the request context (so request state is not lost) and written on a background task so capture
    /// never blocks the response. Secrets in headers are redacted and bodies are truncated to the
    /// configured limits. Response bodies are not retained by the current routing layer, so only response
    /// metadata and headers are captured.
    /// </summary>
    public class RequestHistoryCaptureService
    {
        #region Private-Members

        private readonly string _Header = "[RequestHistoryCapture] ";
        private readonly DatabaseDriverBase _Database;
        private readonly RequestHistorySettings _Settings;
        private readonly LoggingModule _Logging;

        private static readonly HashSet<string> _RedactExact = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "authorization", "proxy-authorization", "cookie", "set-cookie"
        };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="database">Database driver.</param>
        /// <param name="settings">Request-history settings.</param>
        /// <param name="logging">Logging module.</param>
        public RequestHistoryCaptureService(DatabaseDriverBase database, RequestHistorySettings settings, LoggingModule logging)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Capture the given request context. Builds the entry synchronously and writes it on a background
        /// task. Never throws.
        /// </summary>
        /// <param name="ctx">Request context.</param>
        public void Capture(HttpContextBase ctx)
        {
            if (ctx == null || !_Settings.Enabled) return;

            try
            {
                RequestHistoryEntry entry = BuildEntry(ctx);
                _ = Task.Run(() =>
                {
                    try
                    {
                        _Database.RequestHistory.Create(entry);
                    }
                    catch (Exception ex)
                    {
                        _Logging.Warn(_Header + "failed to persist request history:" + Environment.NewLine + ex.ToString());
                    }
                });
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "failed to build request history entry:" + Environment.NewLine + ex.ToString());
            }
        }

        #endregion

        #region Private-Methods

        private RequestHistoryEntry BuildEntry(HttpContextBase ctx)
        {
            string url = ctx.Request.Url != null ? ctx.Request.Url.RawWithQuery : String.Empty;
            string path = url;
            int queryIndex = url.IndexOf('?');
            if (queryIndex >= 0) path = url.Substring(0, queryIndex);

            RequestHistoryEntry entry = new RequestHistoryEntry
            {
                Method = ctx.Request.Method.ToString(),
                Path = path,
                Url = url,
                StatusCode = ctx.Response.StatusCode,
                DurationMs = ctx.Timestamp.TotalMs.HasValue ? ctx.Timestamp.TotalMs.Value : 0,
                SourceIp = ctx.Request.Source != null ? ctx.Request.Source.IpAddress : null,
                RequestHeaders = ToHeaderDictionary(ctx.Request.Headers),
                ResponseHeaders = ToHeaderDictionary(ctx.Response.Headers),
                CreatedUtc = ctx.Timestamp.Start,
                CompletedUtc = ctx.Timestamp.End
            };

            CaptureRequestBody(ctx, entry);
            return entry;
        }

        private void CaptureRequestBody(HttpContextBase ctx, RequestHistoryEntry entry)
        {
            try
            {
                if (ctx.Request.ChunkedTransfer) return;

                string body = ctx.Request.DataAsString;
                if (String.IsNullOrEmpty(body)) return;

                long originalBytes = Encoding.UTF8.GetByteCount(body);
                entry.RequestBodyBytes = originalBytes;

                if (_Settings.MaxRequestBodyBytes <= 0)
                {
                    entry.RequestBody = null;
                    entry.RequestBodyTruncated = originalBytes > 0;
                    return;
                }

                if (originalBytes > _Settings.MaxRequestBodyBytes)
                {
                    int keep = Math.Min(body.Length, _Settings.MaxRequestBodyBytes);
                    entry.RequestBody = body.Substring(0, keep);
                    entry.RequestBodyTruncated = true;
                }
                else
                {
                    entry.RequestBody = body;
                }
            }
            catch
            {
                // Body not available (streaming/chunked); leave it unset.
            }
        }

        private Dictionary<string, string> ToHeaderDictionary(NameValueCollection headers)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (headers == null) return result;

            foreach (string key in headers.AllKeys)
            {
                if (String.IsNullOrEmpty(key)) continue;
                result[key] = IsSecretHeader(key) ? "[redacted]" : headers[key];
            }

            return result;
        }

        private static bool IsSecretHeader(string name)
        {
            if (_RedactExact.Contains(name)) return true;
            string lower = name.ToLowerInvariant();
            return lower.Contains("api-key") || lower.Contains("token");
        }

        #endregion
    }
}
