namespace Viper.Areas.CTS.Models
{
    public class Epa
    {
        public int EpaId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? Order { get; set; }
        public bool Active { get; set; }

        public List<EpaService> Services { get; set; } = new List<EpaService>();

        public Epa()
        {

        }
        public Epa(Viper.Models.CTS.Epa dbEpa)
        {
            EpaId = dbEpa.EpaId;
            Name = dbEpa.Name;
            Description = dbEpa.Description;
            Order = dbEpa.Order;
            Active = dbEpa.Active;
            foreach (var s in dbEpa.Services)
            {
                Services.Add(new EpaService()
                {
                    ServiceId = s.ServiceId,
                    ServiceName = s.ServiceName,
                    ShortName = s.ShortName
                });
            }
        }
    }
}
