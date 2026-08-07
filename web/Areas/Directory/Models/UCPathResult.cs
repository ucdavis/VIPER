namespace Viper.Areas.Directory.Models
{
    public class UCPathResult
    {
        public string? JobCode { get; set; }
        public string? JobCodeDescription { get; set; }
        public string? DepartmentId { get; set; }
        public string? DepartmentDescription { get; set; }
        public string? ActionDescription { get; set; }
        public DateOnly? PositionEffectiveDate { get; set; }
        public string? ReportsTo { get; set; }
        public string? ReportsToPosition { get; set; }
    }
}

