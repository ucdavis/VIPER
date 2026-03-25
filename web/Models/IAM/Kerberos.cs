namespace Viper.Models.IAM
{
    public class Kerberos : IIamData
    {
        public required string IamId { get; set; }
        public string? UserId { get; set; }
        public string? Uuid { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? ClaimDate { get; set; }
        public DateTime? ExpireDate { get; set; }
        public string? FilterableId
        {
            get { return IamId; }
        }
    }
}
