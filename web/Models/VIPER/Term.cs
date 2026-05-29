namespace Viper.Models.VIPER
{
    public class Term
    {
        /// <summary>
        /// Term code for "(DO NOT USE) Facility Schedule" - should be excluded from term lists.
        /// </summary>
        public const int FacilityScheduleTermCode = 999999;

        public int TermCode { get; set; }
        public int AcademicYear { get; set; }
        public string Description { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string TermType { get; set; } = null!;
        public bool CurrentTerm { get; set; }
        public bool CurrentTermMulti { get; set; }
    }
}
