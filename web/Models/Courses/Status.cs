namespace Viper.Models.Courses;

public partial class Status
{
    public int StatusRecordId { get; set; }

    public string? StatusTermCode { get; set; }

    public string? StatusTableName { get; set; }

    public int? StatusRecordCount { get; set; }

    public DateTime? StatusDatetime { get; set; }
}
