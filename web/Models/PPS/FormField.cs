using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class FormField
{
    public int FormFieldId { get; set; }

    public Guid FormId { get; set; }

    public int FieldTypeId { get; set; }

    public string? ViewName { get; set; }

    public string? ViewPermission { get; set; }

    public string? EditPermission { get; set; }

    public int? ParentFormFieldId { get; set; }

    public bool PerUser { get; set; }

    public virtual FieldType FieldType { get; set; } = null!;

    public virtual Form Form { get; set; } = null!;

    public virtual ICollection<FormFieldOption> FormFieldOptions { get; set; } = new List<FormFieldOption>();

    public virtual ICollection<FormFieldRelationship> FormFieldRelationshipChildFormFields { get; set; } = new List<FormFieldRelationship>();

    public virtual ICollection<FormFieldRelationship> FormFieldRelationshipParentFormFields { get; set; } = new List<FormFieldRelationship>();

    public virtual ICollection<FormFieldVersion> FormFieldVersions { get; set; } = new List<FormFieldVersion>();

    public virtual ICollection<FormField> InverseParentFormField { get; set; } = new List<FormField>();

    public virtual ICollection<PageToFormField> PageToFormFields { get; set; } = new List<PageToFormField>();

    public virtual FormField? ParentFormField { get; set; }

    public virtual ICollection<ReportField> ReportFields { get; set; } = new List<ReportField>();

    public virtual ICollection<WorkflowStageTransition> WorkflowStageTransitions { get; set; } = new List<WorkflowStageTransition>();
}
