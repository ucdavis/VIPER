namespace Viper.Areas.Research.Models
{
    public class PgmTableSummary
    {
        public string AwardNumber { get; set; } = string.Empty;
        public string ProjectNumber { get; set; } = string.Empty;
        public float ProjectExpenses { get; set; }
        public float? Sy { get; set; }
    }
}
