using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class KeyAssignment
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
}
