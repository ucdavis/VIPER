namespace Viper.Models.ClinicalScheduler
{
    public class Person
    {
        public string IdsMothraId { get; set; } = null!;
        public string PersonDisplayFullName { get; set; } = null!;
        public string PersonDisplayLastName { get; set; } = null!;
        public string PersonDisplayFirstName { get; set; } = null!;
        public string? IdsMailId { get; set; } = null;
    }
}