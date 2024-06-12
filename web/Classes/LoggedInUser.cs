namespace Viper.Classes
{
    public class LoggedInUser
    {
        public int UserId { get; set; }
        public string LoginId { get; set; } = null!;
        public string MailId { get; set; } = null!;
        public string IamId { get; set; } = null!;
        public string MothraId { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public bool Emulating { get; set; } = false;

        public LoggedInUser()
        {
            UserId = 0;
            LoginId = "";
            MailId = "";
            IamId = "";
            MothraId = "";
            Token = "";
            FirstName = "";
            LastName = "";
        }
    }

}
