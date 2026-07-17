using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Serialization;
using Viper.Areas.Directory.Models;

namespace Viper.Areas.Directory.Services
{
    public class VMACSService
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        // Search runs once per directory result, so an unset base URL or auth
        // token would log per lookup; guard so each is recorded at most once.
        private static int _missingBaseUrlLogged;
        private static int _missingAuthTokenLogged;

        private static string VmacsAuthToken =>
            HttpHelper.GetSetting<string>("Credentials", "VmacsAuthToken") ?? string.Empty;

        private static string? VmacsBaseUrl =>
            HttpHelper.GetSetting<string>("Vmacs", "BaseUrl");

        // Bound the per-lookup wait: DirectoryController blocks on Search().Result
        // once per result, so a hung endpoint must not stall for the default 100s.
        private static readonly HttpClient sharedClient = new() { Timeout = TimeSpan.FromSeconds(10) };

        protected VMACSService() { }

        /// <summary>
        /// </summary>
        /// <param name="loginID"></param>
        /// <returns></returns>
        public static async Task<VMACSQuery?> Search(String? loginID)
        {
            string? baseUrl = VmacsBaseUrl;
            if (!IsValidBaseUrl(baseUrl))
            {
                if (Interlocked.Exchange(ref _missingBaseUrlLogged, 1) == 0)
                {
                    _logger.Warn(
                        "VMACS lookup skipped: Vmacs:BaseUrl is not configured or is not a valid absolute http(s) URL.");
                }
                return null;
            }

            string authToken = VmacsAuthToken;
            if (string.IsNullOrWhiteSpace(authToken))
            {
                // No token means every request would fail auth and return null;
                // skip the doomed call so a directory search doesn't fan out one
                // unauthorized request per result (e.g. Dev, which has no token).
                if (Interlocked.Exchange(ref _missingAuthTokenLogged, 1) == 0)
                {
                    _logger.Warn("VMACS lookup skipped: Credentials:VmacsAuthToken is not configured.");
                }
                return null;
            }

            string request = baseUrl.TrimEnd('/') + BuildSearchPath(loginID, authToken);
            string body;
            try
            {
                using HttpResponseMessage response = await sharedClient.GetAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                body = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                // The VMACS endpoint is known to be flaky; an outage must degrade to
                // null, not throw and abort the whole directory search (the caller
                // blocks on .Result, so enrichment being optional demands this).
                _logger.Warn(ex, "VMACS directory query request failed.");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                // HttpClient.Timeout elapsed - treat a slow endpoint like an outage.
                _logger.Warn(ex, "VMACS directory query request timed out.");
                return null;
            }

            VMACSQuery? vmacs_api;
            try
            {
                vmacs_api = Deserialize(body);
            }
            catch (InvalidOperationException ex)
            {
                // A non-XML 200 (gateway/proxy error page, SSO redirect) must not
                // fail the directory search; enrichment is optional, so degrade to null.
                _logger.Warn(ex, "Failed to deserialize VMACS directory query response.");
                return null;
            }
            if (vmacs_api != null && vmacs_api.item != null)
            {
                return vmacs_api;
            }
            return null;
        }

        /// <summary>
        /// Deserializes a VMACS query.xml payload with XXE protections. Returns the
        /// parsed query - whose item is null when nothing matched - or null.
        /// </summary>
        internal static VMACSQuery? Deserialize(string xml)
        {
            using var reader = new StringReader(xml);
            var serializer = new XmlSerializer(typeof(VMACSQuery));

            // Use XmlReader with secure settings to prevent XXE attacks
            var readerSettings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };
            using var xmlReader = XmlReader.Create(reader, readerSettings);
            return (VMACSQuery?)serializer.Deserialize(xmlReader);
        }

        /// <summary>
        /// True when the configured base URL is an absolute http(s) URI. Guards both
        /// the directory lookup and the health-check probe so a malformed or
        /// unsupported-scheme value fails closed instead of throwing from GetAsync.
        /// </summary>
        internal static bool IsValidBaseUrl([NotNullWhen(true)] string? baseUrl) =>
            !string.IsNullOrWhiteSpace(baseUrl)
            && Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        internal static string BuildSearchPath(string? loginID, string authToken)
        {
            string encodedLoginId = Uri.EscapeDataString(loginID ?? string.Empty);
            return $"/trust/query.xml?dbfile=3&index=CampusLoginId&find={encodedLoginId}&format=CHRIS4&AUTH={authToken}";
        }
    }
}
