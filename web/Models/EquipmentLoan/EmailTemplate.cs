using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class EmailTemplate
{
    public int TemplateId { get; set; }

    public string TemplateName { get; set; } = null!;

    public string? TemplateText1 { get; set; }

    public string? TemplateText2 { get; set; }

    public string? TemplateAudience { get; set; }

    public int? TemplateAutoRemindDays { get; set; }
}
