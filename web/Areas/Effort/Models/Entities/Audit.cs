namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents audit trail entries for effort system changes.
/// Maps to effort.Audits table.
/// </summary>
public class Audit
{
    public int Id { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int RecordId { get; set; }
    public string Action { get; set; } = string.Empty;
    public int ChangedBy { get; set; }
    public DateTime ChangedDate { get; set; }
    public string? Changes { get; set; }
    public DateTime? MigratedDate { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }

    // Legacy preservation columns
    public string? LegacyAction { get; set; }
    public string? LegacyCRN { get; set; }
    public int? LegacyTermCode { get; set; }
    public string? LegacyMothraID { get; set; }
}
