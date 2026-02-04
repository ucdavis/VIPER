namespace Viper.Models.Courses;

public partial class DlRoster
{
    public string RosterPkey { get; set; } = null!;

    public string? RosterTermCode { get; set; }

    public string? RosterCrn { get; set; }

    public string? RosterPidm { get; set; }

    public string? RosterLevelCode { get; set; }

    public string? RosterEnrollStatus { get; set; }

    public string? RosterGradeMode { get; set; }

    public double? RosterUnit { get; set; }
}
