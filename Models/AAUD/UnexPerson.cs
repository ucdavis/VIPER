using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class UnexPerson
{
    public int UnexPersonRecordId { get; set; }

    public string? UnexPersonTermCode { get; set; }

    public string UnexPersonUnexId { get; set; } = null!;

    public string? UnexPersonLastName { get; set; }

    public string? UnexPersonFirstName { get; set; }

    public string? UnexPersonMiddleName { get; set; }

    public string? UnexPersonMothraId { get; set; }

    public string? UnexPersonLoginId { get; set; }

    public string? UnexPersonMailId { get; set; }

    public string? UnexPersonSpridenId { get; set; }

    public string? UnexPersonEmployeeId { get; set; }

    public string? UnexPersonPidm { get; set; }

    public bool? UnexPersonIsSvm { get; set; }

    public string? UnexPersonDisplayLastName { get; set; }

    public string? UnexPersonDisplayFirstName { get; set; }

    public string? UnexPersonDisplayMiddleName { get; set; }

    public string? UnexPersonPpsId { get; set; }
}
