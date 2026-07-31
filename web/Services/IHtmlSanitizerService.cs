namespace Viper.Services
{
    public interface IHtmlSanitizerService
    {
        string Sanitize(string html);

        /// <summary>
        /// Sanitize generated diff markup. Same policy as <see cref="Sanitize"/> but additionally
        /// permits the &lt;ins&gt;/&lt;del&gt; markers htmldiff.net wraps changes in, so a diff can be
        /// re-parsed (balancing the library's malformed tags) and stripped to policy before render.
        /// </summary>
        string SanitizeDiff(string html);
    }
}
