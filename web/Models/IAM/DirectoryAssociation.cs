namespace Viper.Models.IAM
{
    public class DirectoryAssociation : IIamData
    {
        public required string IamId { get; set; }
        public string? DeptCode { get; set; }
        public string? DeptOfficialName { get; set; }
        public string? DeptDisplayName { get; set; }
        public string? DeptAbbrev {  get; set; }
        public bool IsUCDHS { get; set; }
        public string? AssocRank { get; set; }
        public DateTime? AssocStartDate { get; set; }
        public DateTime? AssocEndDate { get; set; }
        public string? TitleOfficialName { get; set; }
        public string? TitleDisplayName { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }
        public string? FilterableId
        {
            get { return IamId; }
        }
    }
}
