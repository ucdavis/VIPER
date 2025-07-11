using Viper.Models.EquipmentLoan;

namespace Viper.Areas.Directory.Models
{
    public class LoanResult
    {
        public string? Asset { get; set; } = null!;
        public DateOnly? Loaned { get; set; } = null!;
        public DateOnly? Due { get; set; } = null!;
        public string? Comments { get; set; } = null!;
    }
}
