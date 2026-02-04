namespace Viper.Classes
{
    public class LoggedInUser
    {
        public int UserId { get; set; }
        public string LoginId { get; set; }
        public string MailId { get; set; }
        public string IamId { get; set; }
        public string MothraId { get; set; }
        public string Token { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
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
