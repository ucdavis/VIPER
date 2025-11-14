using System.Text.Json.Serialization;

namespace Viper.Areas.Students.Models
{
    public class PhotoGalleryViewModel
    {
        public string? ClassLevel { get; set; }
        public List<StudentPhoto> Students { get; set; }
        public GroupingInfo? GroupInfo { get; set; }
        public CourseInfo? CourseInfo { get; set; }
        public ExportOptions ExportOptions { get; set; }
    }

    public class StudentPhoto
    {
        public string MailId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DisplayName { get; set; }
        public string PhotoUrl { get; set; }
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
        public string GroupType { get; set; }
        public string GroupId { get; set; }
        public List<string> AvailableGroups { get; set; }
    }

    public class ExportOptions
    {
        public string Format { get; set; }
        public bool IncludeGroups { get; set; }
        public bool IncludeRossStudents { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
    }

    public class PhotoExportRequest
    {
        public string? ClassLevel { get; set; }
        public string? GroupType { get; set; }
        public string? GroupId { get; set; }
        public string? TermCode { get; set; }
        public string? Crn { get; set; }
        public bool IncludeRossStudents { get; set; }
        public string? ExportFormat { get; set; }
    }

    public class PhotoExportResult
    {
        public string ExportId { get; set; }
        public byte[] FileData { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }
}
