using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// Class for form data to create or edit a row in the SVM table.
    /// Lengths mirror the phones.SVMUnit, phones.SVMUnitPerson and phones.Person columns these
    /// fields are written to, so a value the browser did not cap is rejected with a named field
    /// rather than a SQL truncation error.
    /// </summary>
    public class SVMUnitDataRequest
    {
        [MaxLength(25, ErrorMessage = "Fax must be 25 characters or fewer.")]
        public string Fax { get; set; } = "";

        [MaxLength(50, ErrorMessage = "Location must be 50 characters or fewer.")]
        public string Location { get; set; } = "";

        [MaxLength(10, ErrorMessage = "Dean/Director IAM ID must be 10 characters or fewer.")]
        public string DeanIam { get; set; } = "";

        [MaxLength(25, ErrorMessage = "Dean/Director phone must be 25 characters or fewer.")]
        public string DeanPhone { get; set; } = "";

        [MaxLength(10, ErrorMessage = "Dean/Director interim/vice status must be 10 characters or fewer.")]
        public string DeanInterim { get; set; } = "";

        public int DeanUnitPerson { get; set; } = -1;

        [MaxLength(10, ErrorMessage = "Admin staff IAM ID must be 10 characters or fewer.")]
        public string StaffIam { get; set; } = "";

        [MaxLength(25, ErrorMessage = "Admin staff phone must be 25 characters or fewer.")]
        public string StaffPhone { get; set; } = "";

        [MaxLength(10, ErrorMessage = "Admin staff interim/vice status must be 10 characters or fewer.")]
        public string StaffInterim { get; set; } = "";

        public int StaffUnitPerson { get; set; } = -1;
    }
}
