using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class DvtClient
{
    public short DvtClientTypeId { get; set; }

    public string DvtClientType { get; set; } = null!;

    public bool? DvtClientSubTypeAble { get; set; }

    public string? DvtClientApproverLoginId { get; set; }

    public bool? DvtClientInUse { get; set; }
}
