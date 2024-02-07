namespace Viper.Areas.Directory.Models
{
    public class IndividualSearchResultWithIDs: IndividualSearchResult
    {
        public string? SpridenId { get; set; } = string.Empty;

        public string? Pidm { get; set; } = string.Empty;

        public string? EmployeeId { get; set; } = string.Empty;

        public int? VmacsId { get; set; } = null!;

        public string? VmcasId { get; set; } = string.Empty;

        public string? UnexId { get; set; } = string.Empty;

        public int? MivId { get; set; } = null!;
    }
}
