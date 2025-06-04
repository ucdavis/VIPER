using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class EncounterInstructor
{
    public int EncounterInstructorId { get; set; }

    public int InstructorId { get; set; }

    public int EncounterId { get; set; }

    public virtual Encounter Encounter { get; set; } = null!;

    public virtual Person Instructor { get; set; } = null!;
}
