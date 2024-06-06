namespace Viper.Models.CTS
{
    public class Rotation
    {
        public int RotId { get; set; }
        public int ServiceId {  get; set; }
        public string Name { get; set; } = string.Empty;
        public string Abbreviation { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string CourseNumber {  get; set; } = string.Empty;
        public virtual Service Service { get; set; } = null!;
    }
}
