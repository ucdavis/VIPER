using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Models
{
    public class AuditLog : TblLog
    {
        public string? Role { get; set; }
        public string? Permission { get; set; }
        public string? MemberName { get; set; }
        public string? ModByUserName { get; set; }

    }
}
