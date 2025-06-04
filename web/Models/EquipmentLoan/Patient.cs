using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class Patient
{
    public int PatientId { get; set; }

    public int? PatientNumber { get; set; }

    public string PatientName { get; set; } = null!;

    public string Gender { get; set; } = null!;

    public string Species { get; set; } = null!;

    public virtual ICollection<Encounter> Encounters { get; set; } = new List<Encounter>();
}
