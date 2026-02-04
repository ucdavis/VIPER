namespace Viper.Models.RAPS;

public partial class VmacsLog
{
    public int Id { get; set; }

    public DateTime Timestamp { get; set; }

    public string? MothraId { get; set; }

    public string? Action { get; set; }

    public string? UserLoginids { get; set; }

    public string? StatusCode { get; set; }

    public string? FileContent { get; set; }

    public string? ErrorDetail { get; set; }

    public string? VmacsInstance { get; set; }

    public string? RapsServer { get; set; }
}
