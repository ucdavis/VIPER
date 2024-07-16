namespace Viper.Models.IAM
{
    public class CorePerson
    {
        public required string IamId { get; set; }
        public string? MothraId { get; set; }
        public string? PpsId { get; set; }
        public string? EmployeeId { get; set; }
        public string? StudentId { get; set; }
        public string? BannerPIdM { get; set; }
        public string? ExternalId { get; set; }
        public required string OFirstName { get; set; }
        public string? OMiddleName { get; set; }
        public required string OLastName { get; set; }
        public string? OSuffix { get; set; }
        public required string OFullName { get; set; }
        public string? DFirstName { get; set; }
        public string? DMiddleName { get; set; }
        public string? DLastName { get; set; }
        public string? DSuffix { get; set; }
        public string? DFullName { get; set; }
        public bool IsEmployee { get; set; }
        public bool IsHSEmployee { get; set; }
        public bool IsFaculty { get; set; }
        public bool IsStudent { get; set; }
        public bool IsStaff { get; set; }
        public bool IsExternal { get; set; }
        public string? PrivacyCode { get; set; }
        public DateTime? ModifyDate { get; set; }
    }
}
