namespace Viper.Models.IAM
{
    /// <summary>
    /// Interface for all IAM data objects. Allows objects to have a filterable ID for when we want unique records.
    /// </summary>
    public interface IIamData
    {
        public string? FilterableId { get; }
    }
}
