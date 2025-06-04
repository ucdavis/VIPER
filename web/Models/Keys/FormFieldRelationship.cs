using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class FormFieldRelationship
{
    public int FormFieldRelationshipId { get; set; }

    public int ParentFormFieldId { get; set; }

    public int ChildFormFieldId { get; set; }

    public int FieldOrder { get; set; }

    public virtual FormField ChildFormField { get; set; } = null!;

    public virtual FormField ParentFormField { get; set; } = null!;
}
