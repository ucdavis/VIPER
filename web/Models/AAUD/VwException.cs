namespace Viper.Models.AAUD;

public partial class VwException
{
    public string PersonPKey { get; set; } = null!;

    public string PersonTermCode { get; set; } = null!;

    public string PersonLastName { get; set; } = null!;

    public string PersonFirstName { get; set; } = null!;

    public string? PersonRequestedBy { get; set; }

    public string? PersonCreatedBy { get; set; }

    public string? PersonReasonForException { get; set; }

    public DateTime? PersonExceptionCreateDate { get; set; }

    public DateTime? PersonEndDate { get; set; }

    public string? IdsLoginid { get; set; }

    public string? IdsMailid { get; set; }

    public bool FlagsStudent { get; set; }
}
