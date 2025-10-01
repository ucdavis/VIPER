using System;

namespace Viper.Areas.Directory.Models
{
    public class KeyResult
    {
        public string? KeyNumber { get; set; }
        public string? CutNumber { get; set; }
        public string? AccessDescription { get; set; }
        public DateTime? IssuedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string? IssuedBy { get; set; }
    }
}
