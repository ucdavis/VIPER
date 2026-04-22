namespace Viper.Services
{
    public interface IHtmlSanitizerService
    {
        string Sanitize(string html);
    }
}
