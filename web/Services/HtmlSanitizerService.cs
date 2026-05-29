using Ganss.Xss;

namespace Viper.Services
{
    /// <summary>
    /// Single, shared HTML sanitizer for user-authored content (CMS content blocks, EPA descriptions, etc.).
    /// Configured per CMS-PLAN.md §11.5. Thread-safe after construction; register as a singleton.
    /// </summary>
    public class HtmlSanitizerService : IHtmlSanitizerService
    {
        private readonly HtmlSanitizer _sanitizer;

        public HtmlSanitizerService()
        {
            _sanitizer = new HtmlSanitizer();

            _sanitizer.AllowedTags.Clear();
            foreach (var tag in new[]
            {
                "p", "br", "hr", "strong", "b", "em", "i", "u", "s", "strike",
                "ul", "ol", "li", "dl", "dt", "dd",
                "h1", "h2", "h3", "h4", "h5", "h6",
                "a", "img", "figure", "figcaption",
                "table", "thead", "tbody", "tfoot", "tr", "th", "td", "caption", "colgroup", "col",
                "div", "span", "blockquote", "pre", "code",
                "sub", "sup", "small", "abbr", "cite", "q"
            })
            {
                _sanitizer.AllowedTags.Add(tag);
            }

            _sanitizer.AllowedAttributes.Clear();
            foreach (var attr in new[]
            {
                "href", "src", "alt", "title", "class", "id", "name",
                "width", "height", "colspan", "rowspan", "scope",
                "target", "rel", "download",
                "style"
            })
            {
                _sanitizer.AllowedAttributes.Add(attr);
            }

            // Curated CSS property allowlist. Mirrors legacy antisamy-cms.xml plus font-size, which
            // exists in current content. Anything outside this list is dropped by the parser, and
            // dangerous CSS value constructs are blocked by Ganss.Xss regardless of this list.
            // Shorthand properties (text-decoration, list-style) get expanded to longhands by
            // AngleSharp; both forms must be allowed or the entire declaration is dropped.
            _sanitizer.AllowedCssProperties.Clear();
            foreach (var prop in new[]
            {
                "color", "font-family", "font-size", "font-style", "font-weight",
                "text-align", "vertical-align",
                "text-decoration", "text-decoration-line", "text-decoration-color",
                "text-decoration-style", "text-decoration-thickness",
                "width", "height",
                "margin", "margin-top", "margin-right", "margin-bottom", "margin-left",
                "padding", "padding-top", "padding-right", "padding-bottom", "padding-left",
                "float", "clear", "display",
                "list-style", "list-style-type", "list-style-position", "list-style-image"
            })
            {
                _sanitizer.AllowedCssProperties.Add(prop);
            }

            _sanitizer.AllowedSchemes.Clear();
            _sanitizer.AllowedSchemes.Add("http");
            _sanitizer.AllowedSchemes.Add("https");
            _sanitizer.AllowedSchemes.Add("mailto");
            _sanitizer.AllowedSchemes.Add("tel");

            // Block all CSS at-rules (@import, @font-face, etc.). At-rules are not valid inside
            // an inline style attribute, but clearing the set is defense in depth against parser
            // edge cases.
            _sanitizer.AllowedAtRules.Clear();

            // Block data: URIs in any URL (image XSS vector) on top of the scheme allowlist.
            // Also lock <img src> to relative URLs only — matches the legacy antisamy-cms.xml
            // onsiteURL regex and prevents editor-authored content from embedding third-party
            // tracking beacons or any absolute-URL image (same-origin absolutes included).
            _sanitizer.FilterUrl += (sender, args) =>
            {
                if (args.OriginalUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    args.SanitizedUrl = null;
                    return;
                }

                if (string.Equals(args.Tag?.TagName, "img", StringComparison.OrdinalIgnoreCase)
                    && IsOffSiteUrl(args.OriginalUrl))
                {
                    args.SanitizedUrl = null;
                }
            };
        }

        private static bool IsOffSiteUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            // Protocol-relative ("//host/...") resolves to an external host in the browser.
            if (url.StartsWith("//", StringComparison.Ordinal)) return true;
            return Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri) && uri.IsAbsoluteUri;
        }

        public string Sanitize(string html) => _sanitizer.Sanitize(html);
    }
}
