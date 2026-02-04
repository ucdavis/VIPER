namespace Viper.Models.VIPER;

public partial class Form
{
    public Guid FormId { get; set; }

    public string? FormName { get; set; }

    public string? FriendlyUrl { get; set; }

    public string? Description { get; set; }

    public string? OwnerPermission { get; set; }

    public string? CustomSaveObject { get; set; }

    public virtual ICollection<FormField> FormFields { get; set; } = new List<FormField>();

    public virtual ICollection<FormSubmissionSnapshot> FormSubmissionSnapshots { get; set; } = new List<FormSubmissionSnapshot>();

    public virtual ICollection<FormSubmission> FormSubmissions { get; set; } = new List<FormSubmission>();

    public virtual ICollection<FormVersion> FormVersions { get; set; } = new List<FormVersion>();

    public virtual ICollection<Page> Pages { get; set; } = new List<Page>();

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    public virtual ICollection<WorkflowStage> WorkflowStages { get; set; } = new List<WorkflowStage>();
}
