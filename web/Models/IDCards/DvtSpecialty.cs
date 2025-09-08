using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

/// <summary>
/// Dictionary of Faculty Specialty Area
/// </summary>
public partial class DvtSpecialty
{
    /// <summary>
    /// Pimary key - Faculty Specialty Area ID
    /// </summary>
    public int DvtSpecialtyId { get; set; }

    /// <summary>
    /// Faculty Specialty Area Description
    /// </summary>
    public string DvtSpecialtyArea { get; set; } = null!;
}
