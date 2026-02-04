using System.Text.Json.Serialization;

namespace Viper.Areas.RAPS.Models
{
    public class VMACSExportUser
    {
        [JsonPropertyName("casLogin")]
        public required string CasLogin { get; set; }
        [JsonPropertyName("email")]
        public required string Email { get; set; }
        [JsonPropertyName("accessCodes")]
        public string AccessCodes { get; set; } = string.Empty;
        [JsonPropertyName("permissionIds")]
        public List<int> PermissionIds { get; set; } = new List<int>();
    }
}
