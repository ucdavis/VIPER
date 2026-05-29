using Viper.Models.CTS;

namespace Viper.Areas.CTS.Models
{
    public class Session
    {
        public int SessionId { get; set; }
        public string? SessionType { get; set; }
        public string Title { get; set; } = null!;
        public int? TypeOrder { get; set; }
        public int? PaceOrder { get; set; }

        public List<Offering> Offerings { get; set; } = new();

        public Session() { }
        public Session(CourseSessionOffering cso)
        {
            SessionId = cso.SessionId;
            SessionType = cso.SessionType;
            Title = cso.Title;
            TypeOrder = cso.TypeOrder;
            PaceOrder = cso.PaceOrder;
        }

    }
}
