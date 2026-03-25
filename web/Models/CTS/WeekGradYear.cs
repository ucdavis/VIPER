namespace Viper.Models.CTS
{
    public class WeekGradYear
    {
        public int WeekGradYearId { get; set; }
        public int WeekId { get; set; }
        public int GradYear { get; set; }
        public int WeekNum { get; set; }

        public virtual Week Week { get; set; } = new Week();
    }
}
