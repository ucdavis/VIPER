namespace Viper.Models.CTS
{
    public class Week
    {
        public int WeekId { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public bool ExtendedRotation { get; set; }
        public int TermCode { get; set; }
        public bool StartWeek { get; set; }
        public virtual ICollection<WeekGradYear> WeekGradYears { get; set; } = new List<WeekGradYear>();
    }
}
