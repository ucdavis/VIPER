using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class Status
{
    public int StatusRecordId { get; set; }

    public string? StatusTermCode { get; set; }

    public string? StatusTableName { get; set; }

    public int? StatusRecordCount { get; set; }

    public DateTime? StatusDatetime { get; set; }
}
