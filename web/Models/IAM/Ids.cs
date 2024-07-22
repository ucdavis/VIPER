namespace Viper.Models.IAM
{
    public class Ids : IIamData
    {
        public string? IamId {  get; set; }
        public string? MothraId { get; set; }
        public string? PpsId { get; set; }
        public string? EmployeeId { get; set; }
        public string? StudentId { get; set; }
        public string? BannerPidm {  get; set; }
        public string? ExternalId { get; set; }
        public string? UcnetId { get; set; }
        public string? FilterableId
        {
            get { return IamId; }
        }
    }
}
