namespace Viper.Areas.Directory.Models
{
    public class UCPathResult
    {
        public string? JobCode { get; set; } = null!;
        public string? JobCodeDescription { get; set; } = null!;
        public string? DepartmentId { get; set; } = null!;
        public string? DepartmentDescription { get; set; } = null!;
        public string? ActionDescription { get; set; } = null!;
        public DateOnly? PositionEffectiveDate { get; set; } = null!;
        public string? ReportsTo { get; set; } = null!;
        public string? ReportsToPosition { get; set; } = null!;
    }
}
