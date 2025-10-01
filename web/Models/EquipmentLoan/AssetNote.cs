using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class AssetNote
{
    public int AssetnoteId { get; set; }

    public int AssetnoteAssetid { get; set; }

    public string AssetnotePidm { get; set; } = null!;

    public DateTime AssetnoteDate { get; set; }

    public string AssetnoteNote { get; set; } = null!;

    public virtual Asset AssetnoteAsset { get; set; } = null!;
}
