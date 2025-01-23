namespace Viper.Models.CTS
{
    public class ServiceChief
    {
        public int ServiceChiefId { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string MothraId { get; set; } = string.Empty;
        public int PersonId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}
