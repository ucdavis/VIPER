namespace Viper.Models.IAM
{
    public class MothraEmail
    {
        public required string Email { get; set; }
        public string? KerberosId { get; set; }
        public string? Eid { get; set; }
    }
}
