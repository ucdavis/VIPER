using System.Text.Json.Serialization;

namespace Viper.Areas.Students.Models
{
    public class PhotoGalleryViewModel
    {
        public string? ClassLevel { get; set; }
        public required List<StudentPhoto> Students { get; set; }
        public GroupingInfo? GroupInfo { get; set; }
        public CourseInfo? CourseInfo { get; set; }
        public required ExportOptions ExportOptions { get; set; }
    }

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

    public class GroupingInfo
    {
        public required string GroupType { get; set; }
        public required string GroupId { get; set; }
        public required List<string> AvailableGroups { get; set; }
    }

    public class ExportOptions
    {
        public required string Format { get; set; }
        public bool IncludeGroups { get; set; }
        public bool IncludeRossStudents { get; set; }
        public required string Title { get; set; }
        public required string Subtitle { get; set; }
    }

    public class PhotoExportRequest
    {
        public string? ClassLevel { get; set; }
        public string? GroupType { get; set; }
        public string? GroupId { get; set; }
        public string? TermCode { get; set; }
        public string? Crn { get; set; }
        public required bool IncludeRossStudents { get; set; }
        public string? ExportFormat { get; set; }
    }

    public class PhotoExportResult
    {
        public required string ExportId { get; set; }
        public required byte[] FileData { get; set; }
        public required string FileName { get; set; }
        public required string ContentType { get; set; }
    }
}
