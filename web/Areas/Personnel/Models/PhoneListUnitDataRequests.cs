using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// Class for form data to create or edit a row in unit-specific phone list tables.
    /// Lengths mirror the phones.Person columns these fields are written to, so a value the
    /// browser did not cap is rejected with a named field rather than a SQL truncation error.
    /// </summary>
    public class PhoneListUnitDataRequest
    {
        public required int UnitId { get; set; }

        [MaxLength(100, ErrorMessage = "Office must be 100 characters or fewer.")]
        public string Office { get; set; } = "";

        [MaxLength(10, ErrorMessage = "Employee IAM ID must be 10 characters or fewer.")]
        public required string EmployeeIam { get; set; }

        [MaxLength(25, ErrorMessage = "Public phone must be 25 characters or fewer.")]
        public string Phone { get; set; } = "";

        [MaxLength(25, ErrorMessage = "Direct phone must be 25 characters or fewer.")]
        public string DirectPhone { get; set; } = "";

        public required bool ListFirst { get; set; }
    }
}
