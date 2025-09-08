using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class VwLatestIdcard
{
    public string? IdCardClientId { get; set; }

    public string? IdCardLoginId { get; set; }

    public string? IdCardMailId { get; set; }

    public short? IdCardClientType { get; set; }

    public int? IdCardNumber { get; set; }

    public string? IdCardLastName { get; set; }

    public string? IdCardDisplayName { get; set; }

    public string? IdCardLine2 { get; set; }

    public string? IdCardSpecialty { get; set; }

    public string? IdCardCurrentStatus { get; set; }

    public DateTime? IdCardFinalReviewDate { get; set; }

    public string? IdCardFinalReviewBy { get; set; }
}
