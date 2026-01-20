namespace Viper.Models.Crest;

/// <summary>
/// Links instructors (by PIDM) to specific course session offerings.
/// Read-only entity for extracting instructor assignments from CREST.
/// </summary>
public class EdutaskOfferPerson
{
    public int EdutaskOfferId { get; set; }
    public int PersonId { get; set; }
}
