using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class UcpathVerificationItem
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? VerificationType { get; set; }

    public string? Status { get; set; }

    public DateTime? Timestamp { get; set; }

    public string? LoginId { get; set; }
}
