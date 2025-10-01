using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class Asset
{
    public int AssetId { get; set; }

    public string AssetName { get; set; } = null!;

    public int AssetType { get; set; }

    public string? AssetClass { get; set; }

    public DateTime? AssetInsuranceDate { get; set; }

    public DateTime? AssetDecommissionDate { get; set; }

    public DateTime? AssetRepairDate { get; set; }

    public int? AssetOs { get; set; }

    public string AssetStatus { get; set; } = null!;

    public string? AssetTag { get; set; }

    public string? AssetSerial { get; set; }

    public string? AssetPart { get; set; }

    public string? AssetMake { get; set; }

    public string? AssetModel { get; set; }

    public int? AssetParent { get; set; }

    public string? AssetTagUnq { get; set; }

    public virtual ICollection<AssetNote> AssetNotes { get; set; } = new List<AssetNote>();

    public virtual ICollection<LoanItem> LoanItems { get; set; } = new List<LoanItem>();
}
