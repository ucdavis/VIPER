using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class ContentBlockFile
{
    public int ContentBlockFileId { get; set; }

    public int ContentBlockId { get; set; }

    public int FileId { get; set; }
}
