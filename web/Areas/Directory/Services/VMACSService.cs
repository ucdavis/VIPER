using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Viper.Areas.Directory.Models;

namespace Viper.Areas.Directory.Services
{
    public class VMACSService
    {
        private static string VmacsAuthToken =>
            HttpHelper.GetSetting<string>("Credentials", "VmacsAuthToken") ?? string.Empty;

        private static readonly HttpClient sharedClient = new()
        {
            BaseAddress = new Uri("https://vmacs-vmth.vetmed.ucdavis.edu"),
        };

        protected VMACSService() { }

        /// <summary>
        /// </summary>
        /// <param name="loginID"></param>
        /// <returns></returns>
        public static async Task<VMACSQuery?> Search(String? loginID)
        {
            string request = BuildSearchPath(loginID, VmacsAuthToken);
            using HttpResponseMessage response = await sharedClient.GetAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            var s = await response.Content.ReadAsStringAsync();
            var buffer = Encoding.UTF8.GetBytes(s);
            using (var stream = new MemoryStream(buffer))
            {
                var serializer = new XmlSerializer(typeof(VMACSQuery));

                // Use XmlReader with secure settings to prevent XXE attacks
                var readerSettings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null
                };
                using (var xmlReader = XmlReader.Create(stream, readerSettings))
                {
                    var vmacs_api = (VMACSQuery?)serializer.Deserialize(xmlReader);
                    if (vmacs_api != null && vmacs_api.item != null)
                    {
                        return vmacs_api;
                    }
                }
            }
            return null;
        }

        internal static string BuildSearchPath(string? loginID, string authToken)
        {
            string encodedLoginId = Uri.EscapeDataString(loginID ?? string.Empty);
            return $"/trust/query.xml?dbfile=3&index=CampusLoginId&find={encodedLoginId}&format=CHRIS4&AUTH={authToken}";
        }
    }
}
