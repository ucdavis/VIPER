namespace Viper.Models.IAM
{
    public class FullPerson : CorePerson
    {
        public string? LoginId { get; set; }
        public string? MailId { get; set; }
        public IEnumerable<EmployeeAssociation>? EmployeeAssociations { get; set; }
        public IEnumerable<SisAssociation>? SisAssociations { get; set; }
        public IEnumerable<DirectoryAssociation>? DirectoryAssociations { get; set; }
    }
}
