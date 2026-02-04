namespace Viper.Models.AAUD;

public partial class Exceptionflag
{
    public string FlagsPKey { get; set; } = null!;

    public string FlagsTermCode { get; set; } = null!;

    public string FlagsClientid { get; set; } = null!;

    public bool FlagsStudent { get; set; }

    public bool FlagsAcademic { get; set; }

    public bool FlagsStaff { get; set; }

    public bool FlagsTeachingFaculty { get; set; }

    public bool FlagsWosemp { get; set; }

    public bool FlagsConfidential { get; set; }

    public bool FlagsSvmPeople { get; set; }

    public bool FlagsSvmStudent { get; set; }

    public bool? FlagsSvmRossStudent { get; set; }
}
