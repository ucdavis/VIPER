namespace Viper.Models.CTS
{
    public class StudentEpa
    {
        public int StudentEpaId { get; set; }
        public int EpaId { get; set; }
        public int LevelId { get; set; }
        public int EncounterId { get; set; }

        public virtual Epa Epa { get; set; } = null!;
        public virtual Level Level { get; set; } = null!;
        public virtual Encounter Encounter { get; set; } = null!;
    }
}
