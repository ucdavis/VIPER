using System.Text.Json.Serialization;

namespace Viper.Models
{
    public class ClientData
    {
        public string IP { get; set; } = null!;

        [JsonPropertyName("User_Agent")]
        public string UserAgent { get; set; } = null!;

        public string? Referrer { get; set; }

    }
}
