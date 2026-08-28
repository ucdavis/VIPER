using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace Viper.Classes
{
    /// <summary>
    /// Canonical public origin for this deployment, bound from the "Application" configuration
    /// section. Deployed environments must set it; Development derives the origin from the
    /// request so the dynamic local port keeps working.
    /// </summary>
    public class PublicUrlOptions
    {
        public const string SectionName = "Application";

        /// <summary>
        /// Absolute base URL including scheme, host, optional port and PathBase, e.g.
        /// "https://viper.vetmed.ucdavis.edu/2".
        /// </summary>
        public string? PublicBaseUrl { get; set; }
    }

    /// <summary>
    /// Supplies the origin for URLs that leave the application (CAS service callbacks, sitemap
    /// entries, emulation links). Deployed environments read it from configuration so a forged
    /// Host header cannot influence a security callback.
    /// </summary>
    public interface IPublicUrlService
    {
        /// <summary>
        /// Canonical base URL with no trailing slash, e.g. "https://viper.vetmed.ucdavis.edu/2".
        /// </summary>
        string BaseUrl { get; }

        /// <summary>
        /// Canonical base URL plus an application-relative path, e.g. BuildUrl("/CasLogin").
        /// </summary>
        string BuildUrl(string relativePath);
    }

    /// <inheritdoc cref="IPublicUrlService"/>
    public class PublicUrlService : IPublicUrlService
    {
        private readonly string? _configuredBaseUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PublicUrlService(IOptions<PublicUrlOptions> options, IHttpContextAccessor httpContextAccessor)
        {
            _configuredBaseUrl = NormalizeBaseUrl(options.Value.PublicBaseUrl);
            _httpContextAccessor = httpContextAccessor;
        }

        public string BaseUrl
        {
            get
            {
                if (_configuredBaseUrl != null)
                {
                    return _configuredBaseUrl;
                }

                HttpRequest? request = _httpContextAccessor.HttpContext?.Request;
                return request != null ? FromRequest(request) : LocalDevelopmentOrigin;
            }
        }

        public string BuildUrl(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return BaseUrl;
            }

            return BaseUrl + (relativePath.StartsWith('/') ? relativePath : "/" + relativePath);
        }

        /// <summary>
        /// Trims whitespace and any trailing slash so callers can append "/Path" unconditionally.
        /// Returns null when nothing is configured.
        /// </summary>
        public static string? NormalizeBaseUrl(string? configured)
        {
            return string.IsNullOrWhiteSpace(configured) ? null : configured.Trim().TrimEnd('/');
        }

        /// <summary>
        /// Last resort for Development work that has no request to derive from, such as email
        /// sent from a background job. Resolved once because process environment variables do
        /// not change after start. Deployed environments never reach it because
        /// PublicUrlOptionsValidator fails startup when the canonical origin is missing.
        /// </summary>
        private static readonly string LocalDevelopmentOrigin = BuildLocalDevelopmentOrigin();

        private static string BuildLocalDevelopmentOrigin()
        {
            const int defaultPort = 7157;
            string? httpsPort = Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORT");
            int port = int.TryParse(httpsPort, out int parsed) && parsed > 0 && parsed < 65536 ? parsed : defaultPort;
            return $"https://localhost:{port}";
        }

        /// <summary>
        /// Development fallback: derive the origin from the current request, preserving the
        /// PathBase. Deployed environments never reach this because PublicUrlOptionsValidator
        /// fails startup when the setting is missing.
        /// </summary>
        private static string FromRequest(HttpRequest request)
        {
            string origin = new Uri(request.GetDisplayUrl()).GetLeftPart(UriPartial.Authority);
            return origin + request.PathBase.Value?.TrimEnd('/');
        }
    }

    /// <summary>
    /// Fails startup when a deployed environment has no usable canonical origin, so the app
    /// cannot silently fall back to request-derived URLs for CAS callbacks.
    /// </summary>
    public class PublicUrlOptionsValidator : IValidateOptions<PublicUrlOptions>
    {
        private readonly IWebHostEnvironment _environment;

        public PublicUrlOptionsValidator(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public ValidateOptionsResult Validate(string? name, PublicUrlOptions options)
        {
            return ValidateBaseUrl(options.PublicBaseUrl, _environment.IsDevelopment());
        }

        /// <summary>
        /// Exposed for tests: applies the same rules the startup validator uses.
        /// </summary>
        public static ValidateOptionsResult ValidateBaseUrl(string? configured, bool isDevelopment)
        {
            const string setting = "Application:PublicBaseUrl";
            string? normalized = PublicUrlService.NormalizeBaseUrl(configured);

            if (normalized == null)
            {
                return isDevelopment
                    ? ValidateOptionsResult.Success
                    : ValidateOptionsResult.Fail($"{setting} is required outside Development. Set it to the canonical public URL, for example https://viper.vetmed.ucdavis.edu/2.");
            }

            if (!Uri.TryCreate(normalized, UriKind.Absolute, out Uri? uri))
            {
                return ValidateOptionsResult.Fail($"{setting} must be an absolute URL.");
            }

            if (uri.Scheme != Uri.UriSchemeHttps && !(isDevelopment && uri.Scheme == Uri.UriSchemeHttp))
            {
                return ValidateOptionsResult.Fail($"{setting} must use https outside Development.");
            }

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                return ValidateOptionsResult.Fail($"{setting} must not contain user information.");
            }

            if (!string.IsNullOrEmpty(uri.Query))
            {
                return ValidateOptionsResult.Fail($"{setting} must not contain a query string.");
            }

            if (!string.IsNullOrEmpty(uri.Fragment))
            {
                return ValidateOptionsResult.Fail($"{setting} must not contain a fragment.");
            }

            return ValidateOptionsResult.Success;
        }
    }
}
