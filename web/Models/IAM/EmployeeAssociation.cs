namespace Viper.Models.IAM
{
    public class EmployeeAssociation : IIamData
    {
        public required string IamId { get; set; }
        public string? DeptCode { get; set; }
        public string? DeptOfficialName { get; set; }
        public string? DeptDisplayName { get; set; }
        public string? DeptAbbreviation { get; set; }
        public bool IsUCDHS { get; set; }
        public string? BouOrgOId { get; set; }
        public string? AdminDeptCode { get; set; }
        public string? AdminDeptOfficialName { get; set; }
        public string? AdminDeptDisplayName { get; set; }
        public string? AdminDeptAbbrev { get; set; }
        public bool AdminIsUCDHS { get; set; }
        public string? AdminBouOrgOId { get; set; }
        public string? ApptDeptCode { get; set; }
        public string? ApptDeptOfficialName { get; set; }
        public string? ApptDeptDisplayName { get; set; }
        public string? ApptDeptAbbrev { get; set; }
        public bool ApptIsUCDHS { get; set; }
        public string? ApptBouOrgOId { get; set; }
        public string? AssocRank { get; set; }
        public DateTime? AssocStartDate { get; set; }
        public DateTime? AssocEndDate { get; set; }
        public string? TitleCode { get; set; }
        public string? TitleOfficialName { get; set; }
        public string? TitleDisplayName { get; set; }
        public string? PositionTypeCode { get; set; }
        public string? PositionType { get; set; }
        public string? PercentFullTime { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }
        public string? EmplClass { get; set; }
        public string? EmplClassDesc { get; set; }
        public string? FilterableId
        {
            get { return IamId; }
        }
    }
}
