namespace Viper.Models.IAM
{
    public class SisAssociation
    {
        public required string IamId { get; set; }
        public string? LevelCode { get; set; }
        public string? LevelName { get; set; }
        public string? ClassCode { get; set; }
        public string? ClassName { get; set; }
        public string? CollegeCode { get; set; }
        public string? CollegeName { get; set; }
        public string? AssocRank {  get; set; }
        public string? AssocEndDate { get; set; }
        public string? MajorCode { get; set; }
        public string? MajorName { get; set; }
        public string? FerpaCode { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }
    }
}
