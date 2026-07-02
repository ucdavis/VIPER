using Microsoft.Net.Http.Headers;
using Viper.Areas.CMS.Constants;

namespace Viper.Areas.CMS.Services
{
    /// <summary>
    /// Response hardening shared by the CMS file download paths (/CMS/Files). Every file response
    /// gets X-Content-Type-Options: nosniff so a benign-typed upload cannot be MIME-sniffed into an
    /// active type, and inline-unsafe types (html/svg/...) are forced to download instead of
    /// rendering in the app origin.
    /// </summary>
    public static class CmsFileResponse
    {
        public static void SetNoSniff(HttpResponse response)
        {
            response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        }

        /// <summary>
        /// Content-Disposition type for a served file: "attachment" for types a browser would
        /// execute/render inline, "inline" for everything else.
        /// </summary>
        public static string DispositionType(string fileName)
        {
            return CmsFileTypes.ShouldForceAttachment(fileName) ? "attachment" : "inline";
        }
    }
}
