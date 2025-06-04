using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class FormFieldOption
{
    public int FormFieldOptionId { get; set; }

    public int FormFieldId { get; set; }

    public string Value { get; set; } = null!;

    public virtual FormField FormField { get; set; } = null!;

    public virtual ICollection<FormFieldOptionVersion> FormFieldOptionVersions { get; set; } = new List<FormFieldOptionVersion>();
}
