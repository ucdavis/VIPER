using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Viper.Models.IDCards;

namespace Viper.Classes.SQLContext;

public partial class IDCardsContext : DbContext
{
    public IDCardsContext(DbContextOptions<IDCardsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccessExpiration> AccessExpirations { get; set; }

    public virtual DbSet<AccessLevel> AccessLevels { get; set; }

    public virtual DbSet<Audit> Audits { get; set; }

    public virtual DbSet<BulkLoadResult> BulkLoadResults { get; set; }

    public virtual DbSet<DbIdCard> DbIdCards { get; set; }

    public virtual DbSet<Defuncted> Defuncteds { get; set; }

    public virtual DbSet<DvtApprover> DvtApprovers { get; set; }

    public virtual DbSet<DvtCardStatus> DvtCardStatuses { get; set; }

    public virtual DbSet<DvtClient> DvtClients { get; set; }

    public virtual DbSet<DvtOverseer> DvtOverseers { get; set; }

    public virtual DbSet<DvtReason> DvtReasons { get; set; }

    public virtual DbSet<DvtSpecialApprover> DvtSpecialApprovers { get; set; }

    public virtual DbSet<DvtSpecialty> DvtSpecialties { get; set; }

    public virtual DbSet<DvtSvmUnit> DvtSvmUnits { get; set; }

    public virtual DbSet<EcoTimeBadgeExclusion> EcoTimeBadgeExclusions { get; set; }

    public virtual DbSet<ExtVisit> ExtVisits { get; set; }

    public virtual DbSet<IdCard> IdCards { get; set; }

    public virtual DbSet<IdCardToPrintQueue> IdCardToPrintQueues { get; set; }

    public virtual DbSet<IgnoreList> IgnoreLists { get; set; }

    public virtual DbSet<LenelBadge> LenelBadges { get; set; }

    public virtual DbSet<PhotoExport> PhotoExports { get; set; }

    public virtual DbSet<PrintQueue> PrintQueues { get; set; }

    public virtual DbSet<VwApproverMothraId> VwApproverMothraIds { get; set; }

    public virtual DbSet<VwDelimitedSpecialApprover> VwDelimitedSpecialApprovers { get; set; }

    public virtual DbSet<VwLatestIdcard> VwLatestIdcards { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccessExpiration>(entity =>
        {
            entity.HasKey(e => e.ExpiringId);

            entity.ToTable("accessExpiration");

            entity.HasIndex(e => new { e.IamId, e.AccessLevelId }, "UX_accessExpiration_iam_accessLevel").IsUnique();

            entity.Property(e => e.ExpiringId).HasColumnName("expiringId");
            entity.Property(e => e.AccessLevelId).HasColumnName("accessLevelId");
            entity.Property(e => e.ConfirmedRemoval).HasColumnName("confirmedRemoval");
            entity.Property(e => e.ExpirationDate)
                .HasColumnType("datetime")
                .HasColumnName("expirationDate");
            entity.Property(e => e.ExtendedTo)
                .HasColumnType("datetime")
                .HasColumnName("extendedTo");
            entity.Property(e => e.IamId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("iamId");
            entity.Property(e => e.ModBy)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("modBy");
            entity.Property(e => e.ModTime)
                .HasColumnType("datetime")
                .HasColumnName("modTime");
            entity.Property(e => e.Processed).HasColumnName("processed");
        });

        modelBuilder.Entity<AccessLevel>(entity =>
        {
            entity.ToTable("accessLevel");

            entity.Property(e => e.AccessLevelId).HasColumnName("accessLevelId");
            entity.Property(e => e.Assignable).HasColumnName("assignable");
            entity.Property(e => e.Autoassignable).HasColumnName("autoassignable");
            entity.Property(e => e.Criteria)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("criteria");
            entity.Property(e => e.Description)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.LenelId).HasColumnName("lenelId");
            entity.Property(e => e.LenelName)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("lenelName");
            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Audit>(entity =>
        {
            entity.ToTable("audit");

            entity.Property(e => e.AuditId).HasColumnName("audit_id");
            entity.Property(e => e.AccessLevelId).HasColumnName("accessLevelId");
            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("action");
            entity.Property(e => e.Description)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.IamId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("iamId");
            entity.Property(e => e.IdcardId).HasColumnName("idcard_id");
            entity.Property(e => e.IdcardLoginId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("idcard_loginID");
            entity.Property(e => e.IdcardNumber).HasColumnName("idcard_number");
            entity.Property(e => e.MothraId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("mothraID");
            entity.Property(e => e.PqId).HasColumnName("pq_id");
            entity.Property(e => e.Tentative)
                .HasDefaultValueSql("((0))")
                .HasColumnName("tentative");
            entity.Property(e => e.Timestamp)
                .HasColumnType("datetime")
                .HasColumnName("timestamp");
        });

        modelBuilder.Entity<BulkLoadResult>(entity =>
        {
            entity.HasKey(e => e.BulkResultId);

            entity.ToTable("bulkLoadResult");

            entity.HasIndex(e => e.BulkResultId, "bulkResult_ID");

            entity.HasIndex(e => e.BulkResultMothraId, "bulkResult_mothraID");

            entity.Property(e => e.BulkResultId).HasColumnName("bulkResult_ID");
            entity.Property(e => e.BulkResultDisplayName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("bulkResult_displayName");
            entity.Property(e => e.BulkResultLastName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("bulkResult_lastName");
            entity.Property(e => e.BulkResultMothraId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("bulkResult_mothraID");
            entity.Property(e => e.BulkResultOk).HasColumnName("bulkResult_ok");
        });

        modelBuilder.Entity<DbIdCard>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("DB_idCard");

            entity.Property(e => e.IdCardAppliedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_AppliedBy");
            entity.Property(e => e.IdCardAppliedDate)
                .HasColumnType("smalldatetime")
                .HasColumnName("idCard_AppliedDate");
            entity.Property(e => e.IdCardApprovedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_ApprovedBy");
            entity.Property(e => e.IdCardCardType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("idCard_CardType");
            entity.Property(e => e.IdCardCertification)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("idCard_Certification");
            entity.Property(e => e.IdCardClientId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("idCard_ClientId");
            entity.Property(e => e.IdCardClientType).HasColumnName("idCard_ClientType");
            entity.Property(e => e.IdCardCurrentStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_currentStatus");
            entity.Property(e => e.IdCardDisplayName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_DisplayName");
            entity.Property(e => e.IdCardFinalReviewBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_finalReviewBy");
            entity.Property(e => e.IdCardFinalReviewDate)
                .HasColumnType("smalldatetime")
                .HasColumnName("idCard_finalReviewDate");
            entity.Property(e => e.IdCardId).HasColumnName("idCard_Id");
            entity.Property(e => e.IdCardIssueDate)
                .HasColumnType("smalldatetime")
                .HasColumnName("idCard_IssueDate");
            entity.Property(e => e.IdCardLastName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_LastName");
            entity.Property(e => e.IdCardLine2)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("idCard_Line2");
            entity.Property(e => e.IdCardLoginId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_loginId");
            entity.Property(e => e.IdCardMailId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_mailId");
            entity.Property(e => e.IdCardNotifyDate)
                .HasColumnType("smalldatetime")
                .HasColumnName("idCard_notifyDate");
            entity.Property(e => e.IdCardNumber).HasColumnName("idCard_Number");
            entity.Property(e => e.IdCardPrintedDate)
                .HasColumnType("smalldatetime")
                .HasColumnName("idCard_PrintedDate");
            entity.Property(e => e.IdCardReviewDate)
                .HasColumnType("smalldatetime")
                .HasColumnName("idCard_ReviewDate");
            entity.Property(e => e.IdCardSalutation)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("idCard_Salutation");
            entity.Property(e => e.IdCardSvmId).HasColumnName("idCard_svm_id");
        });

        modelBuilder.Entity<Defuncted>(entity =>
        {
            entity.HasKey(e => e.IdCardId);

            entity.ToTable("Defuncted");

            entity.HasIndex(e => e.IdCardLoginId, "IX_LoginId");

            entity.HasIndex(e => e.IdCardNumber, "IX_idCard_Number");

            entity.HasIndex(e => new { e.IdCardLastName, e.IdCardDisplayName }, "lastName_displayName");

            entity.Property(e => e.IdCardId).HasColumnName("idCard_Id");
            entity.Property(e => e.IdCardAppliedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_AppliedBy");
            entity.Property(e => e.IdCardAppliedDate)
                .HasColumnType("smalldatetime")
                .HasColumnName("idCard_AppliedDate");
            entity.Property(e => e.IdCardCardType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("idCard_CardType");
            entity.Property(e => e.IdCardCertification)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("idCard_Certification");
            entity.Property(e => e.IdCardClientId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("idCard_ClientId");
            entity.Property(e => e.IdCardClientType).HasColumnName("idCard_ClientType");
            entity.Property(e => e.IdCardCurrentStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("k = adminDeleted")
                .HasColumnName("idCard_currentStatus");
            entity.Property(e => e.IdCardDefunctBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_defunctBy");
            entity.Property(e => e.IdCardDefunctDate)
                .HasColumnType("datetime")
                .HasColumnName("idCard_defunctDate");
            entity.Property(e => e.IdCardDisplayName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_DisplayName");
            entity.Property(e => e.IdCardFinalReviewBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_finalReviewBy");
            entity.Property(e => e.IdCardFinalReviewDate)
                .HasColumnType("smalldatetime")
                .HasColumnName("idCard_finalReviewDate");
            entity.Property(e => e.IdCardIssueDate)
                .HasColumnType("smalldatetime")
                .HasColumnName("idCard_IssueDate");
            entity.Property(e => e.IdCardLastName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_LastName");
            entity.Property(e => e.IdCardLine2)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("idCard_Line2");
            entity.Property(e => e.IdCardLoginId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_loginId");
            entity.Property(e => e.IdCardMailId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_mailId");
            entity.Property(e => e.IdCardNotifyDate)
                .HasColumnType("smalldatetime")
                .HasColumnName("idCard_notifyDate");
            entity.Property(e => e.IdCardNumber).HasColumnName("idCard_Number");
            entity.Property(e => e.IdCardPrintedDate)
                .HasColumnType("smalldatetime")
                .HasColumnName("idCard_PrintedDate");
            entity.Property(e => e.IdCardSalutation)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("idCard_Salutation");
            entity.Property(e => e.IdCardSvmId).HasColumnName("idCard_svm_id");
        });

        modelBuilder.Entity<DvtApprover>(entity =>
        {
            entity.ToTable("dvtApprover");

            entity.HasIndex(e => e.DvtApproverUnitCode, "unitCode");

            entity.Property(e => e.DvtApproverId).HasColumnName("dvtApprover_ID");
            entity.Property(e => e.DvtApproverMailId)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("dvtApprover_mailID");
            entity.Property(e => e.DvtApproverName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("dvtApprover_name");
            entity.Property(e => e.DvtApproverUnitCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("dvtApprover_unitCode");
            entity.Property(e => e.DvtApproverUnitName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("dvtApprover_unitName");
        });

        modelBuilder.Entity<DvtCardStatus>(entity =>
        {
            entity.HasKey(e => e.DvtStatusCode);

            entity.ToTable("dvtCardStatus");

            entity.HasIndex(e => e.DvtStatusDesc, "Description");

            entity.HasIndex(e => e.DvtStatusCode, "IX_dvtCardStatus");

            entity.Property(e => e.DvtStatusCode)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasComment("Card Status Code")
                .HasColumnName("dvtStatus_code");
            entity.Property(e => e.DvtStatusDesc)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("Description of Card Status")
                .HasColumnName("dvtStatus_desc");
            entity.Property(e => e.DvtStatusDupOk)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("dvtStatus_dupOK");
            entity.Property(e => e.DvtStatusOverrideOk)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("dvtStatus_overrideOK");
            entity.Property(e => e.DvtStatusVoidable)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("dvtStatus_voidable");
        });

        modelBuilder.Entity<DvtClient>(entity =>
        {
            entity.HasKey(e => e.DvtClientTypeId);

            entity.ToTable("dvtClient");

            entity.Property(e => e.DvtClientTypeId).HasColumnName("dvtClient_TypeId");
            entity.Property(e => e.DvtClientApproverLoginId)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("dvtClient_approver_loginID");
            entity.Property(e => e.DvtClientInUse).HasColumnName("dvtClient_inUse");
            entity.Property(e => e.DvtClientSubTypeAble).HasColumnName("dvtClient_SubTypeAble");
            entity.Property(e => e.DvtClientType)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("dvtClient_Type");
        });

        modelBuilder.Entity<DvtOverseer>(entity =>
        {
            entity.HasKey(e => e.OverseerId).HasName("oversee_id");

            entity.ToTable("dvtOverseer");

            entity.HasIndex(e => e.OverseerId, "oversee_code");

            entity.Property(e => e.OverseerId).HasColumnName("overseer_Id");
            entity.Property(e => e.OverseerMailIds)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("overseer_mailIDs");
            entity.Property(e => e.OverseerRole)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("overseer_role");
            entity.Property(e => e.OverseerSalutation)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("overseer_salutation");
        });

        modelBuilder.Entity<DvtReason>(entity =>
        {
            entity.HasKey(e => e.DvtReasonCode).HasName("PK_dvtAction");

            entity.ToTable("dvtReason");

            entity.HasIndex(e => e.DvtReasonDesc, "Description");

            entity.HasIndex(e => e.DvtReasonCode, "IX_dvtAction");

            entity.Property(e => e.DvtReasonCode)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasComment("Card Status Code")
                .HasColumnName("dvtReason_code");
            entity.Property(e => e.DvtReasonDesc)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("Description of Card Status")
                .HasColumnName("dvtReason_desc");
            entity.Property(e => e.DvtReasonDupOk)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("dvtReason_dupOK");
            entity.Property(e => e.DvtReasonInUse).HasColumnName("dvtReason_inUse");
            entity.Property(e => e.DvtReasonOverrideOk)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("dvtReason_overrideOK");
            entity.Property(e => e.DvtReasonVoidable)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("dvtReason_voidable");
        });

        modelBuilder.Entity<DvtSpecialApprover>(entity =>
        {
            entity.HasKey(e => e.SpecialApproverId);

            entity.ToTable("dvtSpecialApprover");

            entity.HasIndex(e => e.SpecialApproverClientType, "clientType");

            entity.HasIndex(e => e.SpecialApproverId, "recordID");

            entity.Property(e => e.SpecialApproverId).HasColumnName("specialApprover_ID");
            entity.Property(e => e.SpecialApproverClientType).HasColumnName("specialApprover_clientType");
            entity.Property(e => e.SpecialApproverMailId)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("specialApprover_mailID");
            entity.Property(e => e.SpecialApproverName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("specialApprover_name");
            entity.Property(e => e.SpecialApproverUnitCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("specialApprover_unitCode");
        });

        modelBuilder.Entity<DvtSpecialty>(entity =>
        {
            entity.HasKey(e => e.DvtSpecialtyId).HasName("PK_dvtFacSpec");

            entity.ToTable("dvtSpecialty", tb => tb.HasComment("Dictionary of Faculty Specialty Area"));

            entity.HasIndex(e => e.DvtSpecialtyId, "Specialty_ID").IsUnique();

            entity.HasIndex(e => e.DvtSpecialtyArea, "specialty");

            entity.Property(e => e.DvtSpecialtyId)
                .ValueGeneratedNever()
                .HasComment("Pimary key - Faculty Specialty Area ID")
                .HasColumnName("dvtSpecialty_id");
            entity.Property(e => e.DvtSpecialtyArea)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasComment("Faculty Specialty Area Description")
                .HasColumnName("dvtSpecialty_area");
        });

        modelBuilder.Entity<DvtSvmUnit>(entity =>
        {
            entity.ToTable("dvtSvmUnit");

            entity.HasIndex(e => e.DvtSvmUnitId, "dvtSvmUnit_ID");

            entity.Property(e => e.DvtSvmUnitId)
                .ValueGeneratedNever()
                .HasColumnName("dvtSvmUnit_id");
            entity.Property(e => e.DvtSvmUnitCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("dvtSvmUnit_code");
            entity.Property(e => e.DvtSvmUnitName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("dvtSvmUnit_name");
        });

        modelBuilder.Entity<EcoTimeBadgeExclusion>(entity =>
        {
            entity.HasKey(e => e.ExclusionId);

            entity.ToTable("EcoTimeBadgeExclusion");

            entity.Property(e => e.ExclusionId).HasColumnName("exclusionId");
            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("employeeId");
            entity.Property(e => e.Entered)
                .HasColumnType("datetime")
                .HasColumnName("entered");
            entity.Property(e => e.EnteredBy)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("enteredBy");
            entity.Property(e => e.IamId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("iamId");
            entity.Property(e => e.Reason)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("reason");
            entity.Property(e => e.Updated)
                .HasColumnType("datetime")
                .HasColumnName("updated");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("updatedBy");
        });

        modelBuilder.Entity<ExtVisit>(entity =>
        {
            entity.ToTable("extVisit");

            entity.Property(e => e.ExtVisitId).HasColumnName("extVisitID");
            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("action");
            entity.Property(e => e.AuthVerified).HasColumnName("authVerified");
            entity.Property(e => e.IdcardId).HasColumnName("idcardID");
            entity.Property(e => e.Ip)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("ip");
            entity.Property(e => e.Timestamp)
                .HasColumnType("datetime")
                .HasColumnName("timestamp");
        });

        modelBuilder.Entity<IdCard>(entity =>
        {
            entity.ToTable("idCard");

            entity.HasIndex(e => e.IdCardAppliedDate, "appliedDate");

            entity.HasIndex(e => new { e.IdCardAppliedDate, e.IdCardClientType }, "appliedDate_clientType");

            entity.HasIndex(e => e.IdCardClientId, "clientID");

            entity.HasIndex(e => e.IdCardClientType, "clientType");

            entity.HasIndex(e => e.IdCardCurrentStatus, "currentStatus");

            entity.HasIndex(e => new { e.IdCardDisplayName, e.IdCardLastName }, "displayName").HasFillFactor(90);

            entity.HasIndex(e => e.IdCardLoginId, "loginId").HasFillFactor(90);

            entity.HasIndex(e => e.IdCardId, "recordNumber");

            entity.HasIndex(e => e.IdCardCurrentStatus, "status");

            entity.HasIndex(e => e.IdCardSvmId, "svmUnitCode").HasFillFactor(90);

            entity.Property(e => e.IdCardId).HasColumnName("idCard_Id");
            entity.Property(e => e.IdCardAccessLevel)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("idCard_accessLevel");
            entity.Property(e => e.IdCardAppliedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_AppliedBy");
            entity.Property(e => e.IdCardAppliedDate)
                .HasColumnType("smalldatetime")
                .HasColumnName("idCard_AppliedDate");
            entity.Property(e => e.IdCardCardType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("idCard_CardType");
            entity.Property(e => e.IdCardCertification)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("idCard_Certification");
            entity.Property(e => e.IdCardClientId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasComment("Mothra ID")
                .HasColumnName("idCard_ClientId");
            entity.Property(e => e.IdCardClientType)
                .HasComment("1 = SVM Faculty / 2 = Staff (excluding VMDO / VMTH) / 3 = DVM student / 4 = MPVM Student / 5 = Outside Grad Student / 6 = Epi Grad Student / 7 = VMDO / 8 = VMTH / 9 = Resident / 10 = Intern / 11 = Veterinarian / 13 = Imm Grad Student / 14 = Comp Path Grad student")
                .HasColumnName("idCard_ClientType");
            entity.Property(e => e.IdCardCurrentStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("A=Active, L=Lost, V=Void, S=Stolen, WI=For Issuance, WN: For Notification")
                .HasColumnName("idCard_currentStatus");
            entity.Property(e => e.IdCardDisplayName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_DisplayName");
            entity.Property(e => e.IdCardFinalReviewBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_finalReviewBy");
            entity.Property(e => e.IdCardFinalReviewDate)
                .HasColumnType("smalldatetime")
                .HasColumnName("idCard_finalReviewDate");
            entity.Property(e => e.IdCardIssueDate)
                .HasColumnType("smalldatetime")
                .HasColumnName("idCard_IssueDate");
            entity.Property(e => e.IdCardLastName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_LastName");
            entity.Property(e => e.IdCardLine2)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("idCard_Line2");
            entity.Property(e => e.IdCardLoginId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_loginId");
            entity.Property(e => e.IdCardMailId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_mailId");
            entity.Property(e => e.IdCardNotifyDate)
                .HasComment("Date email sent to notify applicant ready to pickup")
                .HasColumnType("smalldatetime")
                .HasColumnName("idCard_notifyDate");
            entity.Property(e => e.IdCardNumber).HasColumnName("idCard_Number");
            entity.Property(e => e.IdCardPrintedDate)
                .HasColumnType("smalldatetime")
                .HasColumnName("idCard_PrintedDate");
            entity.Property(e => e.IdCardSalutation)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("idCard_Salutation");
            entity.Property(e => e.IdCardSpecialty)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("idCard_specialty");
            entity.Property(e => e.IdCardSvmId)
                .HasComment("FK to dvtSvmUnit if appropriate")
                .HasColumnName("idCard_svm_id");
            entity.Property(e => e.IdcardBadgeKey).HasColumnName("idcard_badgeKey");
            entity.Property(e => e.IdcardDeactivatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idcard_deactivatedBy");
            entity.Property(e => e.IdcardDeactivatedDate)
                .HasColumnType("datetime")
                .HasColumnName("idcard_deactivatedDate");
            entity.Property(e => e.IdcardDeactivatedReason)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("idcard_deactivatedReason");
            entity.Property(e => e.IdcardExternalApprover)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("idcard_externalApprover");
            entity.Property(e => e.IdcardExternalKey)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idcard_externalKey");
            entity.Property(e => e.IdcardSystem)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("idcard_system");
        });

        modelBuilder.Entity<IdCardToPrintQueue>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("idCardToPrintQueue");

            entity.Property(e => e.IdcardId).HasColumnName("idcard_id");
            entity.Property(e => e.PrintQueueId).HasColumnName("printQueue_id");
        });

        modelBuilder.Entity<IgnoreList>(entity =>
        {
            entity.HasKey(e => e.IgnoreId);

            entity.ToTable("ignoreList");

            entity.HasIndex(e => e.IamId, "UQ_ignoreList_iamId").IsUnique();

            entity.Property(e => e.IgnoreId).HasColumnName("ignoreId");
            entity.Property(e => e.DoNotGrant).HasColumnName("doNotGrant");
            entity.Property(e => e.DoNotRevoke).HasColumnName("doNotRevoke");
            entity.Property(e => e.IamId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("iamId");
            entity.Property(e => e.ModBy)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("modBy");
            entity.Property(e => e.ModTime)
                .HasColumnType("datetime")
                .HasColumnName("modTime");
        });

        modelBuilder.Entity<LenelBadge>(entity =>
        {
            entity.ToTable("lenelBadge");

            entity.HasIndex(e => e.LenelBadgeKey, "UX_lenelBadgeKey").IsUnique();

            entity.Property(e => e.LenelBadgeId)
                .ValueGeneratedNever()
                .HasColumnName("lenelBadgeId");
            entity.Property(e => e.Activate)
                .HasColumnType("datetime")
                .HasColumnName("activate");
            entity.Property(e => e.BadgeType).HasColumnName("badgeType");
            entity.Property(e => e.Deactivate)
                .HasColumnType("datetime")
                .HasColumnName("deactivate");
            entity.Property(e => e.IamId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("iam_id");
            entity.Property(e => e.Imported)
                .HasColumnType("datetime")
                .HasColumnName("imported");
            entity.Property(e => e.Inactive)
                .HasColumnType("datetime")
                .HasColumnName("inactive");
            entity.Property(e => e.LastChanged)
                .HasColumnType("datetime")
                .HasColumnName("lastChanged");
            entity.Property(e => e.LenelBadgeKey).HasColumnName("lenelBadgeKey");
            entity.Property(e => e.Status).HasColumnName("status");
        });

        modelBuilder.Entity<PhotoExport>(entity =>
        {
            entity.HasKey(e => e.IamId);

            entity.ToTable("photoExport");

            entity.Property(e => e.IamId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("iam_id");
            entity.Property(e => e.Dateexported)
                .HasColumnType("datetime")
                .HasColumnName("dateexported");
        });

        modelBuilder.Entity<PrintQueue>(entity =>
        {
            entity.ToTable("printQueue");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CardColor)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("card_color");
            entity.Property(e => e.CardNumber).HasColumnName("card_number");
            entity.Property(e => e.IdCardId).HasColumnName("idCard_id");
            entity.Property(e => e.Line2)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("line2");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.Notify).HasColumnName("notify");
            entity.Property(e => e.PhotoExists).HasColumnName("photo_exists");

            entity.HasOne(d => d.IdCard).WithMany(p => p.PrintQueues)
                .HasForeignKey(d => d.IdCardId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_printQueue_idCard");
        });

        modelBuilder.Entity<VwApproverMothraId>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_approver_mothraID");

            entity.Property(e => e.DvtApproverMailId)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("dvtApprover_mailID");
            entity.Property(e => e.DvtApproverName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("dvtApprover_name");
            entity.Property(e => e.IdsMothraid)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_mothraid");
        });

        modelBuilder.Entity<VwDelimitedSpecialApprover>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_delimited_specialApprover");

            entity.Property(e => e.SpecialApproverName)
                .IsUnicode(false)
                .HasColumnName("specialApprover_name");
            entity.Property(e => e.SpecialApproverUnitCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("specialApprover_unitCode");
        });

        modelBuilder.Entity<VwLatestIdcard>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_latest_idcard");

            entity.Property(e => e.IdCardClientId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("idCard_ClientId");
            entity.Property(e => e.IdCardClientType).HasColumnName("idCard_ClientType");
            entity.Property(e => e.IdCardCurrentStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_currentStatus");
            entity.Property(e => e.IdCardDisplayName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_DisplayName");
            entity.Property(e => e.IdCardFinalReviewBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_finalReviewBy");
            entity.Property(e => e.IdCardFinalReviewDate)
                .HasColumnType("datetime")
                .HasColumnName("idCard_finalReviewDate");
            entity.Property(e => e.IdCardLastName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_LastName");
            entity.Property(e => e.IdCardLine2)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("idCard_Line2");
            entity.Property(e => e.IdCardLoginId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_loginId");
            entity.Property(e => e.IdCardMailId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("idCard_mailId");
            entity.Property(e => e.IdCardNumber).HasColumnName("idCard_Number");
            entity.Property(e => e.IdCardSpecialty)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("idCard_specialty");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
