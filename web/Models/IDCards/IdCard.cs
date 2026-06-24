using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class IdCard
{
    public int IdCardId { get; set; }

    public int? IdCardNumber { get; set; }

    /// <summary>
    /// Mothra ID
    /// </summary>
    public string? IdCardClientId { get; set; }

    public string? IdCardLoginId { get; set; }

    public string? IdCardMailId { get; set; }

    /// <summary>
    /// 1 = SVM Faculty / 2 = Staff (excluding VMDO / VMTH) / 3 = DVM student / 4 = MPVM Student / 5 = Outside Grad Student / 6 = Epi Grad Student / 7 = VMDO / 8 = VMTH / 9 = Resident / 10 = Intern / 11 = Veterinarian / 13 = Imm Grad Student / 14 = Comp Path Grad student
    /// </summary>
    public short? IdCardClientType { get; set; }

    /// <summary>
    /// FK to dvtSvmUnit if appropriate
    /// </summary>
    public int? IdCardSvmId { get; set; }

    public string? IdCardCardType { get; set; }

    public string? IdCardSalutation { get; set; }

    public string? IdCardDisplayName { get; set; }

    public string? IdCardLastName { get; set; }

    public string? IdCardCertification { get; set; }

    public string? IdCardLine2 { get; set; }

    public DateTime? IdCardAppliedDate { get; set; }

    public string? IdCardAppliedBy { get; set; }

    public DateTime? IdCardFinalReviewDate { get; set; }

    public string? IdCardFinalReviewBy { get; set; }

    public DateTime? IdCardPrintedDate { get; set; }

    public DateTime? IdCardIssueDate { get; set; }

    /// <summary>
    /// Date email sent to notify applicant ready to pickup
    /// </summary>
    public DateTime? IdCardNotifyDate { get; set; }

    /// <summary>
    /// A=Active, L=Lost, V=Void, S=Stolen, WI=For Issuance, WN: For Notification
    /// </summary>
    public string? IdCardCurrentStatus { get; set; }

    public string? IdCardAccessLevel { get; set; }

    public string? IdcardExternalKey { get; set; }

    public string? IdcardExternalApprover { get; set; }

    public string? IdcardDeactivatedReason { get; set; }

    public string? IdcardSystem { get; set; }

    public DateTime? IdcardDeactivatedDate { get; set; }

    public string? IdcardDeactivatedBy { get; set; }

    public string? IdCardSpecialty { get; set; }

    public int? IdcardBadgeKey { get; set; }

    public virtual ICollection<PrintQueue> PrintQueues { get; set; } = new List<PrintQueue>();
}
