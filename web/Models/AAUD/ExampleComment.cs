namespace Viper.Models.AAUD
{
    public class ExampleComment
    {
        public int AaudUserId { get; set; }
        public string? Comment { get; set; }
        public AaudUser AaudUser { get; set; } = null!;
    }
}
