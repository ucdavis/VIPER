namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// Class for form data to create or edit a row in unit-specific phone list tables.
    /// </summary>
    public class PhoneListUnitDataRequest
    {
        public required int UnitId { get; set; }
        public string Office { get; set; } = "";
        public required string EmployeeIam { get; set; }
        public string Phone { get; set; } = "";
        public string DirectPhone { get; set; } = "";
        public bool ListFirst { get; set; } = false;
    }
}
