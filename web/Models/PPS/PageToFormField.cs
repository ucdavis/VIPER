using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PageToFormField
{
    public int PageToFormFieldId { get; set; }

    public int PageId { get; set; }

    public int FormFieldId { get; set; }

    public int Order { get; set; }

    public bool ReadOnly { get; set; }

    public virtual FormField FormField { get; set; } = null!;

    public virtual Page Page { get; set; } = null!;
}
