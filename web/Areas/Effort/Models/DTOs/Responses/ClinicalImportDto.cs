namespace Viper.Areas.Effort.Models.DTOs.Responses;

public enum ClinicalImportMode
{
    AddNewOnly,      // Only insert, skip existing
    ClearReplace,    // Delete ALL clinical records for term, then import fresh
    Sync             // Update existing, add new, delete missing
}

public class ClinicalImportPreviewDto
{
    public ClinicalImportMode Mode { get; set; }
    public int AddCount { get; set; }
    public int UpdateCount { get; set; }
    public int DeleteCount { get; set; }
    public int SkipCount { get; set; }
    public DateTime PreviewGeneratedAt { get; set; }
    public List<ClinicalAssignmentPreview> Assignments { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public class ClinicalAssignmentPreview
{
    public string Status { get; set; } = "";  // "New", "Update", "Delete", "Skip"
    public int? ExistingRecordId { get; set; }
    public string MothraId { get; set; } = "";
    public string InstructorName { get; set; } = "";
    public string CourseNumber { get; set; } = "";
    public string EffortType { get; set; } = "CLI";
    public int Weeks { get; set; }
    public int? ExistingWeeks { get; set; }
    public string RoleName { get; set; } = "Instructor";
}

public class ClinicalImportProgressEvent
{
    public string Type { get; set; } = "";   // "preparing", "importing", "finalizing", "complete", "error"
    public double Progress { get; set; }      // 0.0 to 1.0
    public string Message { get; set; } = "";
    public string? Detail { get; set; }
    public ClinicalImportResultDto? Result { get; set; }

    public static ClinicalImportProgressEvent Preparing() => new()
    {
        Type = "preparing",
        Progress = 0.1,
        Message = "Preparing clinical import..."
    };

    public static ClinicalImportProgressEvent Importing(int current, int total) => new()
    {
        Type = "importing",
        Progress = 0.1 + (0.8 * (double)current / Math.Max(total, 1)),
        Message = "Importing clinical records...",
        Detail = $"{current}/{total} records processed"
    };

    public static ClinicalImportProgressEvent Finalizing() => new()
    {
        Type = "finalizing",
        Progress = 0.95,
        Message = "Finalizing import..."
    };

    public static ClinicalImportProgressEvent Complete(ClinicalImportResultDto result) => new()
    {
        Type = "complete",
        Progress = 1.0,
        Message = "Import complete!",
        Result = result
    };

    public static ClinicalImportProgressEvent Failed(string error) => new()
    {
        Type = "error",
        Progress = 0,
        Message = error
    };
}

public class ClinicalImportResultDto
{
    public bool Success { get; set; }
    public int RecordsAdded { get; set; }
    public int RecordsUpdated { get; set; }
    public int RecordsDeleted { get; set; }
    public int RecordsSkipped { get; set; }
    public string? ErrorMessage { get; set; }
}
