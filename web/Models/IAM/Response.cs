namespace Viper.Models.IAM
{
    public class Response<T>
    {
        public IEnumerable<T>? Data { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
