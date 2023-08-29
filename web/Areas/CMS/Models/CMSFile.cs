using Viper.Models.VIPER;

namespace Viper.Areas.CMS.Models
{
    public partial class CMSFile : Viper.Models.VIPER.File
    {
        public string FriendlyURL;

        public string URL;

        public CMSFileMetaData MetaData;

        public CMSFile()
        {
            FriendlyURL = Data.CMS.GetFriendlyURL(FriendlyName, AllowPublicAccess);
            URL = Data.CMS.GetURL(FileGuid.ToString(), AllowPublicAccess);
            MetaData = new CMSFileMetaData
            {
                Folder = this.Folder,
                Encrypted = this.Encrypted,
                Public = this.AllowPublicAccess,
                Modified = this.ModifiedOn.ToString("yyyy-mm-dd hh:nn:ss"),
                ModifiedBy = this.ModifiedBy
            };
        }
        public CMSFile(Viper.Models.VIPER.File file)
        {
            this.AllowPublicAccess = file.AllowPublicAccess;
            this.ContentBlockToFiles = file.ContentBlockToFiles;
            this.DeletedOn = file.DeletedOn;
            this.Description = file.Description;
            this.Encrypted = file.Encrypted;
            this.FileGuid = file.FileGuid;
            this.FilePath = file.FilePath;
            this.FileToPeople = file.FileToPeople;
            this.FileToPermissions = file.FileToPermissions;
            this.Folder = file.Folder;
            this.FriendlyName = file.FriendlyName;
            this.Key = file.Key;
            this.ModifiedBy = file.ModifiedBy;
            this.ModifiedOn = file.ModifiedOn;
            this.OldUrl = file.OldUrl;

            FriendlyURL = Data.CMS.GetFriendlyURL(FriendlyName, AllowPublicAccess);
            URL = Data.CMS.GetURL(FileGuid.ToString(), AllowPublicAccess);
            MetaData = new CMSFileMetaData
            {
                Folder = this.Folder,
                Encrypted = this.Encrypted,
                Public = this.AllowPublicAccess,
                Modified = this.ModifiedOn.ToString("yyyy-mm-dd hh:nn:ss"),
                ModifiedBy = this.ModifiedBy
            };
        }

    }
}
