using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// Class for form data to create or edit a row in the SVM frequently called numbers table.
    /// Lengths mirror the phones.SVMFrequentNumber columns. Emptiness is checked in
    /// PhoneSVMFrequentNumberService so the two rules are not stated in both places.
    /// </summary>
    public class SVMFrequentNumberRequest
    {
        [MaxLength(100, ErrorMessage = "Location must be 100 characters or fewer.")]
        public required string Label { get; set; } = "";

        [MaxLength(25, ErrorMessage = "Phone number must be 25 characters or fewer.")]
        public required string Phone { get; set; } = "";
    }
}
