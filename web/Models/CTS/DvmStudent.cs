using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Viper.Models.CTS
{
    public class DvmStudent
    {
        public string LastName { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string MiddleName { get; set; } = null!;
        public string LoginId { get; set; } = null!;
        public int Pidm { get; set; }
        public string MailId { get; set; } = null!;
        public string MothraId { get; set; } = null!;
        public string ClassLevel { get; set; } = null!;
        public int TermCode { get; set; }
    }
}
