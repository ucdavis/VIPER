namespace Viper.Models.CTS
{
    public class Service
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ShortName {  get; set; } = string.Empty;
        public virtual ICollection<Rotation> Rotations { get; set; } = new List<Rotation>();
        public virtual ICollection<Encounter> Encounters { get; set; } = new List<Encounter>();
        public virtual ICollection<Epa> Epas { get; set; } = new List<Epa>();
    }
}
