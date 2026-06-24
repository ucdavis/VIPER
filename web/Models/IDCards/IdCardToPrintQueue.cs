using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class IdCardToPrintQueue
{
    public int IdcardId { get; set; }

    public int PrintQueueId { get; set; }
}
