using System;

namespace Viper.Areas.Directory.Models
{
    public class LoanResult
    {
        public string? AssetName { get; set; }
        public DateTime? LoanDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Comments { get; set; }
    }
}
