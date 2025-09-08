using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class Import3
{
    public int ImportId { get; set; }

    public string? KeyNumber { get; set; }

    public string? CutNumber { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public string? Disposition { get; set; }

    public DateTime? DispositionDate { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? MothraId { get; set; }
}
