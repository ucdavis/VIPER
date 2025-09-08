using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Viper.Models.EquipmentLoan;

namespace Viper.Classes.SQLContext;

public partial class EquipmentLoanContext : DbContext
{
    public EquipmentLoanContext(DbContextOptions<EquipmentLoanContext> options)
        : base(options)
    {
    }
    public EquipmentLoanContext()
    {
    }

    public virtual DbSet<AppSetting> AppSettings { get; set; }

    public virtual DbSet<Asset> Assets { get; set; }

    public virtual DbSet<AssetNote> AssetNotes { get; set; }

    public virtual DbSet<AssetType> AssetTypes { get; set; }

    public virtual DbSet<Audit> Audits { get; set; }

    public virtual DbSet<EmailSent> EmailSents { get; set; }

    public virtual DbSet<EmailTemplate> EmailTemplates { get; set; }

    public virtual DbSet<Loan> Loans { get; set; }

    public virtual DbSet<LoanItem> LoanItems { get; set; }

    public virtual DbSet<LoanNote> LoanNotes { get; set; }

    public virtual DbSet<O> Os { get; set; }

    public virtual DbSet<Reason> Reasons { get; set; }

    public virtual DbSet<VwLoan> VwLoans { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (HttpHelper.Settings != null)
        {
            optionsBuilder.UseSqlServer(HttpHelper.Settings["ConnectionStrings:EquipmentLoan"]);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("appSettings");

            entity.Property(e => e.LateReportDays).HasColumnName("lateReportDays");
            entity.Property(e => e.LateReportEmails)
                .HasMaxLength(2000)
                .IsUnicode(false)
                .HasColumnName("lateReportEmails");
        });

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.ToTable("Asset");

            entity.HasIndex(e => e.AssetOs, "IX_Asset_OS");

            entity.HasIndex(e => e.AssetClass, "IX_Asset_class");

            entity.HasIndex(e => e.AssetName, "IX_Asset_name");

            entity.HasIndex(e => e.AssetStatus, "IX_Asset_status");

            entity.HasIndex(e => e.AssetTag, "IX_Asset_tag");

            entity.HasIndex(e => e.AssetType, "IX_Asset_type");

            entity.HasIndex(e => e.AssetTagUnq, "uq_asset_tag").IsUnique();

            entity.Property(e => e.AssetId).HasColumnName("asset_id");
            entity.Property(e => e.AssetClass)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("asset_class");
            entity.Property(e => e.AssetDecommissionDate)
                .HasColumnType("datetime")
                .HasColumnName("asset_decommission_date");
            entity.Property(e => e.AssetInsuranceDate)
                .HasColumnType("datetime")
                .HasColumnName("asset_insurance_date");
            entity.Property(e => e.AssetMake)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("asset_make");
            entity.Property(e => e.AssetModel)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("asset_model");
            entity.Property(e => e.AssetName)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("asset_name");
            entity.Property(e => e.AssetOs).HasColumnName("asset_os");
            entity.Property(e => e.AssetParent).HasColumnName("asset_parent");
            entity.Property(e => e.AssetPart)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("asset_part");
            entity.Property(e => e.AssetRepairDate)
                .HasColumnType("datetime")
                .HasColumnName("asset_repair_date");
            entity.Property(e => e.AssetSerial)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("asset_serial");
            entity.Property(e => e.AssetStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("asset_status");
            entity.Property(e => e.AssetTag)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("asset_tag");
            entity.Property(e => e.AssetTagUnq)
                .HasMaxLength(21)
                .IsUnicode(false)
                .HasComputedColumnSql("(case when [asset_tag] IS NULL then CONVERT([varchar](20),[asset_id],(0)) else case when [asset_tag]='' then CONVERT([varchar](20),[asset_id],(0)) else '~'+[asset_tag] end end)", false)
                .HasColumnName("asset_tag_unq");
            entity.Property(e => e.AssetType).HasColumnName("asset_type");
        });

        modelBuilder.Entity<AssetNote>(entity =>
        {
            entity.ToTable("AssetNote");

            entity.HasIndex(e => e.AssetnoteAssetid, "IX_AssetNote_assetid");

            entity.HasIndex(e => e.AssetnotePidm, "IX_AssetNote_pidm");

            entity.Property(e => e.AssetnoteId).HasColumnName("assetnote_id");
            entity.Property(e => e.AssetnoteAssetid).HasColumnName("assetnote_assetid");
            entity.Property(e => e.AssetnoteDate)
                .HasColumnType("datetime")
                .HasColumnName("assetnote_date");
            entity.Property(e => e.AssetnoteNote)
                .HasMaxLength(2000)
                .IsUnicode(false)
                .HasColumnName("assetnote_note");
            entity.Property(e => e.AssetnotePidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("assetnote_pidm");

            entity.HasOne(d => d.AssetnoteAsset).WithMany(p => p.AssetNotes)
                .HasForeignKey(d => d.AssetnoteAssetid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssetNote_Asset");
        });

        modelBuilder.Entity<AssetType>(entity =>
        {
            entity.ToTable("AssetType");

            entity.Property(e => e.AssettypeId).HasColumnName("assettype_id");
            entity.Property(e => e.AssettypeText)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("assettype_text");
        });

        modelBuilder.Entity<Audit>(entity =>
        {
            entity.ToTable("Audit");

            entity.HasIndex(e => e.AuditCode, "IX_Audit_Code");

            entity.HasIndex(e => e.AuditLoan, "IX_Audit_Loan");

            entity.HasIndex(e => e.AuditTechPidm, "IX_Audit_Tech");

            entity.HasIndex(e => e.AuditUserPidm, "IX_Audit_User");

            entity.HasIndex(e => e.AuditTimestamp, "IX_Audit_timestamp");

            entity.Property(e => e.AuditId).HasColumnName("audit_id");
            entity.Property(e => e.AuditAdditionalInfo)
                .HasMaxLength(2000)
                .IsUnicode(false)
                .HasColumnName("audit_additional_info");
            entity.Property(e => e.AuditCode).HasColumnName("audit_code");
            entity.Property(e => e.AuditLoan).HasColumnName("audit_loan");
            entity.Property(e => e.AuditTechPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("audit_tech_pidm");
            entity.Property(e => e.AuditTimestamp)
                .HasColumnType("datetime")
                .HasColumnName("audit_timestamp");
            entity.Property(e => e.AuditTypeDesc)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("audit_type_desc");
            entity.Property(e => e.AuditUserPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("audit_user_pidm");

            entity.HasOne(d => d.AuditLoanNavigation).WithMany(p => p.Audits)
                .HasForeignKey(d => d.AuditLoan)
                .HasConstraintName("FK_Audit_Loan");
        });

        modelBuilder.Entity<EmailSent>(entity =>
        {
            entity.HasKey(e => e.EmailId);

            entity.ToTable("EmailSent");

            entity.HasIndex(e => e.EmailLoanid, "IX_EmailSent_loanid");

            entity.HasIndex(e => e.EmailPidm, "IX_EmailSent_pidm");

            entity.HasIndex(e => e.EmailSender, "IX_EmailSent_sender");

            entity.HasIndex(e => e.EmailSent1, "IX_EmailSent_sent");

            entity.Property(e => e.EmailId).HasColumnName("email_id");
            entity.Property(e => e.EmailAddress)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("email_address");
            entity.Property(e => e.EmailLoanid).HasColumnName("email_loanid");
            entity.Property(e => e.EmailManual).HasColumnName("email_manual");
            entity.Property(e => e.EmailPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("email_pidm");
            entity.Property(e => e.EmailSender)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("email_sender");
            entity.Property(e => e.EmailSent1)
                .HasColumnType("datetime")
                .HasColumnName("email_sent");
            entity.Property(e => e.EmailText)
                .HasColumnType("text")
                .HasColumnName("email_text");
            entity.Property(e => e.EmailType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("email_type");

            entity.HasOne(d => d.EmailLoan).WithMany(p => p.EmailSents)
                .HasForeignKey(d => d.EmailLoanid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmailSent_Loan");
        });

        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.HasKey(e => e.TemplateId);

            entity.ToTable("EmailTemplate");

            entity.Property(e => e.TemplateId).HasColumnName("template_id");
            entity.Property(e => e.TemplateAudience)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("template_audience");
            entity.Property(e => e.TemplateAutoRemindDays).HasColumnName("template_autoRemindDays");
            entity.Property(e => e.TemplateName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("template_name");
            entity.Property(e => e.TemplateText1)
                .HasColumnType("text")
                .HasColumnName("template_text1");
            entity.Property(e => e.TemplateText2)
                .HasColumnType("text")
                .HasColumnName("template_text2");
        });

        modelBuilder.Entity<Loan>(entity =>
        {
            entity.HasKey(e => e.LoanId).HasName("PK_loan");

            entity.ToTable("Loan");

            entity.HasIndex(e => e.LoanDate, "IX_Loan_date");

            entity.HasIndex(e => e.LoanDueDate, "IX_Loan_due_date");

            entity.HasIndex(e => e.LoanExclude, "IX_Loan_exclude");

            entity.HasIndex(e => e.LoanExcludeDate, "IX_Loan_exclude_date");

            entity.HasIndex(e => e.LoanPidm, "IX_Loan_pidm");

            entity.HasIndex(e => e.LoanReason, "IX_Loan_reason");

            entity.HasIndex(e => e.LoanTechPidm, "IX_Loan_tech_pidm");

            entity.Property(e => e.LoanId).HasColumnName("loan_id");
            entity.Property(e => e.LoanAuthorization)
                .HasMaxLength(2000)
                .IsUnicode(false)
                .HasColumnName("loan_authorization");
            entity.Property(e => e.LoanComments)
                .HasMaxLength(2000)
                .IsUnicode(false)
                .HasColumnName("loan_comments");
            entity.Property(e => e.LoanDate)
                .HasColumnType("datetime")
                .HasColumnName("loan_date");
            entity.Property(e => e.LoanDueDate)
                .HasColumnType("datetime")
                .HasColumnName("loan_due_date");
            entity.Property(e => e.LoanExclude).HasColumnName("loan_exclude");
            entity.Property(e => e.LoanExcludeDate)
                .HasColumnType("datetime")
                .HasColumnName("loan_exclude_date");
            entity.Property(e => e.LoanExtendedOk).HasColumnName("loan_extendedOK");
            entity.Property(e => e.LoanPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("loan_pidm");
            entity.Property(e => e.LoanReason).HasColumnName("loan_reason");
            entity.Property(e => e.LoanSdpId).HasColumnName("loan_sdp_id");
            entity.Property(e => e.LoanSignature)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("loan_signature");
            entity.Property(e => e.LoanTechPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("loan_tech_pidm");

            entity.HasOne(d => d.LoanReasonNavigation).WithMany(p => p.Loans)
                .HasForeignKey(d => d.LoanReason)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Loan_Reason");
        });

        modelBuilder.Entity<LoanItem>(entity =>
        {
            entity.ToTable("LoanItem");

            entity.HasIndex(e => e.LoanitemAssetid, "IX_LoanItem_asset_id");

            entity.HasIndex(e => e.LoanitemCheckin, "IX_LoanItem_checkin");

            entity.HasIndex(e => e.LoanitemCheckinPidm, "IX_LoanItem_checkin_pidm");

            entity.HasIndex(e => e.LoanitemCheckout, "IX_LoanItem_checkout");

            entity.HasIndex(e => e.LoanitemCheckoutPidm, "IX_LoanItem_checkout_pidm");

            entity.HasIndex(e => e.LoanitemLoanid, "IX_LoanItem_loanid");

            entity.Property(e => e.LoanitemId).HasColumnName("loanitem_id");
            entity.Property(e => e.LoanitemAssetid).HasColumnName("loanitem_assetid");
            entity.Property(e => e.LoanitemCheckin)
                .HasColumnType("datetime")
                .HasColumnName("loanitem_checkin");
            entity.Property(e => e.LoanitemCheckinPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("loanitem_checkin_pidm");
            entity.Property(e => e.LoanitemCheckout)
                .HasColumnType("datetime")
                .HasColumnName("loanitem_checkout");
            entity.Property(e => e.LoanitemCheckoutPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("loanitem_checkout_pidm");
            entity.Property(e => e.LoanitemComment)
                .HasMaxLength(2000)
                .IsUnicode(false)
                .HasColumnName("loanitem_comment");
            entity.Property(e => e.LoanitemLoanid).HasColumnName("loanitem_loanid");

            entity.HasOne(d => d.LoanitemAsset).WithMany(p => p.LoanItems)
                .HasForeignKey(d => d.LoanitemAssetid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LoanItem_Asset");

            entity.HasOne(d => d.LoanitemLoan).WithMany(p => p.LoanItems)
                .HasForeignKey(d => d.LoanitemLoanid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LoanItem_loan");
        });

        modelBuilder.Entity<LoanNote>(entity =>
        {
            entity.ToTable("LoanNote");

            entity.HasIndex(e => e.LoannoteLoanid, "IX_LoanNote_loanid");

            entity.HasIndex(e => e.LoannotePidm, "IX_LoanNote_pidm");

            entity.Property(e => e.LoannoteId).HasColumnName("loannote_id");
            entity.Property(e => e.LoannoteDate)
                .HasColumnType("datetime")
                .HasColumnName("loannote_date");
            entity.Property(e => e.LoannoteLoanid).HasColumnName("loannote_loanid");
            entity.Property(e => e.LoannoteNote)
                .HasMaxLength(2000)
                .IsUnicode(false)
                .HasColumnName("loannote_note");
            entity.Property(e => e.LoannotePidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("loannote_pidm");

            entity.HasOne(d => d.LoannoteLoan).WithMany(p => p.LoanNotes)
                .HasForeignKey(d => d.LoannoteLoanid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LoanNote_loan");
        });

        modelBuilder.Entity<O>(entity =>
        {
            entity.HasKey(e => e.OsId);

            entity.ToTable("OS");

            entity.Property(e => e.OsId).HasColumnName("os_id");
            entity.Property(e => e.OsDescription)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("os_description");
        });

        modelBuilder.Entity<Reason>(entity =>
        {
            entity.ToTable("Reason");

            entity.Property(e => e.ReasonId).HasColumnName("reason_id");
            entity.Property(e => e.ReasonReason)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("reason_reason");
        });

        modelBuilder.Entity<VwLoan>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_loans");

            entity.Property(e => e.AssetClass)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("asset_class");
            entity.Property(e => e.AssetDecommissionDate)
                .HasColumnType("datetime")
                .HasColumnName("asset_decommission_date");
            entity.Property(e => e.AssetId).HasColumnName("asset_id");
            entity.Property(e => e.AssetInsuranceDate)
                .HasColumnType("datetime")
                .HasColumnName("asset_insurance_date");
            entity.Property(e => e.AssetMake)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("asset_make");
            entity.Property(e => e.AssetModel)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("asset_model");
            entity.Property(e => e.AssetName)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("asset_name");
            entity.Property(e => e.AssetOs).HasColumnName("asset_os");
            entity.Property(e => e.AssetParent).HasColumnName("asset_parent");
            entity.Property(e => e.AssetPart)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("asset_part");
            entity.Property(e => e.AssetRepairDate)
                .HasColumnType("datetime")
                .HasColumnName("asset_repair_date");
            entity.Property(e => e.AssetSerial)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("asset_serial");
            entity.Property(e => e.AssetStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("asset_status");
            entity.Property(e => e.AssetTag)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("asset_tag");
            entity.Property(e => e.AssetTagUnq)
                .HasMaxLength(21)
                .IsUnicode(false)
                .HasColumnName("asset_tag_unq");
            entity.Property(e => e.AssetType).HasColumnName("asset_type");
            entity.Property(e => e.AssetnoteAssetid).HasColumnName("assetnote_assetid");
            entity.Property(e => e.AssetnoteDate)
                .HasColumnType("datetime")
                .HasColumnName("assetnote_date");
            entity.Property(e => e.AssetnoteId).HasColumnName("assetnote_id");
            entity.Property(e => e.AssetnoteNote)
                .HasMaxLength(2000)
                .IsUnicode(false)
                .HasColumnName("assetnote_note");
            entity.Property(e => e.AssetnotePidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("assetnote_pidm");
            entity.Property(e => e.AssettypeId).HasColumnName("assettype_id");
            entity.Property(e => e.AssettypeText)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("assettype_text");
            entity.Property(e => e.LoanAuthorization)
                .HasMaxLength(2000)
                .IsUnicode(false)
                .HasColumnName("loan_authorization");
            entity.Property(e => e.LoanComments)
                .HasMaxLength(2000)
                .IsUnicode(false)
                .HasColumnName("loan_comments");
            entity.Property(e => e.LoanDate)
                .HasColumnType("datetime")
                .HasColumnName("loan_date");
            entity.Property(e => e.LoanDueDate)
                .HasColumnType("datetime")
                .HasColumnName("loan_due_date");
            entity.Property(e => e.LoanExclude).HasColumnName("loan_exclude");
            entity.Property(e => e.LoanExcludeDate)
                .HasColumnType("datetime")
                .HasColumnName("loan_exclude_date");
            entity.Property(e => e.LoanExtendedOk).HasColumnName("loan_extendedOK");
            entity.Property(e => e.LoanId).HasColumnName("loan_id");
            entity.Property(e => e.LoanPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("loan_pidm");
            entity.Property(e => e.LoanReason).HasColumnName("loan_reason");
            entity.Property(e => e.LoanSdpId).HasColumnName("loan_sdp_id");
            entity.Property(e => e.LoanSignature)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("loan_signature");
            entity.Property(e => e.LoanTechPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("loan_tech_pidm");
            entity.Property(e => e.LoanitemAssetid).HasColumnName("loanitem_assetid");
            entity.Property(e => e.LoanitemCheckin)
                .HasColumnType("datetime")
                .HasColumnName("loanitem_checkin");
            entity.Property(e => e.LoanitemCheckinPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("loanitem_checkin_pidm");
            entity.Property(e => e.LoanitemCheckout)
                .HasColumnType("datetime")
                .HasColumnName("loanitem_checkout");
            entity.Property(e => e.LoanitemCheckoutPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("loanitem_checkout_pidm");
            entity.Property(e => e.LoanitemComment)
                .HasMaxLength(2000)
                .IsUnicode(false)
                .HasColumnName("loanitem_comment");
            entity.Property(e => e.LoanitemId).HasColumnName("loanitem_id");
            entity.Property(e => e.LoanitemLoanid).HasColumnName("loanitem_loanid");
            entity.Property(e => e.MothraId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("mothraID");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
