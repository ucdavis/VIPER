using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class FormFieldOptionVersion
{
    public int FormFieldOptionVersionId { get; set; }

    public int FormFieldOptionId { get; set; }

    public int Version { get; set; }

    public bool Default { get; set; }

    public int Order { get; set; }

    public virtual FormFieldOption FormFieldOption { get; set; } = null!;
}
