using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class VwVmthStudent
{
    public int? VmacsId { get; set; }

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string MiddleName { get; set; } = null!;

    public string? StudentId { get; set; }

    public string? KerberosId { get; set; }

    public string? Email { get; set; }

    public string? ClassLevel { get; set; }

    public string? MothraId { get; set; }
}
