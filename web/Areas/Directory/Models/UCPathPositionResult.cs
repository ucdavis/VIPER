namespace Viper.Areas.Directory.Models
{
    public class UCPathPositionResult
    {
        public string? JobCode { get; set; }
        public string? JobDescription { get; set; }
        public string? DepartmentId { get; set; }
        public string? DepartmentDescription { get; set; }
        public string? JobStatus { get; set; }
        public string? EmployeeStatus { get; set; }
        public string? JobStatusDescription { get; set; }
        public DateTime? PositionEffectiveDate { get; set; }
        public DateTime? ExpectedEndDate { get; set; }
        public decimal? FTE { get; set; }
        public string? Union { get; set; }
        public string? ReportsToName { get; set; }
        public string? ReportsToPosition { get; set; }
    }
}
