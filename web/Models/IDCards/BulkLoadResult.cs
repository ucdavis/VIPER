using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class BulkLoadResult
{
    public int BulkResultId { get; set; }

    public string BulkResultMothraId { get; set; } = null!;

    public string BulkResultDisplayName { get; set; } = null!;

    public string BulkResultLastName { get; set; } = null!;

    public bool BulkResultOk { get; set; }
}
