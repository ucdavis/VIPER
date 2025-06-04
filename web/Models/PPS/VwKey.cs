using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class VwKey
{
    public int Id { get; set; }

    public int KeyId { get; set; }

    public string CutNumber { get; set; } = null!;

    public string? RequestedBy { get; set; }

    public string? AssignedTo { get; set; }

    public string? AdHocName { get; set; }

    public DateTime? RequestDate { get; set; }

    public DateTime? IssuedDate { get; set; }

    public string? IssuedBy { get; set; }

    public int? Disposition { get; set; }

    public DateTime? DispositionDate { get; set; }

    public string? DispositionBy { get; set; }

    public DateTime? DueDate { get; set; }

    public int? Deleted { get; set; }

    public int? Expr1 { get; set; }

    public string? KeyNumber { get; set; }

    public string? AccessDescription { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public string? ManagedBy { get; set; }

    public bool? BuildingMaster { get; set; }

    public bool? Submaster { get; set; }

    public bool? Grandmaster { get; set; }

    public bool? Restricted { get; set; }

    public string? RestrictedContact { get; set; }

    public bool? Expr2 { get; set; }

    public string? Notes { get; set; }

    public string? IssuedBy1 { get; set; }
}
