namespace Viper.Areas.Personnel.Models
{
    /*
     * Response shapes for the phone list endpoints.
     *
     * The endpoints used to return the EF entities directly, which put the database model on the
     * wire: every navigation property the query did not populate still serialized, so clients
     * carried fields that were always null (phoneList, phoneListUnit, unit, section) and the API
     * contract moved whenever the schema did. Worse, those nulls were an accident of the current
     * projections rather than a decision - an .Include() added later would have started emitting
     * whole object graphs, PhoneList.MaintainRole among them.
     *
     * These types say what each endpoint returns. They deliberately omit the unpopulated
     * navigation properties and the internal columns no client reads (IsActive, and the
     * modification metadata on rows whose modification dates are served by their own endpoint).
     * PersonnelMapper maps entities to them and names every omission, so adding a column to an
     * entity cannot silently change what the API sends.
     */

    /// <summary>A person from users.Person, as much of one as a phone list needs.</summary>
    public class ViperPersonDto
    {
        public int PersonId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string IamId { get; set; } = string.Empty;
        public bool CurrentEmployee { get; set; }
        public string MailId { get; set; } = string.Empty;
    }

    /// <summary>
    /// One person's phone data, shared by every list they appear on. DirectPhone is blanked by
    /// the service for callers who may not see it, so it is present but empty rather than absent.
    /// </summary>
    public class PhonePersonDto
    {
        public string PersonIam { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? DirectPhone { get; set; }
        public string? Office { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public ViperPersonDto? ViperPerson { get; set; }
        public ViperPersonDto? ViperModPerson { get; set; }
    }

    /// <summary>One person's place on a department phone list.</summary>
    public class PhoneListUnitPersonDto
    {
        public int PhoneListUnitPersonId { get; set; }
        public int PhoneListUnitId { get; set; }
        public string PersonIam { get; set; } = string.Empty;
        public bool ListFirst { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public PhonePersonDto? Person { get; set; }
        public ViperPersonDto? ViperModPerson { get; set; }
    }

    /// <summary>A unit of a department phone list, with the people on it.</summary>
    public class PhoneListUnitDto
    {
        public int PhoneListUnitId { get; set; }
        public int PhoneListId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? SortOrder { get; set; }
        public List<PhoneListUnitPersonDto> PhoneListUnitPersons { get; set; } = [];
    }

    /// <summary>One person's place in an SVM unit, as a leader or as its admin staff.</summary>
    public class SVMUnitPersonDto
    {
        public int UnitPersonId { get; set; }
        public int UnitId { get; set; }
        public string PersonIam { get; set; } = string.Empty;
        public string? Office { get; set; }
        public string? PosType { get; set; }
        public string? Interim { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public PhonePersonDto? Person { get; set; }
        public ViperPersonDto? ViperModPerson { get; set; }
    }

    /// <summary>An SVM unit, with the people on it.</summary>
    public class SVMUnitDto
    {
        public int UnitId { get; set; }
        public int SectionId { get; set; }
        public string? Name { get; set; }
        public string? Abbrv { get; set; }
        public int? SortOrder { get; set; }
        public string? Fax { get; set; }
        public List<SVMUnitPersonDto> UnitPersons { get; set; } = [];
    }

    /// <summary>A heading on the SVM list, and the column labels its units render under.</summary>
    public class SVMSectionDto
    {
        public int SectionId { get; set; }
        public string? Name { get; set; }
        public bool? IncludeAbbrv { get; set; }
        public string? UnitName { get; set; }
        public string? DirectorTitle { get; set; }
        public int? SortOrder { get; set; }
    }

    /// <summary>A frequently called number, which belongs to a place rather than a person.</summary>
    public class SVMFrequentNumberDto
    {
        public int NumberId { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int? SortOrder { get; set; }
    }

    /// <summary>A person picker result: who they are, plus their phone data if they have any.</summary>
    public class AugmentedViperPersonDto
    {
        public int PersonId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string IamId { get; set; } = string.Empty;
        public bool CurrentEmployee { get; set; }
        public string MailId { get; set; } = string.Empty;
        public PhonePersonDto? PhoneData { get; set; }
    }
}
