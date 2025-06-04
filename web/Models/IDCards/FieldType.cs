using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class FieldType
{
    public int FieldTypeId { get; set; }

    public string FieldTypeName { get; set; } = null!;

    public bool CanBeParent { get; set; }

    public bool CanHaveParent { get; set; }

    public bool? PublicEditAllowed { get; set; }

    public virtual ICollection<FormField> FormFields { get; set; } = new List<FormField>();
}
