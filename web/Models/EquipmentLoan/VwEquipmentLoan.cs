using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class VwEquipmentLoan
{
    public int? AssetId { get; set; }

    public string? AssetName { get; set; }

    public int? AssetType { get; set; }

    public string? AssetClass { get; set; }

    public DateTime? AssetInsuranceDate { get; set; }

    public DateTime? AssetDecommissionDate { get; set; }

    public DateTime? AssetRepairDate { get; set; }

    public int? AssetOs { get; set; }

    public string? AssetStatus { get; set; }

    public string? AssetTag { get; set; }

    public string? AssetSerial { get; set; }

    public string? AssetPart { get; set; }

    public string? AssetMake { get; set; }

    public string? AssetModel { get; set; }

    public int? AssetParent { get; set; }

    public string? AssetTagUnq { get; set; }

    public int LoanId { get; set; }

    public string LoanPidm { get; set; } = null!;

    public string LoanTechPidm { get; set; } = null!;

    public DateTime LoanDate { get; set; }

    public int LoanReason { get; set; }

    public string? LoanComments { get; set; }

    public bool LoanExtendedOk { get; set; }

    public DateTime? LoanDueDate { get; set; }

    public string? LoanAuthorization { get; set; }

    public bool LoanExclude { get; set; }

    public DateTime? LoanExcludeDate { get; set; }

    public int? LoanSdpId { get; set; }

    public string? LoanSignature { get; set; }
}
