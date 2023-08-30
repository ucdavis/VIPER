namespace Viper.Areas.RAPS.Models.Uinform
{
    public class UinformResponse<T>
    {
        public bool Success { get; set; }
        public UinformError? Error { get; set; }
        public T? ResponseObject { get; set; }
    }
}
