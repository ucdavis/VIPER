using System.Text.Json.Serialization;
using UsedImplicitly = JetBrains.Annotations.UsedImplicitlyAttribute;
using ImplicitUseTargetFlags = JetBrains.Annotations.ImplicitUseTargetFlags;

namespace Viper.Areas.Students.Models
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class PhotoGalleryViewModel
    {
        public string? ClassLevel { get; set; }
        public required List<StudentPhoto> Students { get; set; }
        public GroupingInfo? GroupInfo { get; set; }
        public CourseInfo? CourseInfo { get; set; }
        public required ExportOptions ExportOptions { get; set; }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class StudentPhoto
    {
        public required string MailId { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string DisplayName { get; set; }
        public required string PhotoUrl { get; set; }
        public string? GroupAssignment { get; set; }
        public string? EighthsGroup { get; set; }
        public string? TwentiethsGroup { get; set; }
        public string? TeamNumber { get; set; }
        public string? V3SpecialtyGroup { get; set; }
        public bool HasPhoto { get; set; }
        public bool IsRossStudent { get; set; }
        public string? ClassLevel { get; set; }

        // Computed properties for consistent display across UI and exports
        [JsonInclude]
        public string FullName => $"{LastName}, {FirstName}";

        // Alternative name format for group exports (FirstName LastName)
        [JsonInclude]
        public string GroupExportName => $"{FirstName} {LastName}";

        [JsonInclude]
        public List<string> SecondaryTextLines
        {
            get
            {
                var lines = new List<string>();
                if (!string.IsNullOrEmpty(GroupAssignment))
                    lines.Add(GroupAssignment);
                if (!string.IsNullOrEmpty(TeamNumber))
                    lines.Add($"Team {TeamNumber}");
                if (!string.IsNullOrEmpty(V3SpecialtyGroup))
                    lines.Add(V3SpecialtyGroup);
                return lines;
            }
        }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class GroupingInfo
    {
        public required string GroupType { get; set; }
        public required string GroupId { get; set; }
        public required List<string> AvailableGroups { get; set; }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class ExportOptions
    {
        public required string Format { get; set; }
        public bool IncludeGroups { get; set; }
        public bool IncludeRossStudents { get; set; }
        public required string Title { get; set; }
        public required string Subtitle { get; set; }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class PhotoExportRequest
    {
        public string? ClassLevel { get; set; }
        public string? GroupType { get; set; }
        public string? GroupId { get; set; }
        public string? TermCode { get; set; }
        public string? Crn { get; set; }
        public required bool IncludeRossStudents { get; set; }
        public string? ExportFormat { get; set; }

        /// <summary>
        /// The on-screen view that triggered the export ("grid" | "list" | "sheet").
        /// Carried into the saved filename so users can tell exports apart when
        /// they have multiple downloads of the same class.
        /// </summary>
        public string? View { get; set; }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class PhotoExportResult
    {
        public required string ExportId { get; set; }
        public required byte[] FileData { get; set; }
        public required string FileName { get; set; }
        public required string ContentType { get; set; }
    }
}
