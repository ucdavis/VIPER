namespace Viper.Classes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class SearchExcludeAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class SearchNameAttribute : Attribute
    {
        public string? FriendlyName { get; set; }
    }
}
