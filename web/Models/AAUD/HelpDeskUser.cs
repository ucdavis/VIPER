using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class HelpDeskUser
{
    public string? PersonDisplayFullName { get; set; }

    public string? IdsMailid { get; set; }

    public string PersonLastName { get; set; } = null!;

    public string PersonFirstName { get; set; } = null!;
}
