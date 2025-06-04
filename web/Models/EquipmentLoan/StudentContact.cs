using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class StudentContact
{
    public int StdContactId { get; set; }

    public int Pidm { get; set; }

    public int? PersonId { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? Zip { get; set; }

    public string? Home { get; set; }

    public string? Cell { get; set; }

    public bool ContactPermanent { get; set; }

    public DateTime? LastUpdated { get; set; }

    public string? UpdatedBy { get; set; }

    public virtual ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();

    public virtual Person? Person { get; set; }
}
