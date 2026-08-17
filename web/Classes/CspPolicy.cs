using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;

namespace Viper.Classes
{
    /// <summary>
    /// Content-Security-Policy helpers.
    /// The application-wide policy still carries 'unsafe-eval' because the legacy Razor pages load
    /// Vue's full build (wwwroot/lib/vue/dist/vue.global.prod.js) and mount it on &lt;body&gt; with
    /// an in-DOM template, so Vue compiles that template at runtime through Function(code)().
    /// Removing the allowance blanks every page rendered by Views/Shared/_VIPERLayout.cshtml.
    /// </summary>
    public static class CspPolicy
    {
        private const string UnsafeEval = "'unsafe-eval'";

        /// <summary>
        /// Drops the allowance from a built Vue SPA response, whose templates Vite precompiled.
        /// </summary>
        public static void TightenForBuiltSpa(StaticFileResponseContext ctx)
        {
            var headers = ctx.Context.Response.Headers;
            if (headers.TryGetValue(HeaderNames.ContentSecurityPolicy, out var policy))
            {
                headers[HeaderNames.ContentSecurityPolicy] = WithoutUnsafeEval(policy.ToString());
            }
        }

        /// <summary>Returns the policy with every 'unsafe-eval' source expression removed.</summary>
        public static string WithoutUnsafeEval(string? headerValue)
        {
            return string.IsNullOrEmpty(headerValue)
                ? string.Empty
                : string.Join(';', headerValue.Split(';').Select(StripUnsafeEval));
        }

        private static string StripUnsafeEval(string directive)
        {
            if (!directive.Contains(UnsafeEval, StringComparison.Ordinal))
            {
                return directive;
            }

            string[] kept = directive
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(token => !string.Equals(token, UnsafeEval, StringComparison.Ordinal))
                .ToArray();

            // Dropping the only source expression would leave a bare directive name. An empty
            // source list already matches nothing, but 'none' states that explicitly.
            return kept.Length == 1
                ? kept[0] + " 'none'"
                : string.Join(' ', kept);
        }
    }
}
