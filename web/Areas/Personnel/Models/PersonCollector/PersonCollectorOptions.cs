namespace Viper.Areas.Personnel.Models.PersonCollector;

/// <summary>
/// One checkbox on the Person Collector form. The browser only ever sends the <see cref="Key"/>;
/// the codes it stands for stay on the server, so no client value reaches the procedures.
/// </summary>
public sealed record PersonCollectorOption(string Key, string Label, IReadOnlyList<string> Codes);

/// <summary>
/// The Person Collector's choices, copied from the legacy form. Department codes are UCPath
/// department IDs; faculty codes are job groups; the veterinarian codes are title codes.
/// </summary>
public static class PersonCollectorOptions
{
    public static readonly IReadOnlyList<PersonCollectorOption> Departments =
    [
        new("VMDO", "VMDO", ["072000", "072001"]),
        new("VMTH", "VMTH", ["072100", "072105"]),
        new("APC", "APC", ["072037"]),
        new("PHR", "PHR", ["072067"]),
        new("PMI", "PMI", ["072057"]),
        new("VMB", "VMB", ["072047"]),
        new("VME", "VME", ["072030"]),
        new("VSR", "VSR", ["072035"]),
        new("CAHFS", "CAHFS", ["072200", "072140"]),
        new("CCM", "CCM/CIID", ["072090"]),
        new("OHI", "OHI", ["072073"]),
        new("VGL", "VGL", ["072120"]),
        new("WHC", "WHC", ["072072"]),
        new("WIFSS", "WIFSS", ["072065"]),
    ];

    public static readonly IReadOnlyList<PersonCollectorOption> SenateGroups =
    [
        new("CLINICAL", "Professors of Clinical ____", ["317"]),
        new("PROFESSOR", "Professors", ["010", "011", "114"]),
        new("RESIDENCE", "Professors in Residence", ["311"]),
        new("DEAN", "Deans/Admin", ["S21", "S56"]),
    ];

    public static readonly IReadOnlyList<PersonCollectorOption> FederationGroups =
    [
        new("HSCLINICAL", "HS Clinical Professors", ["341"]),
        new("LECTURER", "Lecturers", ["211", "221", "210", "225"]),
        new("ADJUNCT", "Adjunct Professors", ["335"]),
        new("PROJECTSCIENTIST", "Project Scientist", ["581"]),
        new("RESEARCH", "Prof Research", ["541"]),
        new("COOPEXTENSION", "Specialist in Coop Extension", ["729"]),
        new("SPECIALIST", "Specialist", ["551", "553", "557"]),
    ];

    /// <summary>Veterinary student classes (Banner class codes).</summary>
    public static readonly IReadOnlyList<PersonCollectorOption> StudentClasses =
    [
        new("V1", "V1", ["V1"]),
        new("V2", "V2", ["V2"]),
        new("V3", "V3", ["V3"]),
        new("V4", "V4", ["V4"]),
    ];

    /// <summary>The staff veterinarian title codes (the legacy procedure calls them job groups).</summary>
    public static readonly IReadOnlyList<string> VeterinarianTitleCodes = ["9533", "9532", "0503"];

    /// <summary>The degree code for MPVM students.</summary>
    public const string MpvmDegree = "MPVM";
}
