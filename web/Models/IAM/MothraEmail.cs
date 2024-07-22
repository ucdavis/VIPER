namespace Viper.Models.IAM
{
    public class MothraEmail : IIamData
    {
        public required string Email { get; set; }
        public string? KerberosId { get; set; }
        public string? Eid { get; set; }

        public string? FilterableId
        {
            get { return Email; }
        }
    }
}
