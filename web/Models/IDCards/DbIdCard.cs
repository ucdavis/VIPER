using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class DbIdCard
{
    public int IdCardId { get; set; }

    public int? IdCardNumber { get; set; }

    public string? IdCardClientId { get; set; }

    public string? IdCardLoginId { get; set; }

    public string? IdCardMailId { get; set; }

    public short? IdCardClientType { get; set; }

    public int? IdCardSvmId { get; set; }

    public string? IdCardCardType { get; set; }

    public string? IdCardSalutation { get; set; }

    public string? IdCardDisplayName { get; set; }

    public string? IdCardLastName { get; set; }

    public string? IdCardCertification { get; set; }

    public string? IdCardLine2 { get; set; }

    public DateTime? IdCardReviewDate { get; set; }

    public string? IdCardApprovedBy { get; set; }

    public DateTime? IdCardAppliedDate { get; set; }

    public string? IdCardAppliedBy { get; set; }

    public DateTime? IdCardFinalReviewDate { get; set; }

    public string? IdCardFinalReviewBy { get; set; }

    public DateTime? IdCardPrintedDate { get; set; }

    public DateTime? IdCardIssueDate { get; set; }

    public DateTime? IdCardNotifyDate { get; set; }

    public string? IdCardCurrentStatus { get; set; }
}
