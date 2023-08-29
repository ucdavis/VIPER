using System.Text.Json.Serialization;

namespace Viper.Areas.RAPS.Models
{
    public class VMACSExportPermission
    {
        [JsonPropertyName("id")]
        public int Id {  get; set; }
        [JsonPropertyName("group")]
        public required string Group { get; set; }
        [JsonPropertyName("permission")]
        public required string Permission { get; set; }
        [JsonPropertyName("accessCodes")]
        public string AccessCodes { get; set; } = string.Empty;
    }
}
