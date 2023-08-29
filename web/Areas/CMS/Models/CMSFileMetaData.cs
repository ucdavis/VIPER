namespace Viper.Areas.CMS.Models
{
    public class CMSFileMetaData
    {
        public string? Folder { get; set; }
        public bool? Encrypted { get; set; }
        public bool? Public { get; set; }
        public string? Modified { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
