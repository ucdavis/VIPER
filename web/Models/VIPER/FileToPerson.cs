namespace Viper.Models.VIPER;

public partial class FileToPerson
{
    public int FileToPersonId { get; set; }

    public Guid FileGuid { get; set; }

    public string IamId { get; set; } = null!;

    public virtual File File { get; set; } = null!;
}
