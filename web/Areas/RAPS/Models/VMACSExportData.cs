using System.Text.Json.Serialization;

namespace Viper.Areas.RAPS.Models
{
    public class VMACSExportData
    {
        [JsonPropertyName("permissions")]
        public List<VMACSExportPermission> Permissions { get; set; } = new();
        [JsonPropertyName("users")]
        public List<VMACSExportUser> Users { get; set; } = new();
    }
}
