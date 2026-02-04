namespace Viper.Models.CTS;

public partial class Patient
{
    public int PatientId { get; set; }

    public int? PatientNumber { get; set; }

    public string PatientName { get; set; } = null!;

    public string Gender { get; set; } = null!;

    public string Species { get; set; } = null!;
    public virtual Encounter Encounter { get; set; } = null!;
}
