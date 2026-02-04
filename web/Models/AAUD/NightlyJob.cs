namespace Viper.Models.AAUD;

public partial class NightlyJob
{
    public int NightlyJobRecordId { get; set; }

    public string NightlyJobTermCode { get; set; } = null!;

    public bool NightlyJobActive { get; set; }
}
