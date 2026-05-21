namespace Viper.Classes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SearchExcludeAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class SearchNameAttribute : Attribute
    {
        public string? FriendlyName { get; set; }
    }
}
