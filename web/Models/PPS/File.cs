using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class File
{
    public Guid FileGuid { get; set; }

    public string FilePath { get; set; } = null!;

    public string? Folder { get; set; }

    public string FriendlyName { get; set; } = null!;

    public bool Encrypted { get; set; }

    public string? Key { get; set; }

    public string Description { get; set; } = null!;

    public bool AllowPublicAccess { get; set; }

    public string? OldUrl { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public DateTime? DeletedOn { get; set; }

    public virtual ICollection<ContentBlockToFile> ContentBlockToFiles { get; set; } = new List<ContentBlockToFile>();

    public virtual ICollection<FileToPerson> FileToPeople { get; set; } = new List<FileToPerson>();

    public virtual ICollection<FileToPermission> FileToPermissions { get; set; } = new List<FileToPermission>();
}
