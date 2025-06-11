using Microsoft.EntityFrameworkCore;
using Viper.Models.VIPER;

namespace Viper.Classes.SQLContext;

public partial class VIPERContext : DbContext
{
#pragma warning disable CS8618
    public VIPERContext()
    {
    }

    public VIPERContext(DbContextOptions<VIPERContext> options)
        : base(options)
    {
    }
#pragma warning restore CS8618

    /* DBO */
    public virtual DbSet<Keys> Keys { get; set; }
    public virtual DbSet<AppControl> AppControls { get; set; }

    public virtual DbSet<CasDbcache> CasDbcaches { get; set; }

    public virtual DbSet<CasDbcacheDashboard> CasDbcacheDashboards { get; set; }

    public virtual DbSet<ContentBlock> ContentBlocks { get; set; }

    public virtual DbSet<ContentBlockFile> ContentBlockFiles { get; set; }

    public virtual DbSet<ContentBlockToFile> ContentBlockToFiles { get; set; }

    public virtual DbSet<ContentBlockToPermission> ContentBlockToPermissions { get; set; }

    public virtual DbSet<ContentHistory> ContentHistories { get; set; }

    public virtual DbSet<FieldType> FieldTypes { get; set; }

    public virtual DbSet<Viper.Models.VIPER.File> Files { get; set; }

    public virtual DbSet<FileAudit> FileAudits { get; set; }

    public virtual DbSet<FileToPermission> FileToPermissions { get; set; }

    public virtual DbSet<FileToPerson> FileToPeople { get; set; }

    public virtual DbSet<Form> Forms { get; set; }

    public virtual DbSet<FormField> FormFields { get; set; }

    public virtual DbSet<FormFieldOption> FormFieldOptions { get; set; }

    public virtual DbSet<FormFieldOptionVersion> FormFieldOptionVersions { get; set; }

    public virtual DbSet<FormFieldRelationship> FormFieldRelationships { get; set; }

    public virtual DbSet<FormFieldVersion> FormFieldVersions { get; set; }

    public virtual DbSet<FormSubmission> FormSubmissions { get; set; }

    public virtual DbSet<FormSubmissionSnapshot> FormSubmissionSnapshots { get; set; }

    public virtual DbSet<FormVersion> FormVersions { get; set; }

    public virtual DbSet<LeftNavItem> LeftNavItems { get; set; }

    public virtual DbSet<LeftNavItemToPermission> LeftNavItemToPermissions { get; set; }

    public virtual DbSet<LeftNavMenu> LeftNavMenus { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Page> Pages { get; set; }

    public virtual DbSet<PageToFormField> PageToFormFields { get; set; }

    public virtual DbSet<QuickLink> QuickLinks { get; set; }

    public virtual DbSet<QuickLinkPerson> QuickLinkPeople { get; set; }

    public virtual DbSet<RelationType> RelationTypes { get; set; }

    public virtual DbSet<Relationship> Relationships { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<ReportField> ReportFields { get; set; }

    public virtual DbSet<SecureMediaAudit> SecureMediaAudits { get; set; }

    public virtual DbSet<SessionTimeout> SessionTimeouts{ get; set; }

    public virtual DbSet<SlowPage> SlowPages { get; set; }

    public virtual DbSet<Viper.Models.VIPER.System> Systems { get; set; }

    public virtual DbSet<TbSecuremediamanager> TbSecuremediamanagers { get; set; }

    public virtual DbSet<TblVfNotificationLog> TblVfNotificationLogs { get; set; }

    public virtual DbSet<Term> Terms { get; set; }

    public virtual DbSet<VfNotification> VfNotifications { get; set; }

    public virtual DbSet<VwSvmAffiliate> VwSvmAffiliates { get; set; }

    public virtual DbSet<WorkflowStage> WorkflowStages { get; set; }

    public virtual DbSet<WorkflowStageTransition> WorkflowStageTransitions { get; set; }

    public virtual DbSet<WorkflowStageVersion> WorkflowStageVersions { get; set; }

    /* users */
    public virtual DbSet<Person> People { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (HttpHelper.Settings != null)
        {
            optionsBuilder.UseSqlServer(HttpHelper.Settings["ConnectionStrings:VIPER"]);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppControl>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("AppControl");

            entity.Property(e => e.EndTime).HasColumnType("datetime");
            entity.Property(e => e.EnteredBy)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.EnteredTime).HasColumnType("datetime");
            entity.Property(e => e.FolderName)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.Message)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.MessageHeader)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.StartTime).HasColumnType("datetime");
        });

        modelBuilder.Entity<CasDbcache>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("CAS_DBCACHE");

            entity.Property(e => e.CookieId)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("cookie_id");
            entity.Property(e => e.OriginalTimeStamp)
                .HasColumnType("datetime")
                .HasColumnName("original_time_stamp");
            entity.Property(e => e.TimeStamp)
                .HasColumnType("datetime")
                .HasColumnName("time_stamp");
            entity.Property(e => e.UserId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("user_id");
        });

        modelBuilder.Entity<CasDbcacheDashboard>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("CAS_DBCACHE_DASHBOARD");

            entity.Property(e => e.CookieId)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("cookie_id");
            entity.Property(e => e.OriginalTimeStamp)
                .HasColumnType("datetime")
                .HasColumnName("original_time_stamp");
            entity.Property(e => e.TimeStamp)
                .HasColumnType("datetime")
                .HasColumnName("time_stamp");
            entity.Property(e => e.UserId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("user_id");
        });

        modelBuilder.Entity<ContentBlock>(entity =>
        {
            entity.ToTable("ContentBlock");

            entity.HasIndex(e => e.FriendlyName, "IX_ContentBlock_friendlyName");

            entity.Property(e => e.ContentBlockId).HasColumnName("contentBlockID");
            entity.Property(e => e.AllowPublicAccess).HasColumnName("allowPublicAccess");
            entity.Property(e => e.Application)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("application");
            entity.Property(e => e.BlockOrder).HasColumnName("blockOrder");
            entity.Property(e => e.Content)
                .HasColumnType("text")
                .HasColumnName("contentBlock");
            entity.Property(e => e.DeletedOn)
                .HasColumnType("datetime")
                .HasColumnName("deletedOn");
            entity.Property(e => e.FriendlyName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("friendlyName");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("modifiedBy");
            entity.Property(e => e.ModifiedOn)
                .HasColumnType("datetime")
                .HasColumnName("modifiedOn");
            entity.Property(e => e.Page)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("page");
            entity.Property(e => e.System)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("system");
            entity.Property(e => e.Title)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("title");
            entity.Property(e => e.ViperSectionPath)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("viperSectionPath");
        });

        modelBuilder.Entity<ContentBlockFile>(entity =>
        {
            entity.HasKey(e => e.ContentBlockId);

            entity.ToTable("ContentBlockFile");

            entity.Property(e => e.ContentBlockId)
                .ValueGeneratedNever()
                .HasColumnName("contentBlockID");
            entity.Property(e => e.ContentBlockFileId)
                .ValueGeneratedOnAdd()
                .HasColumnName("contentBlockFileID");
            entity.Property(e => e.FileId).HasColumnName("fileID");
        });

        modelBuilder.Entity<ContentBlockToFile>(entity =>
        {
            entity.HasKey(e => e.ContentBlockFileId);

            entity.ToTable("ContentBlockToFile");

            entity.Property(e => e.ContentBlockFileId).HasColumnName("contentBlockFileID");
            entity.Property(e => e.ContentBlockId).HasColumnName("contentBlockID");
            entity.Property(e => e.FileGuid).HasColumnName("fileGUID");

            entity.HasOne(d => d.ContentBlock).WithMany(p => p.ContentBlockToFiles)
                .HasForeignKey(d => d.ContentBlockId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ContentBlockToFile_ContentBlock");

            entity.HasOne(d => d.File).WithMany(p => p.ContentBlockToFiles)
                .HasForeignKey(d => d.FileGuid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ContentBlockToFile_files");
        });

        modelBuilder.Entity<ContentBlockToPermission>(entity =>
        {
            entity.HasKey(e => e.ContentBlockPermissionId).HasName("PK_ConetntBlockToPermission");

            entity.ToTable("ContentBlockToPermission");

            entity.Property(e => e.ContentBlockPermissionId).HasColumnName("ContentBlockPermissionID");
            entity.Property(e => e.ContentBlockId).HasColumnName("ContentBlockID");
            entity.Property(e => e.Permission)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("permission");

            entity.HasOne(d => d.ContentBlock).WithMany(p => p.ContentBlockToPermissions)
                .HasForeignKey(d => d.ContentBlockId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ContentBlockToPermissions_ContentBlock");
        });

        modelBuilder.Entity<ContentHistory>(entity =>
        {
            entity.ToTable("ContentHistory");

            entity.Property(e => e.ContentHistoryId).HasColumnName("contentHistoryID");
            entity.Property(e => e.ContentBlockContent)
                .HasColumnType("text")
                .HasColumnName("contentBlockContent");
            entity.Property(e => e.ContentBlockId).HasColumnName("contentBlockID");
            entity.Property(e => e.DeletedOn)
                .HasColumnType("datetime")
                .HasColumnName("deletedOn");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("modifiedBy");
            entity.Property(e => e.ModifiedOn)
                .HasColumnType("datetime")
                .HasColumnName("modifiedOn");

            entity.HasOne(d => d.ContentBlock).WithMany(p => p.ContentHistories)
                .HasForeignKey(d => d.ContentBlockId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ContentHistory_contentBlock");
        });

        modelBuilder.Entity<FieldType>(entity =>
        {
            entity.ToTable("FieldType");

            entity.Property(e => e.FieldTypeId).HasColumnName("fieldTypeId");
            entity.Property(e => e.CanBeParent).HasColumnName("canBeParent");
            entity.Property(e => e.CanHaveParent).HasColumnName("canHaveParent");
            entity.Property(e => e.FieldTypeName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("fieldTypeName");
            entity.Property(e => e.PublicEditAllowed)
                .HasDefaultValueSql("((0))")
                .HasColumnName("publicEditAllowed");
        });

        modelBuilder.Entity<Viper.Models.VIPER.File>(entity =>
        {
            entity.HasKey(e => e.FileGuid);

            entity.ToTable("files");

            entity.HasIndex(e => e.FriendlyName, "UX_files_friendlyName").IsUnique();

            entity.Property(e => e.FileGuid)
                .ValueGeneratedNever()
                .HasColumnName("fileGUID");
            entity.Property(e => e.AllowPublicAccess).HasColumnName("allowPublicAccess");
            entity.Property(e => e.DeletedOn)
                .HasColumnType("datetime")
                .HasColumnName("deletedOn");
            entity.Property(e => e.Description)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.Encrypted).HasColumnName("encrypted");
            entity.Property(e => e.FilePath)
                .IsUnicode(false)
                .HasColumnName("filePath");
            entity.Property(e => e.Folder)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("folder");
            entity.Property(e => e.FriendlyName)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("friendlyName");
            entity.Property(e => e.Key)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("key");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("modifiedBy");
            entity.Property(e => e.ModifiedOn)
                .HasColumnType("datetime")
                .HasColumnName("modifiedOn");
            entity.Property(e => e.OldUrl)
                .IsUnicode(false)
                .HasColumnName("oldURL");
        });

        modelBuilder.Entity<FileAudit>(entity =>
        {
            entity.HasKey(e => e.AuditId);

            entity.ToTable("fileAudit");

            entity.Property(e => e.AuditId).HasColumnName("auditID");
            entity.Property(e => e.Action)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("action");
            entity.Property(e => e.ClientData)
                .IsUnicode(false)
                .HasColumnName("clientData");
            entity.Property(e => e.Detail)
                .IsUnicode(false)
                .HasColumnName("detail");
            entity.Property(e => e.FileGuid).HasColumnName("fileGUID");
            entity.Property(e => e.FileMetaData)
                .IsUnicode(false)
                .HasColumnName("fileMetaData");
            entity.Property(e => e.FilePath)
                .IsUnicode(false)
                .HasColumnName("filePath");
            entity.Property(e => e.IamId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("iam_id");
            entity.Property(e => e.Loginid)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("loginid");
            entity.Property(e => e.Timestamp)
                .HasColumnType("datetime")
                .HasColumnName("timestamp");
        });

        modelBuilder.Entity<FileToPermission>(entity =>
        {
            entity.ToTable("fileToPermission");

            entity.Property(e => e.FileToPermissionId).HasColumnName("fileToPermissionID");
            entity.Property(e => e.FileGuid).HasColumnName("fileGUID");
            entity.Property(e => e.Permission)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("permission");

            entity.HasOne(d => d.File).WithMany(p => p.FileToPermissions)
                .HasForeignKey(d => d.FileGuid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_fileToPermission_files");
        });

        modelBuilder.Entity<FileToPerson>(entity =>
        {
            entity.ToTable("fileToPerson");

            entity.Property(e => e.FileToPersonId).HasColumnName("fileToPersonID");
            entity.Property(e => e.FileGuid).HasColumnName("fileGUID");
            entity.Property(e => e.IamId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("iamID");

            entity.HasOne(d => d.File).WithMany(p => p.FileToPeople)
                .HasForeignKey(d => d.FileGuid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_fileToPerson_fileToPerson");
        });

        modelBuilder.Entity<Form>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK_ViperForm");

            entity.ToTable("Form");

            entity.Property(e => e.FormId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("formId");
            entity.Property(e => e.CustomSaveObject)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("customSaveObject");
            entity.Property(e => e.Description)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.FormName)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("formName");
            entity.Property(e => e.FriendlyUrl)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("friendlyURL");
            entity.Property(e => e.OwnerPermission)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("ownerPermission");
        });

        modelBuilder.Entity<FormField>(entity =>
        {
            entity.ToTable("FormField");

            entity.Property(e => e.FormFieldId).HasColumnName("formFieldId");
            entity.Property(e => e.EditPermission)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("editPermission");
            entity.Property(e => e.FieldTypeId).HasColumnName("fieldTypeId");
            entity.Property(e => e.FormId).HasColumnName("formId");
            entity.Property(e => e.ParentFormFieldId).HasColumnName("parentFormFieldId");
            entity.Property(e => e.PerUser).HasColumnName("perUser");
            entity.Property(e => e.ViewName)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("viewName");
            entity.Property(e => e.ViewPermission)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("viewPermission");

            entity.HasOne(d => d.FieldType).WithMany(p => p.FormFields)
                .HasForeignKey(d => d.FieldTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FormField_FieldType");

            entity.HasOne(d => d.Form).WithMany(p => p.FormFields)
                .HasForeignKey(d => d.FormId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FormField_Form");

            entity.HasOne(d => d.ParentFormField).WithMany(p => p.InverseParentFormField)
                .HasForeignKey(d => d.ParentFormFieldId)
                .HasConstraintName("FK_FormField_FormField");
        });

        modelBuilder.Entity<FormFieldOption>(entity =>
        {
            entity.ToTable("FormFieldOption");

            entity.Property(e => e.FormFieldOptionId).HasColumnName("formFieldOptionId");
            entity.Property(e => e.FormFieldId).HasColumnName("formFieldId");
            entity.Property(e => e.Value)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("value");

            entity.HasOne(d => d.FormField).WithMany(p => p.FormFieldOptions)
                .HasForeignKey(d => d.FormFieldId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FormFieldOption_FormField");
        });

        modelBuilder.Entity<FormFieldOptionVersion>(entity =>
        {
            entity.HasKey(e => e.FormFieldOptionVersionId).HasName("PK_FormFieldOptionVersion_1");

            entity.ToTable("FormFieldOptionVersion");

            entity.Property(e => e.FormFieldOptionVersionId).HasColumnName("formFieldOptionVersionId");
            entity.Property(e => e.Default).HasColumnName("default");
            entity.Property(e => e.FormFieldOptionId).HasColumnName("formFieldOptionId");
            entity.Property(e => e.Order).HasColumnName("order");
            entity.Property(e => e.Version).HasColumnName("version");

            entity.HasOne(d => d.FormFieldOption).WithMany(p => p.FormFieldOptionVersions)
                .HasForeignKey(d => d.FormFieldOptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FormFieldOptionVersion_FormFieldOption");
        });

        modelBuilder.Entity<FormFieldRelationship>(entity =>
        {
            entity.HasKey(e => e.FormFieldRelationshipId).HasName("PK_FormFieldRelationship_1");

            entity.ToTable("FormFieldRelationship");

            entity.Property(e => e.FormFieldRelationshipId).HasColumnName("formFieldRelationshipId");
            entity.Property(e => e.ChildFormFieldId).HasColumnName("childFormFieldId");
            entity.Property(e => e.FieldOrder).HasColumnName("fieldOrder");
            entity.Property(e => e.ParentFormFieldId).HasColumnName("parentFormFieldId");

            entity.HasOne(d => d.ChildFormField).WithMany(p => p.FormFieldRelationshipChildFormFields)
                .HasForeignKey(d => d.ChildFormFieldId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FormFieldRelationship_FormFieldChild");

            entity.HasOne(d => d.ParentFormField).WithMany(p => p.FormFieldRelationshipParentFormFields)
                .HasForeignKey(d => d.ParentFormFieldId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FormFieldRelationship_FormFieldParent");
        });

        modelBuilder.Entity<FormFieldVersion>(entity =>
        {
            entity.ToTable("FormFieldVersion");

            entity.HasIndex(e => new { e.FormFieldId, e.Version }, "IX_FormFieldVersion").IsUnique();

            entity.Property(e => e.FormFieldVersionId).HasColumnName("formFieldVersionId");
            entity.Property(e => e.AddToSubmissionTitle).HasColumnName("addToSubmissionTitle");
            entity.Property(e => e.DefaultValue)
                .IsUnicode(false)
                .HasColumnName("defaultValue");
            entity.Property(e => e.DisplayClass)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("displayClass");
            entity.Property(e => e.FieldName)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("fieldName");
            entity.Property(e => e.FormFieldId).HasColumnName("formFieldId");
            entity.Property(e => e.LabelText)
                .IsUnicode(false)
                .HasColumnName("labelText");
            entity.Property(e => e.MaxLength).HasColumnName("maxLength");
            entity.Property(e => e.MaxValue).HasColumnName("maxValue");
            entity.Property(e => e.MinLength).HasColumnName("minLength");
            entity.Property(e => e.MinValue).HasColumnName("minValue");
            entity.Property(e => e.OrderWithinParent).HasColumnName("orderWithinParent");
            entity.Property(e => e.Required).HasColumnName("required");
            entity.Property(e => e.Version).HasColumnName("version");

            entity.HasOne(d => d.FormField).WithMany(p => p.FormFieldVersions)
                .HasForeignKey(d => d.FormFieldId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FormFieldVersion_FormField");
        });

        modelBuilder.Entity<FormSubmission>(entity =>
        {
            entity.ToTable("FormSubmission");

            entity.Property(e => e.FormSubmissionId)
                .ValueGeneratedNever()
                .HasColumnName("formSubmissionId");
            entity.Property(e => e.FormId).HasColumnName("formId");
            entity.Property(e => e.InitiatedBy)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("initiatedBy");
            entity.Property(e => e.InitiatedOn)
                .HasColumnType("datetime")
                .HasColumnName("initiatedOn");
            entity.Property(e => e.LastUpdatedBy)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("lastUpdatedBy");
            entity.Property(e => e.LastUpdatedOn)
                .HasColumnType("datetime")
                .HasColumnName("lastUpdatedOn");
            entity.Property(e => e.SubmissionData)
                .HasColumnType("text")
                .HasColumnName("submissionData");
            entity.Property(e => e.SubmissionTitle)
                .IsUnicode(false)
                .HasColumnName("submissionTitle");
            entity.Property(e => e.Version).HasColumnName("version");
            entity.Property(e => e.WorkflowStageData)
                .HasColumnType("text")
                .HasColumnName("workflowStageData");
            entity.Property(e => e.WorkflowStageId).HasColumnName("workflowStageId");

            entity.HasOne(d => d.Form).WithMany(p => p.FormSubmissions)
                .HasForeignKey(d => d.FormId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FormSubmission_Form");

            entity.HasOne(d => d.WorkflowStage).WithMany(p => p.FormSubmissions)
                .HasForeignKey(d => d.WorkflowStageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FormSubmission_WorkflowStage");
        });

        modelBuilder.Entity<FormSubmissionSnapshot>(entity =>
        {
            entity.HasKey(e => e.FormsubmissionSnapshotId).HasName("PK_FormSubmissionSnapShot");

            entity.ToTable("FormSubmissionSnapshot");

            entity.Property(e => e.FormsubmissionSnapshotId)
                .ValueGeneratedNever()
                .HasColumnName("formsubmissionSnapshotId");
            entity.Property(e => e.FormId).HasColumnName("formId");
            entity.Property(e => e.FormSubmissionId).HasColumnName("formSubmissionId");
            entity.Property(e => e.InitiatedBy)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("initiatedBy");
            entity.Property(e => e.InitiatedOn)
                .HasColumnType("datetime")
                .HasColumnName("initiatedOn");
            entity.Property(e => e.LastUpdatedBy)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("lastUpdatedBy");
            entity.Property(e => e.LastUpdatedOn)
                .HasColumnType("datetime")
                .HasColumnName("lastUpdatedOn");
            entity.Property(e => e.SnapshotTimestamp)
                .HasColumnType("datetime")
                .HasColumnName("snapshotTimestamp");
            entity.Property(e => e.SubmissionData)
                .HasColumnType("text")
                .HasColumnName("submissionData");
            entity.Property(e => e.SubmissionTitle)
                .IsUnicode(false)
                .HasColumnName("submissionTitle");
            entity.Property(e => e.Version).HasColumnName("version");
            entity.Property(e => e.WorkflowStageData)
                .HasColumnType("text")
                .HasColumnName("workflowStageData");
            entity.Property(e => e.WorkflowStageId).HasColumnName("workflowStageId");

            entity.HasOne(d => d.Form).WithMany(p => p.FormSubmissionSnapshots)
                .HasForeignKey(d => d.FormId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FormSubmissionSnapshot_Form");

            entity.HasOne(d => d.WorkflowStage).WithMany(p => p.FormSubmissionSnapshots)
                .HasForeignKey(d => d.WorkflowStageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FormSubmissionSnapshot_WorkflowStage");
        });

        modelBuilder.Entity<FormVersion>(entity =>
        {
            entity.ToTable("FormVersion");

            entity.HasIndex(e => new { e.FormId, e.Version }, "IX_FormVersion").IsUnique();

            entity.Property(e => e.FormVersionId).HasColumnName("formVersionId");
            entity.Property(e => e.AllowNewSubmissions).HasColumnName("allowNewSubmissions");
            entity.Property(e => e.Description)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.ExistingSubmissionsOpen).HasColumnName("existingSubmissionsOpen");
            entity.Property(e => e.FormId).HasColumnName("formId");
            entity.Property(e => e.FormName)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("formName");
            entity.Property(e => e.FriendlyUrl)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("friendlyURL");
            entity.Property(e => e.ShowInManageUi).HasColumnName("showInManageUI");
            entity.Property(e => e.Version).HasColumnName("version");

            entity.HasOne(d => d.Form).WithMany(p => p.FormVersions)
                .HasForeignKey(d => d.FormId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FormVersion_Form");
        });

        modelBuilder.Entity<LeftNavItem>(entity =>
        {
            entity.ToTable("LeftNavItem");

            entity.Property(e => e.LeftNavItemId).HasColumnName("leftNavItemID");
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
            entity.Property(e => e.IsHeader).HasColumnName("isHeader");
            entity.Property(e => e.LeftNavMenuId).HasColumnName("leftNavMenuID");
            entity.Property(e => e.MenuItemText)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("menuItemText");
            entity.Property(e => e.Url)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("url");

            entity.HasOne(d => d.LeftNavMenu).WithMany(p => p.LeftNavItems)
                .HasForeignKey(d => d.LeftNavMenuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeftNavItem_LeftNavMenu");
        });

        modelBuilder.Entity<LeftNavItemToPermission>(entity =>
        {
            entity.HasKey(e => e.LeftNavItemPermissionId).HasName("PK_LeftNavToPermission");

            entity.ToTable("LeftNavItemToPermission");

            entity.Property(e => e.LeftNavItemPermissionId).HasColumnName("LeftNavItemPermissionID");
            entity.Property(e => e.LeftNavItemId).HasColumnName("LeftNavItemID");
            entity.Property(e => e.Permission)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("permission");

            entity.HasOne(d => d.LeftNavItem).WithMany(p => p.LeftNavItemToPermissions)
                .HasForeignKey(d => d.LeftNavItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeftNavToPermission_LeftNavItem");
        });

        modelBuilder.Entity<LeftNavMenu>(entity =>
        {
            entity.ToTable("LeftNavMenu");

            entity.HasIndex(e => e.FriendlyName, "IX_LeftNavMenu_friendlyName");

            entity.Property(e => e.LeftNavMenuId).HasColumnName("leftNavMenuID");
            entity.Property(e => e.Application)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("application");
            entity.Property(e => e.FriendlyName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("friendlyName");
            entity.Property(e => e.MenuHeaderText)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("menuHeaderText");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("modifiedBy");
            entity.Property(e => e.ModifiedOn)
                .HasColumnType("datetime")
                .HasColumnName("modifiedOn");
            entity.Property(e => e.Page)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("page");
            entity.Property(e => e.System)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("system");
            entity.Property(e => e.ViperSectionPath)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("viperSectionPath");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notification");

            entity.Property(e => e.NotificationId).HasColumnName("notificationId");
            entity.Property(e => e.BccEmail)
                .IsUnicode(false)
                .HasColumnName("bccEmail");
            entity.Property(e => e.BodyTemplate).HasColumnName("bodyTemplate");
            entity.Property(e => e.CcEmail)
                .IsUnicode(false)
                .HasColumnName("ccEmail");
            entity.Property(e => e.FromEmail)
                .IsUnicode(false)
                .HasColumnName("fromEmail");
            entity.Property(e => e.SubjectTemplate)
                .HasMaxLength(78)
                .HasColumnName("subjectTemplate");
            entity.Property(e => e.ToEmail)
                .IsUnicode(false)
                .HasColumnName("toEmail");
            entity.Property(e => e.Version).HasColumnName("version");
            entity.Property(e => e.WorkflowStageTransitionId).HasColumnName("workflowStageTransitionId");

            entity.HasOne(d => d.WorkflowStageTransition).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.WorkflowStageTransitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notification_WorkflowStageTransition");
        });

        modelBuilder.Entity<Page>(entity =>
        {
            entity.ToTable("Page");

            entity.Property(e => e.PageId).HasColumnName("pageId");
            entity.Property(e => e.FormId).HasColumnName("formId");
            entity.Property(e => e.Order).HasColumnName("order");
            entity.Property(e => e.ReadOnlyWorkflowstages)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("readOnlyWorkflowstages");
            entity.Property(e => e.Title)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("title");
            entity.Property(e => e.Version).HasColumnName("version");

            entity.HasOne(d => d.Form).WithMany(p => p.Pages)
                .HasForeignKey(d => d.FormId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Page_Form");
        });

        modelBuilder.Entity<PageToFormField>(entity =>
        {
            entity.ToTable("PageToFormField");

            entity.HasIndex(e => new { e.PageId, e.FormFieldId }, "UX_PageToFormField_Page_Field").IsUnique();

            entity.Property(e => e.PageToFormFieldId).HasColumnName("pageToFormFieldId");
            entity.Property(e => e.FormFieldId).HasColumnName("formFieldId");
            entity.Property(e => e.Order).HasColumnName("order");
            entity.Property(e => e.PageId).HasColumnName("pageId");
            entity.Property(e => e.ReadOnly).HasColumnName("readOnly");

            entity.HasOne(d => d.FormField).WithMany(p => p.PageToFormFields)
                .HasForeignKey(d => d.FormFieldId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PageToFormField_FormField");

            entity.HasOne(d => d.Page).WithMany(p => p.PageToFormFields)
                .HasForeignKey(d => d.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PageToFormField_Page");
        });

        modelBuilder.Entity<QuickLink>(entity =>
        {
            entity.Property(e => e.QuickLinkId).HasColumnName("QuickLink_id");
            entity.Property(e => e.QuickLinkLabel)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("QuickLink_Label");
            entity.Property(e => e.QuickLinkPermission)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("QuickLink_Permission");
            entity.Property(e => e.QuickLinkTab)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("QuickLink_Tab");
            entity.Property(e => e.QuickLinkUrl)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("QuickLink_URL");
            entity.Property(e => e.SystemDescription)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("System_Description");
            entity.Property(e => e.SystemQuicklink).HasColumnName("System_Quicklink");
            entity.Property(e => e.SystemType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("System_Type");
        });

        modelBuilder.Entity<QuickLinkPerson>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.QuickLinkPeopleId)
                .ValueGeneratedOnAdd()
                .HasColumnName("QuickLinkPeople_ID");
            entity.Property(e => e.QuickLinkPeopleLinkId).HasColumnName("QuickLinkPeople_Link_ID");
            entity.Property(e => e.QuickLinkPeopleOrder).HasColumnName("QuickLinkPeople_Order");
            entity.Property(e => e.QuickLinkPeoplePidm).HasColumnName("QuickLinkPeople_PIDM");
        });

        modelBuilder.Entity<RelationType>(entity =>
        {
            entity.HasKey(e => e.RelTypeId);

            entity.Property(e => e.RelTypeId).HasColumnName("RelType_ID");
            entity.Property(e => e.RelTypeDescription)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("RelType_Description");
            entity.Property(e => e.RelTypeType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("RelType_Type");
        });

        modelBuilder.Entity<Relationship>(entity =>
        {
            entity.HasKey(e => e.RelId);

            entity.Property(e => e.RelId).HasColumnName("Rel_ID");
            entity.Property(e => e.RelComment)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Rel_Comment");
            entity.Property(e => e.RelRelTypeId).HasColumnName("Rel_RelType_ID");
            entity.Property(e => e.RelSystem1).HasColumnName("Rel_System1");
            entity.Property(e => e.RelSystem2).HasColumnName("Rel_System2");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.ToTable("Report");

            entity.Property(e => e.ReportId).HasColumnName("reportId");
            entity.Property(e => e.Excel).HasColumnName("excel");
            entity.Property(e => e.FormColumns)
                .HasColumnType("text")
                .HasColumnName("formColumns");
            entity.Property(e => e.FormId).HasColumnName("formId");
            entity.Property(e => e.LastUpdatedBy)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("lastUpdatedBy");
            entity.Property(e => e.LastUpdatedOn)
                .HasColumnType("datetime")
                .HasColumnName("lastUpdatedOn");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("name");

            entity.HasOne(d => d.Form).WithMany(p => p.Reports)
                .HasForeignKey(d => d.FormId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Report_Form");
        });

        modelBuilder.Entity<ReportField>(entity =>
        {
            entity.ToTable("ReportField");

            entity.Property(e => e.ReportFieldId).HasColumnName("reportFieldId");
            entity.Property(e => e.FilterOperation)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("filterOperation");
            entity.Property(e => e.FilterValue)
                .IsUnicode(false)
                .HasColumnName("filterValue");
            entity.Property(e => e.FormFieldId).HasColumnName("formFieldId");
            entity.Property(e => e.Order).HasColumnName("order");
            entity.Property(e => e.ReportId).HasColumnName("reportId");

            entity.HasOne(d => d.FormField).WithMany(p => p.ReportFields)
                .HasForeignKey(d => d.FormFieldId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReportField_FormField");

            entity.HasOne(d => d.Report).WithMany(p => p.ReportFields)
                .HasForeignKey(d => d.ReportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReportField_Report");
        });

        modelBuilder.Entity<SecureMediaAudit>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("SecureMediaAudit");

            entity.Property(e => e.Action)
                .HasMaxLength(50)
                .HasColumnName("action");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.Whoby)
                .HasMaxLength(50)
                .HasColumnName("whoby");
            entity.Property(e => e.Whotime)
                .HasColumnType("datetime")
                .HasColumnName("whotime");
        });

        modelBuilder.Entity<SessionTimeout>(entity =>
        {
            entity.HasKey(e => new { e.LoginId, e.Service });
            entity.ToTable("SessionTimeout");
            entity.Property(e => e.LoginId)
                .HasMaxLength(8)
                .HasColumnName("loginid");
            entity.Property(e => e.SessionTimeoutDateTime)
                .HasColumnType("datetime")
                .HasColumnName("sessionTimeout");
            entity.Property(e => e.Service)
                .HasMaxLength(50)
                .HasColumnName("service");
        });

        modelBuilder.Entity<SlowPage>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DateRendered).HasColumnType("datetime");
            entity.Property(e => e.LoginId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("LoginID");
            entity.Property(e => e.Page)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Path)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Viper.Models.VIPER.System>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.SystemId).HasColumnName("System_ID");
            entity.Property(e => e.SystemName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("System_Name");
            entity.Property(e => e.SystemType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("System_Type");
        });

        modelBuilder.Entity<TbSecuremediamanager>(entity =>
        {
            entity.ToTable("tb_securemediamanager");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("action");
            entity.Property(e => e.ActionItem)
                .HasMaxLength(400)
                .HasColumnName("action_item");
            entity.Property(e => e.Time)
                .HasColumnType("datetime")
                .HasColumnName("time");
            entity.Property(e => e.Who)
                .HasMaxLength(500)
                .HasColumnName("who");
        });

        modelBuilder.Entity<TblVfNotificationLog>(entity =>
        {
            entity.HasKey(e => e.NotifyLogId);

            entity.ToTable("tbl_VF_Notification_Log");

            entity.Property(e => e.NotifyLogId)
                .HasComment("log entry id")
                .HasColumnName("notify_log_id");
            entity.Property(e => e.NotifyId)
                .HasComment("notification id")
                .HasColumnName("notify_id");
            entity.Property(e => e.NotifyLogDatetime)
                .HasComment("notification sent date and time")
                .HasColumnType("datetime")
                .HasColumnName("notify_log_datetime");
            entity.Property(e => e.NotifyLogSendTo)
                .HasMaxLength(500)
                .HasComment("notification sent to list")
                .HasColumnName("notify_log_send_to");
        });

        modelBuilder.Entity<Term>(entity =>
        {
            entity.ToTable("vwTerms");
            entity.HasKey(e => e.TermCode);
        });

        modelBuilder.Entity<VfNotification>(entity =>
        {
            entity.HasKey(e => e.NotifyId).HasName("PK_tbl_VF_notify");

            entity.ToTable("VF_notification");

            entity.Property(e => e.NotifyId)
                .HasComment("ID")
                .HasColumnName("notify_id");
            entity.Property(e => e.Body).HasColumnName("body");
            entity.Property(e => e.FormId)
                .HasComment("form ID")
                .HasColumnName("form_id");
            entity.Property(e => e.NotifyDesc)
                .HasMaxLength(500)
                .HasComment("short description, ie application approved/declined")
                .HasColumnName("notify_desc");
            entity.Property(e => e.SendTo)
                .HasMaxLength(500)
                .HasColumnName("send_to");
            entity.Property(e => e.Stage)
                .HasMaxLength(200)
                .HasColumnName("stage");
            entity.Property(e => e.StageId).HasColumnName("stageId");
            entity.Property(e => e.Subject)
                .HasMaxLength(500)
                .HasColumnName("subject");
        });

        modelBuilder.Entity<VwSvmAffiliate>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_svmAffiliates");

            entity.Property(e => e.Email)
                .HasMaxLength(44)
                .IsUnicode(false);
            entity.Property(e => e.FieldOrder)
                .HasMaxLength(92)
                .IsUnicode(false)
                .HasColumnName("fieldOrder");
            entity.Property(e => e.FieldText)
                .HasMaxLength(92)
                .IsUnicode(false)
                .HasColumnName("fieldText");
            entity.Property(e => e.FieldValue)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("fieldValue");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("first_name");
            entity.Property(e => e.FullName)
                .HasMaxLength(91)
                .IsUnicode(false);
            entity.Property(e => e.IdsIamId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("ids_iam_id");
            entity.Property(e => e.IdsLoginid)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("ids_loginid");
            entity.Property(e => e.IdsMailid)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("ids_mailid");
            entity.Property(e => e.IdsMothraid)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_mothraid");
            entity.Property(e => e.LastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("last_name");
        });

        modelBuilder.Entity<WorkflowStage>(entity =>
        {
            entity.ToTable("WorkflowStage");

            entity.Property(e => e.WorkflowStageId).HasColumnName("workflowStageId");
            entity.Property(e => e.EditPermission)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("editPermission");
            entity.Property(e => e.FormId).HasColumnName("formId");
            entity.Property(e => e.IsFinal).HasColumnName("isFinal");
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.PubliclyAccessible).HasColumnName("publiclyAccessible");
            entity.Property(e => e.ViewPermission)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("viewPermission");

            entity.HasOne(d => d.Form).WithMany(p => p.WorkflowStages)
                .HasForeignKey(d => d.FormId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkflowStage_Form");

            entity.HasMany(d => d.Pages).WithMany(p => p.WorkflowStages)
                .UsingEntity<Dictionary<string, object>>(
                    "WorkflowStageToPage",
                    r => r.HasOne<Page>().WithMany()
                        .HasForeignKey("PageId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_WorkflowStageToPage_Page"),
                    l => l.HasOne<WorkflowStage>().WithMany()
                        .HasForeignKey("WorkflowStageId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_WorkflowStageToPage_WorkflowStage"),
                    j =>
                    {
                        j.HasKey("WorkflowStageId", "PageId");
                        j.ToTable("WorkflowStageToPage");
                        j.IndexerProperty<int>("WorkflowStageId").HasColumnName("workflowStageId");
                        j.IndexerProperty<int>("PageId").HasColumnName("pageId");
                    });
        });

        modelBuilder.Entity<WorkflowStageTransition>(entity =>
        {
            entity.ToTable("WorkflowStageTransition");

            entity.HasIndex(e => new { e.FromWorkflowStage, e.ToWorkflowStage, e.Version }, "UX_WorkflowStageTransition_FromStage_ToStage").IsUnique();

            entity.Property(e => e.WorkflowStageTransitionId).HasColumnName("workflowStageTransitionId");
            entity.Property(e => e.ConditionFormFieldId).HasColumnName("conditionFormFieldId");
            entity.Property(e => e.FromWorkflowStage).HasColumnName("fromWorkflowStage");
            entity.Property(e => e.LoadImmediately).HasColumnName("loadImmediately");
            entity.Property(e => e.ToWorkflowStage).HasColumnName("toWorkflowStage");
            entity.Property(e => e.Value)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("value");
            entity.Property(e => e.Version).HasColumnName("version");

            entity.HasOne(d => d.ConditionFormField).WithMany(p => p.WorkflowStageTransitions)
                .HasForeignKey(d => d.ConditionFormFieldId)
                .HasConstraintName("FK_WorkflowStageTransition_FormField");

            entity.HasOne(d => d.FromWorkflowStageNavigation).WithMany(p => p.WorkflowStageTransitionFromWorkflowStageNavigations)
                .HasForeignKey(d => d.FromWorkflowStage)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkflowStageTransition_WorkflowStageTransition");

            entity.HasOne(d => d.ToWorkflowStageNavigation).WithMany(p => p.WorkflowStageTransitionToWorkflowStageNavigations)
                .HasForeignKey(d => d.ToWorkflowStage)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkflowStageTransition_WorkflowStageTransition1");
        });

        modelBuilder.Entity<WorkflowStageVersion>(entity =>
        {
            entity.ToTable("WorkflowStageVersion");

            entity.Property(e => e.WorkflowStageVersionId).HasColumnName("workflowStageVersionId");
            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.Message)
                .IsUnicode(false)
                .HasColumnName("message");
            entity.Property(e => e.Order).HasColumnName("order");
            entity.Property(e => e.Version).HasColumnName("version");
            entity.Property(e => e.WorkflowStageId).HasColumnName("workflowStageId");

            entity.HasOne(d => d.WorkflowStage).WithMany(p => p.WorkflowStageVersions)
                .HasForeignKey(d => d.WorkflowStageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkflowStageVersion_WorkflowStage");
        });

        /* users */
        modelBuilder.Entity<Person>(entity =>
        {
            entity.ToTable("Person", "users");
            entity.HasKey("PersonId");
            entity.HasOne(e => e.StudentInfo).WithMany()
                .HasForeignKey(e => new { e.StudentTerm, e.SpridenId })
                .IsRequired(false);
        });
        OnModelCreatingCTS(modelBuilder);
        OnModelCreatingStudents(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    partial void OnModelCreatingCTS(ModelBuilder modelBuilder);
    partial void OnModelCreatingStudents(ModelBuilder modelBuilder);
}
