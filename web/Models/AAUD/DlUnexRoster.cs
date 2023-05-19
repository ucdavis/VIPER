using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class DlUnexRoster
{
    public string UnexRosterId { get; set; } = null!;

    public string UnexRosterTermCode { get; set; } = null!;

    public string UnexRosterCrn { get; set; } = null!;

    public int UnexRosterRecordId { get; set; }
}
