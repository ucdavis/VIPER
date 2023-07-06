using System;
using System.Collections.Generic;

namespace Viper.Models.RAPS;

public partial class VwEmployee
{
    public string MothraId { get; set; } = null!;

    public string? MemberId { get; set; } = null!;

    public string? EmployeeId { get; set; }

    public string? LoginId { get; set; }

    public string DisplayFirstName { get; set; } = null!;

    public string DisplayLastName { get; set; } = null!;

    public string DisplayFullName { get; set; } = null!;
}
