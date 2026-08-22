using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;

namespace Viper.Classes
{
    /// <summary>
    /// The app-wide policy keeps 'unsafe-eval': _VIPERLayout Razor pages mount Vue's full build on
    /// the document body, compiling that in-DOM template via Function(code)(). Dropping it blanks them.
    /// </summary>
    public static class CspPolicy
    {
        private const string UnsafeEval = "'unsafe-eval'";

        /// <summary>Drops the allowance from a built SPA response with precompiled templates.</summary>
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
            // Without this, a valueless directive like upgrade-insecure-requests would gain 'none'.
            if (!directive.Contains(UnsafeEval, StringComparison.Ordinal))
            {
                return directive;
            }

            string[] kept = directive
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(token => !string.Equals(token, UnsafeEval, StringComparison.Ordinal))
                .ToArray();

            // A bare directive name already matches nothing; 'none' states that explicitly.
            return kept.Length == 1
                ? kept[0] + " 'none'"
                : string.Join(' ', kept);
        }
    }
}
