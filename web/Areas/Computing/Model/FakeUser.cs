namespace Viper.Areas.Computing.Model
{
    public class FakeUser
    {
        public string PKey { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string ClientId { get; set; } = null!;
        public string? LoginId { get; set; }
        public string? MothraId { get; set; }
        public string? MailId { get; set; }
        public string? Pidm { get; set; }
        public string? IamId { get; set; }
        public bool IsProtected { get; set; } = false;
    }
}
