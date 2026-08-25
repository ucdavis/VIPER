namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// Class for form data to create or edit a row in the SVM frequently called numbers table.
    /// </summary>
    public class SVMFrequentNumberRequest
    {
        public required string Label { get; set; } = "";
        public required string Phone { get; set; } = "";
    }
}
