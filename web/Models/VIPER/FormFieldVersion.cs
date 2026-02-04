namespace Viper.Models.VIPER;

public partial class FormFieldVersion
{
    public int FormFieldVersionId { get; set; }

    public int FormFieldId { get; set; }

    public int Version { get; set; }

    public string FieldName { get; set; } = null!;

    public string? LabelText { get; set; }

    public bool Required { get; set; }

    public int? MinLength { get; set; }

    public int? MaxLength { get; set; }

    public int? MinValue { get; set; }

    public int? MaxValue { get; set; }

    public string? DisplayClass { get; set; }

    public bool AddToSubmissionTitle { get; set; }

    public int? OrderWithinParent { get; set; }

    public string? DefaultValue { get; set; }

    public virtual FormField FormField { get; set; } = null!;
}
