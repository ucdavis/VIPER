using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class PrintQueue
{
    public int Id { get; set; }

    public int IdCardId { get; set; }

    public string Name { get; set; } = null!;

    public string Line2 { get; set; } = null!;

    public int? CardNumber { get; set; }

    public string CardColor { get; set; } = null!;

    public bool PhotoExists { get; set; }

    public bool Notify { get; set; }

    public virtual IdCard IdCard { get; set; } = null!;
}
