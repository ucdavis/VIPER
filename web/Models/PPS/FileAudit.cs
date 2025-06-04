using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class FileAudit
{
    public int AuditId { get; set; }

    public DateTime Timestamp { get; set; }

    public string? Loginid { get; set; }

    public string Action { get; set; } = null!;

    public string? Detail { get; set; }

    public Guid FileGuid { get; set; }

    public string FilePath { get; set; } = null!;

    public string? IamId { get; set; }

    public string? FileMetaData { get; set; }

    public string? ClientData { get; set; }
}
