namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// Class for form data to create or edit a row in the SVM table.
    /// </summary>
    public class SVMUnitDataRequest
    {
        public string Fax { get; set; } = "";
        public string Location { get; set; } = "";
        public string DeanIam { get; set; } = "";
        public string DeanPhone { get; set; } = "";
        public string DeanInterim { get; set; } = "";
        public int DeanUnitPerson { get; set; } = -1;
        public string StaffIam { get; set; } = "";
        public string StaffPhone { get; set; } = "";
        public string StaffInterim { get; set; } = "";
        public int StaffUnitPerson { get; set; } = -1;
    }
}
