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

    public virtual DbSet<AppControl> AppControls { get; set; }

    public virtual DbSet<Bundle> Bundles { get; set; }

    public virtual DbSet<BundleCompetency> BundleCompetencies { get; set; }

    public virtual DbSet<BundleCompetencyGroup> BundleCompetencyGroups { get; set; }

    public virtual DbSet<BundleCompetencyLevel> BundleCompetencyLevels { get; set; }

    public virtual DbSet<BundleRole> BundleRoles { get; set; }

    public virtual DbSet<BundleService> BundleServices { get; set; }

    public virtual DbSet<CasDbcache> CasDbcaches { get; set; }

    public virtual DbSet<CasDbcacheDashboard> CasDbcacheDashboards { get; set; }

    public virtual DbSet<ClassYearLeftReason> ClassYearLeftReasons { get; set; }

    public virtual DbSet<CompTemp> CompTemps { get; set; }

    public virtual DbSet<Competency> Competencies { get; set; }

    public virtual DbSet<CompetencyMapping> CompetencyMappings { get; set; }

    public virtual DbSet<CompetencyOutcome> CompetencyOutcomes { get; set; }

    public virtual DbSet<ContentBlock> ContentBlocks { get; set; }

    public virtual DbSet<ContentBlockFile> ContentBlockFiles { get; set; }

    public virtual DbSet<ContentBlockToFile> ContentBlockToFiles { get; set; }

    public virtual DbSet<ContentBlockToPermission> ContentBlockToPermissions { get; set; }

    public virtual DbSet<ContentHistory> ContentHistories { get; set; }

    public virtual DbSet<CourseCompetency> CourseCompetencies { get; set; }

    public virtual DbSet<CourseRole> CourseRoles { get; set; }

    public virtual DbSet<CtsAudit> CtsAudits { get; set; }

    public virtual DbSet<DeviceCheckIn> DeviceCheckIns { get; set; }

    public virtual DbSet<Domain> Domains { get; set; }

    public virtual DbSet<EmergencyContact> EmergencyContacts { get; set; }

    public virtual DbSet<Encounter> Encounters { get; set; }

    public virtual DbSet<EncounterInstructor> EncounterInstructors { get; set; }

    public virtual DbSet<Epa> Epas { get; set; }

    public virtual DbSet<EpaService> EpaServices { get; set; }

    public virtual DbSet<FieldType> FieldTypes { get; set; }

    public virtual DbSet<Viper.Models.EquipmentLoan.File> Files { get; set; }

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

    public virtual DbSet<Level> Levels { get; set; }

    public virtual DbSet<MilestoneLevel> MilestoneLevels { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Page> Pages { get; set; }

    public virtual DbSet<PageToFormField> PageToFormFields { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<Person> People { get; set; }

    public virtual DbSet<QrtzBlobTrigger> QrtzBlobTriggers { get; set; }

    public virtual DbSet<QrtzCalendar> QrtzCalendars { get; set; }

    public virtual DbSet<QrtzCronTrigger> QrtzCronTriggers { get; set; }

    public virtual DbSet<QrtzFiredTrigger> QrtzFiredTriggers { get; set; }

    public virtual DbSet<QrtzJobDetail> QrtzJobDetails { get; set; }

    public virtual DbSet<QrtzLock> QrtzLocks { get; set; }

    public virtual DbSet<QrtzPausedTriggerGrp> QrtzPausedTriggerGrps { get; set; }

    public virtual DbSet<QrtzSchedulerState> QrtzSchedulerStates { get; set; }

    public virtual DbSet<QrtzSimpleTrigger> QrtzSimpleTriggers { get; set; }

    public virtual DbSet<QrtzSimpropTrigger> QrtzSimpropTriggers { get; set; }

    public virtual DbSet<QrtzTrigger> QrtzTriggers { get; set; }

    public virtual DbSet<QuickLink> QuickLinks { get; set; }

    public virtual DbSet<QuickLinkPerson> QuickLinkPeople { get; set; }

    public virtual DbSet<RelationType> RelationTypes { get; set; }

    public virtual DbSet<Relationship> Relationships { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<ReportField> ReportFields { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<ScheduledTask> ScheduledTasks { get; set; }

    public virtual DbSet<ScheduledTaskHistory> ScheduledTaskHistories { get; set; }

    public virtual DbSet<SecureMediaAudit> SecureMediaAudits { get; set; }

    public virtual DbSet<SessionCompetency> SessionCompetencies { get; set; }

    public virtual DbSet<SessionTimeout> SessionTimeouts { get; set; }

    public virtual DbSet<SlowPage> SlowPages { get; set; }

    public virtual DbSet<StudentClassYear> StudentClassYears { get; set; }

    public virtual DbSet<StudentCompetency> StudentCompetencies { get; set; }

    public virtual DbSet<StudentContact> StudentContacts { get; set; }

    public virtual DbSet<StudentEpa> StudentEpas { get; set; }

    public virtual DbSet<Viper.Models.EquipmentLoan.System> Systems { get; set; }

    public virtual DbSet<TbSecuremediamanager> TbSecuremediamanagers { get; set; }

    public virtual DbSet<TblVfNotificationLog> TblVfNotificationLogs { get; set; }

    public virtual DbSet<VfNotification> VfNotifications { get; set; }

    public virtual DbSet<VwCourse> VwCourses { get; set; }

    public virtual DbSet<VwCourseSessionOffering> VwCourseSessionOfferings { get; set; }

    public virtual DbSet<VwCourseSessionOffering1> VwCourseSessionOfferings1 { get; set; }

    public virtual DbSet<VwDvmstudent> VwDvmstudents { get; set; }

    public virtual DbSet<VwEquipmentLoan> VwEquipmentLoans { get; set; }

    public virtual DbSet<VwEvaluateesByInstance> VwEvaluateesByInstances { get; set; }

    public virtual DbSet<VwInstance> VwInstances { get; set; }

    public virtual DbSet<VwInstructorSchedule> VwInstructorSchedules { get; set; }

    public virtual DbSet<VwKey> VwKeys { get; set; }

    public virtual DbSet<VwLegacyCompetency> VwLegacyCompetencies { get; set; }

    public virtual DbSet<VwLegacySessionCompetency> VwLegacySessionCompetencies { get; set; }

    public virtual DbSet<VwPerson> VwPeople { get; set; }

    public virtual DbSet<VwPersonJobPosition> VwPersonJobPositions { get; set; }

    public virtual DbSet<VwRotation> VwRotations { get; set; }

    public virtual DbSet<VwService> VwServices { get; set; }

    public virtual DbSet<VwServiceChief> VwServiceChiefs { get; set; }

    public virtual DbSet<VwSession> VwSessions { get; set; }

    public virtual DbSet<VwStudent> VwStudents { get; set; }

    public virtual DbSet<VwStudentSchedule> VwStudentSchedules { get; set; }

    public virtual DbSet<VwSvmAffiliate> VwSvmAffiliates { get; set; }

    public virtual DbSet<VwTerm> VwTerms { get; set; }

    public virtual DbSet<VwWeek> VwWeeks { get; set; }

    public virtual DbSet<VwWeekGradYear> VwWeekGradYears { get; set; }

    public virtual DbSet<WorkflowStage> WorkflowStages { get; set; }

    public virtual DbSet<WorkflowStageTransition> WorkflowStageTransitions { get; set; }

    public virtual DbSet<WorkflowStageVersion> WorkflowStageVersions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppControl>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("AppControl", tb => tb.HasComment(""));

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

        modelBuilder.Entity<Bundle>(entity =>
        {
            entity.ToTable("Bundle", "cts");

            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .IsUnicode(false);
        });

        modelBuilder.Entity<BundleCompetency>(entity =>
        {
            entity.ToTable("BundleCompetency", "cts");

            entity.HasOne(d => d.BundleCompetencyGroup).WithMany(p => p.BundleCompetencies)
                .HasForeignKey(d => d.BundleCompetencyGroupId)
                .HasConstraintName("FK_BundleCompetency_BundleCompetencyGroupId");

            entity.HasOne(d => d.Bundle).WithMany(p => p.BundleCompetencies)
                .HasForeignKey(d => d.BundleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BundleCompetency_Bundle");

            entity.HasOne(d => d.Competency).WithMany(p => p.BundleCompetencies)
                .HasForeignKey(d => d.CompetencyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BundleCompetency_Competency");

            entity.HasOne(d => d.Role).WithMany(p => p.BundleCompetencies)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_BundleCompetency_Role");
        });

        modelBuilder.Entity<BundleCompetencyGroup>(entity =>
        {
            entity.HasKey(e => e.BundleCompetencyGroupId).HasName("PK_BundleCompetencyGroupId");

            entity.ToTable("BundleCompetencyGroup", "cts");

            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.HasOne(d => d.Bundle).WithMany(p => p.BundleCompetencyGroups)
                .HasForeignKey(d => d.BundleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BundleCompetencyGroupId_Bundle");
        });

        modelBuilder.Entity<BundleCompetencyLevel>(entity =>
        {
            entity.HasKey(e => e.BundleCompetencyLevelId).HasName("PK_BundleLevel");

            entity.ToTable("BundleCompetencyLevel", "cts");

            entity.HasOne(d => d.BundleCompetency).WithMany(p => p.BundleCompetencyLevels)
                .HasForeignKey(d => d.BundleCompetencyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BundleLevel_BundleCompetency");

            entity.HasOne(d => d.Level).WithMany(p => p.BundleCompetencyLevels)
                .HasForeignKey(d => d.LevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BundleLevel_Level");
        });

        modelBuilder.Entity<BundleRole>(entity =>
        {
            entity.ToTable("BundleRole", "cts");

            entity.HasOne(d => d.Bundle).WithMany(p => p.BundleRoles)
                .HasForeignKey(d => d.BundleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BundleRole_Bundle");

            entity.HasOne(d => d.Role).WithMany(p => p.BundleRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BundleRole_Role");
        });

        modelBuilder.Entity<BundleService>(entity =>
        {
            entity.ToTable("BundleService", "cts");

            entity.HasOne(d => d.Bundle).WithMany(p => p.BundleServices)
                .HasForeignKey(d => d.BundleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BundleService_Bundle");
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

        modelBuilder.Entity<ClassYearLeftReason>(entity =>
        {
            entity.ToTable("ClassYearLeftReason", "students");

            entity.Property(e => e.Reason)
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<CompTemp>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("comp_temp", "cts");

            entity.Property(e => e.CompetencyId).ValueGeneratedOnAdd();
            entity.Property(e => e.Description).IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Number)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Competency>(entity =>
        {
            entity.ToTable("Competency", "cts");

            entity.Property(e => e.Description).IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Number)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Domain).WithMany(p => p.Competencies)
                .HasForeignKey(d => d.DomainId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Competency_Domain");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK_Competency_Competency");
        });

        modelBuilder.Entity<CompetencyMapping>(entity =>
        {
            entity.ToTable("CompetencyMapping", "cts");

            entity.HasOne(d => d.Competency).WithMany(p => p.CompetencyMappings)
                .HasForeignKey(d => d.CompetencyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CompetencyMapping_CompetencyMapping");
        });

        modelBuilder.Entity<CompetencyOutcome>(entity =>
        {
            entity.ToTable("CompetencyOutcome", "cts");

            entity.HasOne(d => d.Competency).WithMany(p => p.CompetencyOutcomes)
                .HasForeignKey(d => d.CompetencyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CompetencyOutcome_Competency");
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
            entity.Property(e => e.ContentBlock1)
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

        modelBuilder.Entity<CourseCompetency>(entity =>
        {
            entity.ToTable("CourseCompetency", "cts");

            entity.HasOne(d => d.Competency).WithMany(p => p.CourseCompetencies)
                .HasForeignKey(d => d.CompetencyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CourseCompetency_Competency");

            entity.HasOne(d => d.Level).WithMany(p => p.CourseCompetencies)
                .HasForeignKey(d => d.LevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CourseCompetency_Level");
        });

        modelBuilder.Entity<CourseRole>(entity =>
        {
            entity.ToTable("CourseRole", "cts");

            entity.HasOne(d => d.Role).WithMany(p => p.CourseRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CourseRole_Role");
        });

        modelBuilder.Entity<CtsAudit>(entity =>
        {
            entity.ToTable("CtsAudit", "cts");

            entity.Property(e => e.Action)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Area)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Detail).IsUnicode(false);
            entity.Property(e => e.Timestamp).HasColumnType("datetime");

            entity.HasOne(d => d.Encounter).WithMany(p => p.CtsAudits)
                .HasForeignKey(d => d.EncounterId)
                .HasConstraintName("FK_CtsAudit_Encounter");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.CtsAudits)
                .HasForeignKey(d => d.ModifiedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CtsAudit_Person");
        });

        modelBuilder.Entity<DeviceCheckIn>(entity =>
        {
            entity.HasKey(e => e.DeviceCheckInId).HasName("PK_deviceCheckIn");

            entity.ToTable("DeviceCheckIn");

            entity.Property(e => e.DeviceCheckInId).HasColumnName("deviceCheckInId");
            entity.Property(e => e.Brand)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("brand");
            entity.Property(e => e.BrandOther)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("brandOther");
            entity.Property(e => e.ClientEmail)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("clientEmail");
            entity.Property(e => e.ClientName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("clientName");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("createdBy");
            entity.Property(e => e.DeviceType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("deviceType");
            entity.Property(e => e.DeviceTypeOther)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("deviceTypeOther");
            entity.Property(e => e.Dropoff)
                .HasColumnType("datetime")
                .HasColumnName("dropoff");
            entity.Property(e => e.Return)
                .HasColumnType("datetime")
                .HasColumnName("return");
            entity.Property(e => e.ReturnedBy)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("returnedBy");
            entity.Property(e => e.Serial)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("serial");
            entity.Property(e => e.ServiceDeskId).HasColumnName("serviceDeskId");
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TechnicianEmail)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("technicianEmail");
            entity.Property(e => e.TechnicianName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("technicianName");
        });

        modelBuilder.Entity<Domain>(entity =>
        {
            entity.ToTable("Domain", "cts");

            entity.Property(e => e.Description)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .IsUnicode(false);
        });

        modelBuilder.Entity<EmergencyContact>(entity =>
        {
            entity.HasKey(e => e.EmContactId);

            entity.ToTable("emergencyContact", "students");

            entity.Property(e => e.EmContactId).HasColumnName("emContactId");
            entity.Property(e => e.Cell)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("cell");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.Home)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("home");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.Relationship)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("relationship");
            entity.Property(e => e.StdContactId).HasColumnName("stdContactId");
            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("type");
            entity.Property(e => e.Work)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("work");

            entity.HasOne(d => d.StdContact).WithMany(p => p.EmergencyContacts)
                .HasForeignKey(d => d.StdContactId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_emergencyContact_studentContact");
        });

        modelBuilder.Entity<Encounter>(entity =>
        {
            entity.ToTable("Encounter", "cts");

            entity.HasIndex(e => e.EncounterDate, "IX_Encounter_Date");

            entity.HasIndex(e => e.EncounterType, "IX_Encounter_Type");

            entity.Property(e => e.Comment).IsUnicode(false);
            entity.Property(e => e.Diagnosis)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.EditComment).IsUnicode(false);
            entity.Property(e => e.EncounterDate).HasColumnType("date");
            entity.Property(e => e.EnteredOn).HasColumnType("datetime");
            entity.Property(e => e.PresentingComplaint)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.StudentLevel)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.VisitNumber)
                .HasMaxLength(6)
                .IsUnicode(false);

            entity.HasOne(d => d.Clinician).WithMany(p => p.EncounterClinicians)
                .HasForeignKey(d => d.ClinicianId)
                .HasConstraintName("FK_Encounter_ClinicianId");

            entity.HasOne(d => d.EnteredByNavigation).WithMany(p => p.EncounterEnteredByNavigations)
                .HasForeignKey(d => d.EnteredBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Encounter_EnteredBy");

            entity.HasOne(d => d.Epa).WithMany(p => p.Encounters)
                .HasForeignKey(d => d.EpaId)
                .HasConstraintName("FK_Encounter_Epa");

            entity.HasOne(d => d.Level).WithMany(p => p.Encounters)
                .HasForeignKey(d => d.LevelId)
                .HasConstraintName("FK_Encounter_Level");

            entity.HasOne(d => d.Patient).WithMany(p => p.Encounters)
                .HasForeignKey(d => d.PatientId)
                .HasConstraintName("FK_Encounter_Patient");

            entity.HasOne(d => d.StudentUser).WithMany(p => p.EncounterStudentUsers)
                .HasForeignKey(d => d.StudentUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Encounter_Student");
        });

        modelBuilder.Entity<EncounterInstructor>(entity =>
        {
            entity.ToTable("EncounterInstructor", "cts");

            entity.Property(e => e.EncounterInstructorId).HasColumnName("EncounterInstructorID");

            entity.HasOne(d => d.Encounter).WithMany(p => p.EncounterInstructors)
                .HasForeignKey(d => d.EncounterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EncounterInstructor_Encounter");

            entity.HasOne(d => d.Instructor).WithMany(p => p.EncounterInstructors)
                .HasForeignKey(d => d.InstructorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EncounterInstructor_Person");
        });

        modelBuilder.Entity<Epa>(entity =>
        {
            entity.ToTable("Epa", "cts");

            entity.Property(e => e.Description).IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<EpaService>(entity =>
        {
            entity.ToTable("EpaService", "cts");

            entity.HasOne(d => d.Epa).WithMany(p => p.EpaServices)
                .HasForeignKey(d => d.EpaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EpaService_Epa");
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

        modelBuilder.Entity<Viper.Models.EquipmentLoan.File>(entity =>
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

        modelBuilder.Entity<Level>(entity =>
        {
            entity.ToTable("Level", "cts");

            entity.Property(e => e.Description).IsUnicode(false);
            entity.Property(e => e.LevelName)
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<MilestoneLevel>(entity =>
        {
            entity.ToTable("MilestoneLevel", "cts");

            entity.Property(e => e.Description).IsUnicode(false);

            entity.HasOne(d => d.Bundle).WithMany(p => p.MilestoneLevels)
                .HasForeignKey(d => d.BundleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MilestoneLevel_Bundle");

            entity.HasOne(d => d.Level).WithMany(p => p.MilestoneLevels)
                .HasForeignKey(d => d.LevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MilestoneLevel_MilestoneLevel");
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

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patient", "cts");

            entity.Property(e => e.PatientId).ValueGeneratedNever();
            entity.Property(e => e.Gender)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PatientName)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Species)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Person>(entity =>
        {
            entity.HasKey(e => e.PersonId).HasName("PK_aaudUser");

            entity.ToTable("Person", "users");

            entity.HasIndex(e => e.Current, "IX_Person_Current");

            entity.HasIndex(e => e.EmployeeId, "IX_Person_EmployeeId");

            entity.HasIndex(e => e.FirstName, "IX_Person_FirstName");

            entity.HasIndex(e => e.Future, "IX_Person_Future");

            entity.HasIndex(e => e.IamId, "IX_Person_IamId");

            entity.HasIndex(e => e.LastName, "IX_Person_LastName");

            entity.HasIndex(e => e.LoginId, "IX_Person_LoginId");

            entity.HasIndex(e => e.MothraId, "IX_Person_MothraId");

            entity.HasIndex(e => e.Pidm, "IX_Person_Pidm");

            entity.Property(e => e.PersonId).ValueGeneratedNever();
            entity.Property(e => e.Added).HasColumnType("datetime");
            entity.Property(e => e.ClientId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Current).HasComputedColumnSql("(case when [CurrentStudent]=(1) OR [CurrentEmployee]=(1) then (1) else (0) end)", false);
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.FullName)
                .HasMaxLength(91)
                .IsUnicode(false)
                .HasComputedColumnSql("(([FirstName]+' ')+[LastName])", false);
            entity.Property(e => e.Future).HasComputedColumnSql("(case when [FutureStudent]=(1) OR [FutureEmployee]=(1) then (1) else (0) end)", false);
            entity.Property(e => e.IamId)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Inactivated).HasColumnType("datetime");
            entity.Property(e => e.LastName)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.LoginId)
                .HasMaxLength(18)
                .IsUnicode(false);
            entity.Property(e => e.MailId)
                .HasMaxLength(32)
                .IsUnicode(false);
            entity.Property(e => e.MiddleName)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.MothraId)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.Pidm)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.PpsId)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.SpridenId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.UnexId)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.VmcasId)
                .HasMaxLength(15)
                .IsUnicode(false);
        });

        modelBuilder.Entity<QrtzBlobTrigger>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.TriggerName, e.TriggerGroup });

            entity.ToTable("QRTZ_BLOB_TRIGGERS");

            entity.Property(e => e.SchedName)
                .HasMaxLength(120)
                .HasColumnName("SCHED_NAME");
            entity.Property(e => e.TriggerName)
                .HasMaxLength(150)
                .HasColumnName("TRIGGER_NAME");
            entity.Property(e => e.TriggerGroup)
                .HasMaxLength(150)
                .HasColumnName("TRIGGER_GROUP");
            entity.Property(e => e.BlobData).HasColumnName("BLOB_DATA");
        });

        modelBuilder.Entity<QrtzCalendar>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.CalendarName });

            entity.ToTable("QRTZ_CALENDARS");

            entity.Property(e => e.SchedName)
                .HasMaxLength(120)
                .HasColumnName("SCHED_NAME");
            entity.Property(e => e.CalendarName)
                .HasMaxLength(200)
                .HasColumnName("CALENDAR_NAME");
            entity.Property(e => e.Calendar).HasColumnName("CALENDAR");
        });

        modelBuilder.Entity<QrtzCronTrigger>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.TriggerName, e.TriggerGroup });

            entity.ToTable("QRTZ_CRON_TRIGGERS");

            entity.Property(e => e.SchedName)
                .HasMaxLength(120)
                .HasColumnName("SCHED_NAME");
            entity.Property(e => e.TriggerName)
                .HasMaxLength(150)
                .HasColumnName("TRIGGER_NAME");
            entity.Property(e => e.TriggerGroup)
                .HasMaxLength(150)
                .HasColumnName("TRIGGER_GROUP");
            entity.Property(e => e.CronExpression)
                .HasMaxLength(120)
                .HasColumnName("CRON_EXPRESSION");
            entity.Property(e => e.TimeZoneId)
                .HasMaxLength(80)
                .HasColumnName("TIME_ZONE_ID");

            entity.HasOne(d => d.QrtzTrigger).WithOne(p => p.QrtzCronTrigger)
                .HasForeignKey<QrtzCronTrigger>(d => new { d.SchedName, d.TriggerName, d.TriggerGroup })
                .HasConstraintName("FK_QRTZ_CRON_TRIGGERS_QRTZ_TRIGGERS");
        });

        modelBuilder.Entity<QrtzFiredTrigger>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.EntryId });

            entity.ToTable("QRTZ_FIRED_TRIGGERS");

            entity.HasIndex(e => new { e.SchedName, e.JobGroup, e.JobName }, "IDX_QRTZ_FT_G_J");

            entity.HasIndex(e => new { e.SchedName, e.TriggerGroup, e.TriggerName }, "IDX_QRTZ_FT_G_T");

            entity.HasIndex(e => new { e.SchedName, e.InstanceName, e.RequestsRecovery }, "IDX_QRTZ_FT_INST_JOB_REQ_RCVRY");

            entity.Property(e => e.SchedName)
                .HasMaxLength(120)
                .HasColumnName("SCHED_NAME");
            entity.Property(e => e.EntryId)
                .HasMaxLength(140)
                .HasColumnName("ENTRY_ID");
            entity.Property(e => e.FiredTime).HasColumnName("FIRED_TIME");
            entity.Property(e => e.InstanceName)
                .HasMaxLength(200)
                .HasColumnName("INSTANCE_NAME");
            entity.Property(e => e.IsNonconcurrent).HasColumnName("IS_NONCONCURRENT");
            entity.Property(e => e.JobGroup)
                .HasMaxLength(150)
                .HasColumnName("JOB_GROUP");
            entity.Property(e => e.JobName)
                .HasMaxLength(150)
                .HasColumnName("JOB_NAME");
            entity.Property(e => e.Priority).HasColumnName("PRIORITY");
            entity.Property(e => e.RequestsRecovery).HasColumnName("REQUESTS_RECOVERY");
            entity.Property(e => e.SchedTime).HasColumnName("SCHED_TIME");
            entity.Property(e => e.State)
                .HasMaxLength(16)
                .HasColumnName("STATE");
            entity.Property(e => e.TriggerGroup)
                .HasMaxLength(150)
                .HasColumnName("TRIGGER_GROUP");
            entity.Property(e => e.TriggerName)
                .HasMaxLength(150)
                .HasColumnName("TRIGGER_NAME");
        });

        modelBuilder.Entity<QrtzJobDetail>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.JobName, e.JobGroup });

            entity.ToTable("QRTZ_JOB_DETAILS");

            entity.Property(e => e.SchedName)
                .HasMaxLength(120)
                .HasColumnName("SCHED_NAME");
            entity.Property(e => e.JobName)
                .HasMaxLength(150)
                .HasColumnName("JOB_NAME");
            entity.Property(e => e.JobGroup)
                .HasMaxLength(150)
                .HasColumnName("JOB_GROUP");
            entity.Property(e => e.Description)
                .HasMaxLength(250)
                .HasColumnName("DESCRIPTION");
            entity.Property(e => e.IsDurable).HasColumnName("IS_DURABLE");
            entity.Property(e => e.IsNonconcurrent).HasColumnName("IS_NONCONCURRENT");
            entity.Property(e => e.IsUpdateData).HasColumnName("IS_UPDATE_DATA");
            entity.Property(e => e.JobClassName)
                .HasMaxLength(250)
                .HasColumnName("JOB_CLASS_NAME");
            entity.Property(e => e.JobData).HasColumnName("JOB_DATA");
            entity.Property(e => e.RequestsRecovery).HasColumnName("REQUESTS_RECOVERY");
        });

        modelBuilder.Entity<QrtzLock>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.LockName });

            entity.ToTable("QRTZ_LOCKS");

            entity.Property(e => e.SchedName)
                .HasMaxLength(120)
                .HasColumnName("SCHED_NAME");
            entity.Property(e => e.LockName)
                .HasMaxLength(40)
                .HasColumnName("LOCK_NAME");
        });

        modelBuilder.Entity<QrtzPausedTriggerGrp>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.TriggerGroup });

            entity.ToTable("QRTZ_PAUSED_TRIGGER_GRPS");

            entity.Property(e => e.SchedName)
                .HasMaxLength(120)
                .HasColumnName("SCHED_NAME");
            entity.Property(e => e.TriggerGroup)
                .HasMaxLength(150)
                .HasColumnName("TRIGGER_GROUP");
        });

        modelBuilder.Entity<QrtzSchedulerState>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.InstanceName });

            entity.ToTable("QRTZ_SCHEDULER_STATE");

            entity.Property(e => e.SchedName)
                .HasMaxLength(120)
                .HasColumnName("SCHED_NAME");
            entity.Property(e => e.InstanceName)
                .HasMaxLength(200)
                .HasColumnName("INSTANCE_NAME");
            entity.Property(e => e.CheckinInterval).HasColumnName("CHECKIN_INTERVAL");
            entity.Property(e => e.LastCheckinTime).HasColumnName("LAST_CHECKIN_TIME");
        });

        modelBuilder.Entity<QrtzSimpleTrigger>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.TriggerName, e.TriggerGroup });

            entity.ToTable("QRTZ_SIMPLE_TRIGGERS");

            entity.Property(e => e.SchedName)
                .HasMaxLength(120)
                .HasColumnName("SCHED_NAME");
            entity.Property(e => e.TriggerName)
                .HasMaxLength(150)
                .HasColumnName("TRIGGER_NAME");
            entity.Property(e => e.TriggerGroup)
                .HasMaxLength(150)
                .HasColumnName("TRIGGER_GROUP");
            entity.Property(e => e.RepeatCount).HasColumnName("REPEAT_COUNT");
            entity.Property(e => e.RepeatInterval).HasColumnName("REPEAT_INTERVAL");
            entity.Property(e => e.TimesTriggered).HasColumnName("TIMES_TRIGGERED");

            entity.HasOne(d => d.QrtzTrigger).WithOne(p => p.QrtzSimpleTrigger)
                .HasForeignKey<QrtzSimpleTrigger>(d => new { d.SchedName, d.TriggerName, d.TriggerGroup })
                .HasConstraintName("FK_QRTZ_SIMPLE_TRIGGERS_QRTZ_TRIGGERS");
        });

        modelBuilder.Entity<QrtzSimpropTrigger>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.TriggerName, e.TriggerGroup });

            entity.ToTable("QRTZ_SIMPROP_TRIGGERS");

            entity.Property(e => e.SchedName)
                .HasMaxLength(120)
                .HasColumnName("SCHED_NAME");
            entity.Property(e => e.TriggerName)
                .HasMaxLength(150)
                .HasColumnName("TRIGGER_NAME");
            entity.Property(e => e.TriggerGroup)
                .HasMaxLength(150)
                .HasColumnName("TRIGGER_GROUP");
            entity.Property(e => e.BoolProp1).HasColumnName("BOOL_PROP_1");
            entity.Property(e => e.BoolProp2).HasColumnName("BOOL_PROP_2");
            entity.Property(e => e.DecProp1)
                .HasColumnType("numeric(13, 4)")
                .HasColumnName("DEC_PROP_1");
            entity.Property(e => e.DecProp2)
                .HasColumnType("numeric(13, 4)")
                .HasColumnName("DEC_PROP_2");
            entity.Property(e => e.IntProp1).HasColumnName("INT_PROP_1");
            entity.Property(e => e.IntProp2).HasColumnName("INT_PROP_2");
            entity.Property(e => e.LongProp1).HasColumnName("LONG_PROP_1");
            entity.Property(e => e.LongProp2).HasColumnName("LONG_PROP_2");
            entity.Property(e => e.StrProp1)
                .HasMaxLength(512)
                .HasColumnName("STR_PROP_1");
            entity.Property(e => e.StrProp2)
                .HasMaxLength(512)
                .HasColumnName("STR_PROP_2");
            entity.Property(e => e.StrProp3)
                .HasMaxLength(512)
                .HasColumnName("STR_PROP_3");
            entity.Property(e => e.TimeZoneId)
                .HasMaxLength(80)
                .HasColumnName("TIME_ZONE_ID");

            entity.HasOne(d => d.QrtzTrigger).WithOne(p => p.QrtzSimpropTrigger)
                .HasForeignKey<QrtzSimpropTrigger>(d => new { d.SchedName, d.TriggerName, d.TriggerGroup })
                .HasConstraintName("FK_QRTZ_SIMPROP_TRIGGERS_QRTZ_TRIGGERS");
        });

        modelBuilder.Entity<QrtzTrigger>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.TriggerName, e.TriggerGroup });

            entity.ToTable("QRTZ_TRIGGERS");

            entity.HasIndex(e => new { e.SchedName, e.CalendarName }, "IDX_QRTZ_T_C");

            entity.HasIndex(e => new { e.SchedName, e.JobGroup, e.JobName }, "IDX_QRTZ_T_G_J");

            entity.HasIndex(e => new { e.SchedName, e.NextFireTime }, "IDX_QRTZ_T_NEXT_FIRE_TIME");

            entity.HasIndex(e => new { e.SchedName, e.TriggerState, e.NextFireTime }, "IDX_QRTZ_T_NFT_ST");

            entity.HasIndex(e => new { e.SchedName, e.MisfireInstr, e.NextFireTime, e.TriggerState }, "IDX_QRTZ_T_NFT_ST_MISFIRE");

            entity.HasIndex(e => new { e.SchedName, e.MisfireInstr, e.NextFireTime, e.TriggerGroup, e.TriggerState }, "IDX_QRTZ_T_NFT_ST_MISFIRE_GRP");

            entity.HasIndex(e => new { e.SchedName, e.TriggerGroup, e.TriggerState }, "IDX_QRTZ_T_N_G_STATE");

            entity.HasIndex(e => new { e.SchedName, e.TriggerName, e.TriggerGroup, e.TriggerState }, "IDX_QRTZ_T_N_STATE");

            entity.HasIndex(e => new { e.SchedName, e.TriggerState }, "IDX_QRTZ_T_STATE");

            entity.Property(e => e.SchedName)
                .HasMaxLength(120)
                .HasColumnName("SCHED_NAME");
            entity.Property(e => e.TriggerName)
                .HasMaxLength(150)
                .HasColumnName("TRIGGER_NAME");
            entity.Property(e => e.TriggerGroup)
                .HasMaxLength(150)
                .HasColumnName("TRIGGER_GROUP");
            entity.Property(e => e.CalendarName)
                .HasMaxLength(200)
                .HasColumnName("CALENDAR_NAME");
            entity.Property(e => e.Description)
                .HasMaxLength(250)
                .HasColumnName("DESCRIPTION");
            entity.Property(e => e.EndTime).HasColumnName("END_TIME");
            entity.Property(e => e.JobData).HasColumnName("JOB_DATA");
            entity.Property(e => e.JobGroup)
                .HasMaxLength(150)
                .HasColumnName("JOB_GROUP");
            entity.Property(e => e.JobName)
                .HasMaxLength(150)
                .HasColumnName("JOB_NAME");
            entity.Property(e => e.MisfireInstr).HasColumnName("MISFIRE_INSTR");
            entity.Property(e => e.NextFireTime).HasColumnName("NEXT_FIRE_TIME");
            entity.Property(e => e.PrevFireTime).HasColumnName("PREV_FIRE_TIME");
            entity.Property(e => e.Priority).HasColumnName("PRIORITY");
            entity.Property(e => e.StartTime).HasColumnName("START_TIME");
            entity.Property(e => e.TriggerState)
                .HasMaxLength(16)
                .HasColumnName("TRIGGER_STATE");
            entity.Property(e => e.TriggerType)
                .HasMaxLength(8)
                .HasColumnName("TRIGGER_TYPE");

            entity.HasOne(d => d.QrtzJobDetail).WithMany(p => p.QrtzTriggers)
                .HasForeignKey(d => new { d.SchedName, d.JobName, d.JobGroup })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QRTZ_TRIGGERS_QRTZ_JOB_DETAILS");
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

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role", "cts");

            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ScheduledTask>(entity =>
        {
            entity.ToTable("ScheduledTask");

            entity.Property(e => e.ScheduledTaskId).HasColumnName("scheduledTaskId");
            entity.Property(e => e.FrequencyNum).HasColumnName("frequencyNum");
            entity.Property(e => e.FrequencyType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("frequencyType");
            entity.Property(e => e.HistoryToKeep).HasColumnName("historyToKeep");
            entity.Property(e => e.TaskName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("taskName");
            entity.Property(e => e.TaskUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("taskUrl");
        });

        modelBuilder.Entity<ScheduledTaskHistory>(entity =>
        {
            entity.ToTable("ScheduledTaskHistory");

            entity.Property(e => e.ScheduledTaskHistoryId).HasColumnName("scheduledTaskHistoryId");
            entity.Property(e => e.Errors)
                .HasColumnType("text")
                .HasColumnName("errors");
            entity.Property(e => e.HasErrors).HasColumnName("hasErrors");
            entity.Property(e => e.Messages)
                .HasColumnType("text")
                .HasColumnName("messages");
            entity.Property(e => e.ScheduledTaskId).HasColumnName("scheduledTaskId");
            entity.Property(e => e.Timestamp)
                .HasColumnType("datetime")
                .HasColumnName("timestamp");

            entity.HasOne(d => d.ScheduledTask).WithMany(p => p.ScheduledTaskHistories)
                .HasForeignKey(d => d.ScheduledTaskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScheduledTaskHistory_ScheduledTask");
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

        modelBuilder.Entity<SessionCompetency>(entity =>
        {
            entity.ToTable("SessionCompetency", "cts");

            entity.HasOne(d => d.Competency).WithMany(p => p.SessionCompetencies)
                .HasForeignKey(d => d.CompetencyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SessionCompetency_Competency");

            entity.HasOne(d => d.Level).WithMany(p => p.SessionCompetencies)
                .HasForeignKey(d => d.LevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SessionCompetency_Level");

            entity.HasOne(d => d.Role).WithMany(p => p.SessionCompetencies)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_SessionCompetency_Role");
        });

        modelBuilder.Entity<SessionTimeout>(entity =>
        {
            entity.HasKey(e => new { e.Loginid, e.Service });

            entity.ToTable("SessionTimeout");

            entity.Property(e => e.Loginid)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("loginid");
            entity.Property(e => e.Service)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("service");
            entity.Property(e => e.SessionTimeout1)
                .HasColumnType("datetime")
                .HasColumnName("sessionTimeout");
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

        modelBuilder.Entity<StudentClassYear>(entity =>
        {
            entity.ToTable("StudentClassYear", "students");

            entity.Property(e => e.Added).HasColumnType("datetime");
            entity.Property(e => e.Comment).IsUnicode(false);
            entity.Property(e => e.Updated).HasColumnType("datetime");

            entity.HasOne(d => d.AddedByNavigation).WithMany(p => p.StudentClassYearAddedByNavigations)
                .HasForeignKey(d => d.AddedBy)
                .HasConstraintName("FK_StudentClassYear_AddedBy");

            entity.HasOne(d => d.LeftReasonNavigation).WithMany(p => p.StudentClassYears)
                .HasForeignKey(d => d.LeftReason)
                .HasConstraintName("FK_StudentClassYear_ClassYearLeftReason");

            entity.HasOne(d => d.Person).WithMany(p => p.StudentClassYearPeople)
                .HasForeignKey(d => d.PersonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentClassYear_Person");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.StudentClassYearUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_StudentClassYear_UpdatedBy");
        });

        modelBuilder.Entity<StudentCompetency>(entity =>
        {
            entity.ToTable("StudentCompetency", "cts");

            entity.Property(e => e.StudentCompetencyId).ValueGeneratedOnAdd();
            entity.Property(e => e.Added).HasColumnType("datetime");
            entity.Property(e => e.Updated).HasColumnType("datetime");
            entity.Property(e => e.VerifiedTimestamp).HasColumnType("datetime");

            entity.HasOne(d => d.BundleGroup).WithMany(p => p.StudentCompetencies)
                .HasForeignKey(d => d.BundleGroupId)
                .HasConstraintName("FK_StudentCompetency_BundleCompetencyGroup");

            entity.HasOne(d => d.Bundle).WithMany(p => p.StudentCompetencies)
                .HasForeignKey(d => d.BundleId)
                .HasConstraintName("FK_StudentCompetency_Bundle");

            entity.HasOne(d => d.Competency).WithMany(p => p.StudentCompetencies)
                .HasForeignKey(d => d.CompetencyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentCompetency_Competency");

            entity.HasOne(d => d.Encounter).WithMany(p => p.StudentCompetencies)
                .HasForeignKey(d => d.EncounterId)
                .HasConstraintName("FK_StudentCompetency_Encounter");

            entity.HasOne(d => d.StudentCompetencyNavigation).WithOne(p => p.StudentCompetency)
                .HasForeignKey<StudentCompetency>(d => d.StudentCompetencyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentCompetency_Level");

            entity.HasOne(d => d.StudentUser).WithMany(p => p.StudentCompetencyStudentUsers)
                .HasForeignKey(d => d.StudentUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentCompetency_Student");

            entity.HasOne(d => d.VerifiedByNavigation).WithMany(p => p.StudentCompetencyVerifiedByNavigations)
                .HasForeignKey(d => d.VerifiedBy)
                .HasConstraintName("FK_StudentCompetency_VerifiedPerson");
        });

        modelBuilder.Entity<StudentContact>(entity =>
        {
            entity.HasKey(e => e.StdContactId);

            entity.ToTable("studentContact", "students");

            entity.Property(e => e.StdContactId).HasColumnName("stdContactId");
            entity.Property(e => e.Address)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("address");
            entity.Property(e => e.Cell)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("cell");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("city");
            entity.Property(e => e.ContactPermanent).HasColumnName("contactPermanent");
            entity.Property(e => e.Home)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("home");
            entity.Property(e => e.LastUpdated)
                .HasColumnType("datetime")
                .HasColumnName("lastUpdated");
            entity.Property(e => e.PersonId).HasColumnName("personId");
            entity.Property(e => e.Pidm).HasColumnName("pidm");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("updatedBy");
            entity.Property(e => e.Zip)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("zip");

            entity.HasOne(d => d.Person).WithMany(p => p.StudentContacts)
                .HasForeignKey(d => d.PersonId)
                .HasConstraintName("FK_studentContact_Person");
        });

        modelBuilder.Entity<StudentEpa>(entity =>
        {
            entity.ToTable("StudentEpa", "cts");

            entity.Property(e => e.Comment).IsUnicode(false);

            entity.HasOne(d => d.Encounter).WithMany(p => p.StudentEpas)
                .HasForeignKey(d => d.EncounterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentEpa_Encounter");

            entity.HasOne(d => d.Epa).WithMany(p => p.StudentEpas)
                .HasForeignKey(d => d.EpaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentEpa_Epa");

            entity.HasOne(d => d.Level).WithMany(p => p.StudentEpas)
                .HasForeignKey(d => d.LevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentEpa_Level");
        });

        modelBuilder.Entity<Viper.Models.EquipmentLoan.System>(entity =>
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

        modelBuilder.Entity<VwCourse>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwCourse", "cts");

            entity.Property(e => e.AcademicYear)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.CourseNum)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Crn)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Description)
                .HasMaxLength(1500)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Title)
                .HasMaxLength(150)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwCourseSessionOffering>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwCourseSessionOffering", "cts");

            entity.Property(e => e.Academicyear)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("academicyear");
            entity.Property(e => e.Blocktype)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("blocktype");
            entity.Property(e => e.CanvasCourseId).HasColumnName("canvasCourseID");
            entity.Property(e => e.CanvasEventId).HasColumnName("canvasEventID");
            entity.Property(e => e.Courseid).HasColumnName("courseid");
            entity.Property(e => e.Createpersonid).HasColumnName("createpersonid");
            entity.Property(e => e.Crn)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("crn");
            entity.Property(e => e.Equipment)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("equipment");
            entity.Property(e => e.Fromdate)
                .HasColumnType("smalldatetime")
                .HasColumnName("fromdate");
            entity.Property(e => e.Fromtime)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("fromtime");
            entity.Property(e => e.Keyconcept)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("keyconcept");
            entity.Property(e => e.MediasiteLive).HasColumnName("mediasiteLive");
            entity.Property(e => e.MediasitePresentation)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("mediasitePresentation");
            entity.Property(e => e.MediasiteSchedule)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("mediasiteSchedule");
            entity.Property(e => e.MediasiteTemplate)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("mediasiteTemplate");
            entity.Property(e => e.Modifydate)
                .HasColumnType("smalldatetime")
                .HasColumnName("modifydate");
            entity.Property(e => e.Modifypersonid).HasColumnName("modifypersonid");
            entity.Property(e => e.Notes)
                .HasMaxLength(1500)
                .IsUnicode(false)
                .HasColumnName("notes");
            entity.Property(e => e.OfferingNotes)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("offeringNotes");
            entity.Property(e => e.PaceOrder).HasColumnName("pace_order");
            entity.Property(e => e.ReadingRecommended)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("reading_recommended");
            entity.Property(e => e.ReadingRequired)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("reading_required");
            entity.Property(e => e.ReadingSessionmaterial)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("reading_sessionmaterial");
            entity.Property(e => e.Room)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("room");
            entity.Property(e => e.SeqNumb)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("seqNumb");
            entity.Property(e => e.Sessionid).HasColumnName("sessionid");
            entity.Property(e => e.Sessiontype)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("sessiontype");
            entity.Property(e => e.Ssacoursenum)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ssacoursenum");
            entity.Property(e => e.Studentgroup)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("studentgroup");
            entity.Property(e => e.Supplemental).HasColumnName("supplemental");
            entity.Property(e => e.SvmBlockId).HasColumnName("SVM_blockID");
            entity.Property(e => e.Thrudate)
                .HasColumnType("smalldatetime")
                .HasColumnName("thrudate");
            entity.Property(e => e.Thrutime)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("thrutime");
            entity.Property(e => e.Title)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("title");
            entity.Property(e => e.TypeOrder).HasColumnName("type_order");
            entity.Property(e => e.Vocabulary)
                .HasMaxLength(750)
                .IsUnicode(false)
                .HasColumnName("vocabulary");
        });

        modelBuilder.Entity<VwCourseSessionOffering1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwCourseSessionOffering");

            entity.Property(e => e.Academicyear)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("academicyear");
            entity.Property(e => e.Blocktype)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("blocktype");
            entity.Property(e => e.CanvasCourseId).HasColumnName("canvasCourseID");
            entity.Property(e => e.CanvasEventId).HasColumnName("canvasEventID");
            entity.Property(e => e.Courseid).HasColumnName("courseid");
            entity.Property(e => e.Createpersonid).HasColumnName("createpersonid");
            entity.Property(e => e.Crn)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("crn");
            entity.Property(e => e.Equipment)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("equipment");
            entity.Property(e => e.Fromdate)
                .HasColumnType("smalldatetime")
                .HasColumnName("fromdate");
            entity.Property(e => e.Fromtime)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("fromtime");
            entity.Property(e => e.Keyconcept)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("keyconcept");
            entity.Property(e => e.MediasiteLive).HasColumnName("mediasiteLive");
            entity.Property(e => e.MediasitePresentation)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("mediasitePresentation");
            entity.Property(e => e.MediasiteSchedule)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("mediasiteSchedule");
            entity.Property(e => e.MediasiteTemplate)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("mediasiteTemplate");
            entity.Property(e => e.Modifydate)
                .HasColumnType("smalldatetime")
                .HasColumnName("modifydate");
            entity.Property(e => e.Modifypersonid).HasColumnName("modifypersonid");
            entity.Property(e => e.Notes)
                .HasMaxLength(1500)
                .IsUnicode(false)
                .HasColumnName("notes");
            entity.Property(e => e.OfferingNotes)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("offeringNotes");
            entity.Property(e => e.PaceOrder).HasColumnName("pace_order");
            entity.Property(e => e.ReadingRecommended)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("reading_recommended");
            entity.Property(e => e.ReadingRequired)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("reading_required");
            entity.Property(e => e.ReadingSessionmaterial)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("reading_sessionmaterial");
            entity.Property(e => e.Room)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("room");
            entity.Property(e => e.SeqNumb)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("seqNumb");
            entity.Property(e => e.Sessionid).HasColumnName("sessionid");
            entity.Property(e => e.Sessiontype)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("sessiontype");
            entity.Property(e => e.Ssacoursenum)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ssacoursenum");
            entity.Property(e => e.Studentgroup)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("studentgroup");
            entity.Property(e => e.Supplemental).HasColumnName("supplemental");
            entity.Property(e => e.SvmBlockId).HasColumnName("SVM_blockID");
            entity.Property(e => e.Thrudate)
                .HasColumnType("smalldatetime")
                .HasColumnName("thrudate");
            entity.Property(e => e.Thrutime)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("thrutime");
            entity.Property(e => e.Title)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("title");
            entity.Property(e => e.TypeOrder).HasColumnName("type_order");
            entity.Property(e => e.Vocabulary)
                .HasMaxLength(750)
                .IsUnicode(false)
                .HasColumnName("vocabulary");
        });

        modelBuilder.Entity<VwDvmstudent>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwDVMStudents", "cts");

            entity.Property(e => e.IdsLoginId)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("ids_loginId");
            entity.Property(e => e.IdsMailid)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("ids_mailid");
            entity.Property(e => e.IdsMothraId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_mothraId");
            entity.Property(e => e.IdsPidm).HasColumnName("ids_pidm");
            entity.Property(e => e.PersonFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_first_name");
            entity.Property(e => e.PersonLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("person_last_name");
            entity.Property(e => e.PersonMiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_middle_name");
            entity.Property(e => e.StudentsClassLevel)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("students_class_level");
            entity.Property(e => e.StudentsTermCode).HasColumnName("students_term_code");
        });

        modelBuilder.Entity<VwEquipmentLoan>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwEquipmentLoans");

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
        });

        modelBuilder.Entity<VwEvaluateesByInstance>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwEvaluateesByInstances", "eval");

            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.InstructorMothraId)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.MothraId)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.Rotation)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Service)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.StartDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<VwInstance>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwInstances", "eval");

            entity.Property(e => e.InstanceDueDate).HasColumnType("datetime");
            entity.Property(e => e.InstanceId).ValueGeneratedOnAdd();
            entity.Property(e => e.InstanceMode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InstanceMothraId)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.InstanceStartWeek).HasColumnType("datetime");
            entity.Property(e => e.InstanceStatus)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwInstructorSchedule>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwInstructorSchedule", "cts");

            entity.Property(e => e.Abbreviation)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CrseNumb)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.DateEnd).HasColumnType("datetime");
            entity.Property(e => e.DateStart).HasColumnType("datetime");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.FullName)
                .HasMaxLength(91)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.MailId)
                .HasMaxLength(32)
                .IsUnicode(false);
            entity.Property(e => e.MiddleName)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.MothraId)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.RotationName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ServiceName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SubjCode)
                .HasMaxLength(3)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwKey>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwKeys");

            entity.Property(e => e.AccessDescription)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.AdHocName)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.AssignedTo)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.CutNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DispositionBy)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.DispositionDate).HasColumnType("datetime");
            entity.Property(e => e.DueDate).HasColumnType("datetime");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.IssuedBy)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.IssuedBy1)
                .HasMaxLength(91)
                .IsUnicode(false)
                .HasColumnName("issued_by");
            entity.Property(e => e.IssuedDate).HasColumnType("datetime");
            entity.Property(e => e.KeyId).HasColumnName("KeyID");
            entity.Property(e => e.KeyNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ManagedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Notes)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.RequestDate).HasColumnType("datetime");
            entity.Property(e => e.RequestedBy)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.RestrictedContact)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<VwLegacyCompetency>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwLegacyCompetencies", "cts");

            entity.Property(e => e.DvmCompetencyId).ValueGeneratedOnAdd();
            entity.Property(e => e.DvmCompetencyName)
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwLegacySessionCompetency>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwLegacySessionCompetencies", "cts");

            entity.Property(e => e.AcademicYear)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.CourseTitle)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.DvmCompetencyName)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.DvmLevelName)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DvmRoleName)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MultiRole)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.SessionDescription)
                .HasMaxLength(1500)
                .IsUnicode(false);
            entity.Property(e => e.SessionStatus)
                .HasMaxLength(31)
                .IsUnicode(false);
            entity.Property(e => e.SessionTitle)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.SessionTypeDescription)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Sessiontype)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("sessiontype");
        });

        modelBuilder.Entity<VwPerson>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwPerson", "personnel");

            entity.Property(e => e.Birthdate).HasColumnName("BIRTHDATE");
            entity.Property(e => e.CitizenshipStatus)
                .HasMaxLength(30)
                .HasColumnName("CITIZENSHIP_STATUS");
            entity.Property(e => e.CitizenshipStatusCode)
                .HasMaxLength(1)
                .HasColumnName("CITIZENSHIP_STATUS_CODE");
            entity.Property(e => e.Country)
                .HasMaxLength(30)
                .HasColumnName("COUNTRY");
            entity.Property(e => e.CountryCode)
                .HasMaxLength(3)
                .HasColumnName("COUNTRY_CODE");
            entity.Property(e => e.EmailAddr)
                .HasMaxLength(70)
                .HasColumnName("EMAIL_ADDR");
            entity.Property(e => e.EmpAcdmcFederationFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FEDERATION_FLG");
            entity.Property(e => e.EmpAcdmcFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FLG");
            entity.Property(e => e.EmpAcdmcSenateFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_SENATE_FLG");
            entity.Property(e => e.EmpAcdmcStdtFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_STDT_FLG");
            entity.Property(e => e.EmpFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_FACULTY_FLG");
            entity.Property(e => e.EmpLadderRankFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_LADDER_RANK_FLG");
            entity.Property(e => e.EmpMgrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MGR_FLG");
            entity.Property(e => e.EmpMspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_FLG");
            entity.Property(e => e.EmpMspCareerPartialyrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_PARTIALYR_FLG");
            entity.Property(e => e.EmpMspCasualFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CASUAL_FLG");
            entity.Property(e => e.EmpMspCntrctFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CNTRCT_FLG");
            entity.Property(e => e.EmpMspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_FLG");
            entity.Property(e => e.EmpMspSeniorMgmtFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_SENIOR_MGMT_FLG");
            entity.Property(e => e.EmpSspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_FLG");
            entity.Property(e => e.EmpSspCareerPartialyrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_PARTIALYR_FLG");
            entity.Property(e => e.EmpSspCasualFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CASUAL_FLG");
            entity.Property(e => e.EmpSspCasualRestrictedFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CASUAL_RESTRICTED_FLG");
            entity.Property(e => e.EmpSspCntrctFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CNTRCT_FLG");
            entity.Property(e => e.EmpSspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_FLG");
            entity.Property(e => e.EmpSspFloaterFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_FLOATER_FLG");
            entity.Property(e => e.EmpSspPerDiemFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_PER_DIEM_FLG");
            entity.Property(e => e.EmpSupvrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SUPVR_FLG");
            entity.Property(e => e.EmpTeachingFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_TEACHING_FACULTY_FLG");
            entity.Property(e => e.EmpWosempFlg)
                .HasMaxLength(11)
                .HasColumnName("EMP_WOSEMP_FLG");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .HasColumnName("FIRST_NAME");
            entity.Property(e => e.LastName)
                .HasMaxLength(30)
                .HasColumnName("LAST_NAME");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(30)
                .HasColumnName("MIDDLE_NAME");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("NAME");
            entity.Property(e => e.OrigHireDt).HasColumnName("ORIG_HIRE_DT");
            entity.Property(e => e.PpsId)
                .HasMaxLength(254)
                .HasColumnName("PPS_ID");
        });

        modelBuilder.Entity<VwPersonJobPosition>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwPersonJobPosition", "personnel");

            entity.Property(e => e.Action)
                .HasMaxLength(3)
                .HasColumnName("ACTION");
            entity.Property(e => e.ActionDt).HasColumnName("ACTION_DT");
            entity.Property(e => e.AnnualRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("ANNUAL_RT");
            entity.Property(e => e.Birthdate).HasColumnName("BIRTHDATE");
            entity.Property(e => e.CitizenshipStatus)
                .HasMaxLength(30)
                .HasColumnName("CITIZENSHIP_STATUS");
            entity.Property(e => e.CitizenshipStatusCode)
                .HasMaxLength(1)
                .HasColumnName("CITIZENSHIP_STATUS_CODE");
            entity.Property(e => e.ClassIndc)
                .HasMaxLength(1)
                .HasColumnName("CLASS_INDC");
            entity.Property(e => e.Country)
                .HasMaxLength(30)
                .HasColumnName("COUNTRY");
            entity.Property(e => e.CountryCode)
                .HasMaxLength(3)
                .HasColumnName("COUNTRY_CODE");
            entity.Property(e => e.DeptCd)
                .HasMaxLength(6)
                .HasColumnName("DEPT_CD");
            entity.Property(e => e.DeptDesc)
                .HasMaxLength(40)
                .HasColumnName("DEPT_DESC");
            entity.Property(e => e.DeptShortDesc)
                .HasMaxLength(15)
                .HasColumnName("DEPT_SHORT_DESC");
            entity.Property(e => e.Deptid)
                .HasMaxLength(10)
                .HasColumnName("DEPTID");
            entity.Property(e => e.EffDateActive).HasColumnName("EFF_DATE_ACTIVE");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.Effseq).HasColumnName("EFFSEQ");
            entity.Property(e => e.EmailAddr)
                .HasMaxLength(70)
                .HasColumnName("EMAIL_ADDR");
            entity.Property(e => e.EmpAcdmcFederationFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FEDERATION_FLG");
            entity.Property(e => e.EmpAcdmcFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_FLG");
            entity.Property(e => e.EmpAcdmcSenateFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_ACDMC_SENATE_FLG");
            entity.Property(e => e.EmpFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_FACULTY_FLG");
            entity.Property(e => e.EmpLadderRankFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_LADDER_RANK_FLG");
            entity.Property(e => e.EmpMspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_FLG");
            entity.Property(e => e.EmpMspCareerPartialyrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CAREER_PARTIALYR_FLG");
            entity.Property(e => e.EmpMspCasualFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CASUAL_FLG");
            entity.Property(e => e.EmpMspCntrctFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_CNTRCT_FLG");
            entity.Property(e => e.EmpMspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_FLG");
            entity.Property(e => e.EmpMspSeniorMgmtFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_MSP_SENIOR_MGMT_FLG");
            entity.Property(e => e.EmpSspCareerFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_FLG");
            entity.Property(e => e.EmpSspCareerPartialyrFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CAREER_PARTIALYR_FLG");
            entity.Property(e => e.EmpSspCasualFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CASUAL_FLG");
            entity.Property(e => e.EmpSspCasualRestrictedFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CASUAL_RESTRICTED_FLG");
            entity.Property(e => e.EmpSspCntrctFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_CNTRCT_FLG");
            entity.Property(e => e.EmpSspFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_FLG");
            entity.Property(e => e.EmpSspPerDiemFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_SSP_PER_DIEM_FLG");
            entity.Property(e => e.EmpTeachingFacultyFlg)
                .HasMaxLength(1)
                .HasColumnName("EMP_TEACHING_FACULTY_FLG");
            entity.Property(e => e.EmpWosempFlg)
                .HasMaxLength(11)
                .HasColumnName("EMP_WOSEMP_FLG");
            entity.Property(e => e.EmplRcd).HasColumnName("EMPL_RCD");
            entity.Property(e => e.EmplStatus)
                .HasMaxLength(1)
                .HasColumnName("EMPL_STATUS");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.ExpectedEndDate).HasColumnName("EXPECTED_END_DATE");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .HasColumnName("FIRST_NAME");
            entity.Property(e => e.FlsaStatus)
                .HasMaxLength(1)
                .HasColumnName("FLSA_STATUS");
            entity.Property(e => e.Fte)
                .HasColumnType("numeric(7, 6)")
                .HasColumnName("FTE");
            entity.Property(e => e.Grade)
                .HasMaxLength(3)
                .HasColumnName("GRADE");
            entity.Property(e => e.HourlyRt)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("HOURLY_RT");
            entity.Property(e => e.Isprimary).HasColumnName("ISPRIMARY");
            entity.Property(e => e.JobStatus)
                .HasMaxLength(1)
                .HasColumnName("JOB_STATUS");
            entity.Property(e => e.JobStatus1)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("JobStatus");
            entity.Property(e => e.JobStatusDesc)
                .HasMaxLength(30)
                .HasColumnName("JOB_STATUS_DESC");
            entity.Property(e => e.Jobcode)
                .HasMaxLength(6)
                .HasColumnName("JOBCODE");
            entity.Property(e => e.JobcodeDesc)
                .HasMaxLength(30)
                .HasColumnName("JOBCODE_DESC");
            entity.Property(e => e.JobcodeShortDesc)
                .HasMaxLength(10)
                .HasColumnName("JOBCODE_SHORT_DESC");
            entity.Property(e => e.JobcodeUnionCode)
                .HasMaxLength(3)
                .HasColumnName("JOBCODE_UNION_CODE");
            entity.Property(e => e.Jobgroup)
                .HasMaxLength(3)
                .HasColumnName("JOBGROUP");
            entity.Property(e => e.JobgroupDesc)
                .HasMaxLength(50)
                .HasColumnName("JOBGROUP_DESC");
            entity.Property(e => e.LastName)
                .HasMaxLength(30)
                .HasColumnName("LAST_NAME");
            entity.Property(e => e.ManuallySeparated)
                .HasMaxLength(2)
                .HasColumnName("MANUALLY_SEPARATED");
            entity.Property(e => e.MonthlyRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("MONTHLY_RT");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("NAME");
            entity.Property(e => e.OrigHireDt).HasColumnName("ORIG_HIRE_DT");
            entity.Property(e => e.PositionAction)
                .HasMaxLength(3)
                .HasColumnName("POSITION_ACTION");
            entity.Property(e => e.PositionActionDt).HasColumnName("POSITION_ACTION_DT");
            entity.Property(e => e.PositionDeptDesc)
                .HasMaxLength(40)
                .HasColumnName("POSITION_DEPT_DESC");
            entity.Property(e => e.PositionDeptShortDesc)
                .HasMaxLength(15)
                .HasColumnName("POSITION_DEPT_SHORT_DESC");
            entity.Property(e => e.PositionDeptid)
                .HasMaxLength(10)
                .HasColumnName("POSITION_DEPTID");
            entity.Property(e => e.PositionDesc)
                .HasMaxLength(30)
                .HasColumnName("POSITION_DESC");
            entity.Property(e => e.PositionEffdt).HasColumnName("POSITION_EFFDT");
            entity.Property(e => e.PositionNbr)
                .HasMaxLength(8)
                .HasColumnName("POSITION_NBR");
            entity.Property(e => e.PositionShortDesc)
                .HasMaxLength(10)
                .HasColumnName("POSITION_SHORT_DESC");
            entity.Property(e => e.PositionStatus)
                .HasMaxLength(1)
                .HasColumnName("POSITION_STATUS");
            entity.Property(e => e.PpsId)
                .HasMaxLength(254)
                .HasColumnName("PPS_ID");
            entity.Property(e => e.Primaryindex)
                .HasMaxLength(2)
                .HasColumnName("PRIMARYINDEX");
            entity.Property(e => e.ReportsTo)
                .HasMaxLength(8)
                .HasColumnName("REPORTS_TO");
            entity.Property(e => e.SubDivCd)
                .HasMaxLength(6)
                .HasColumnName("SUB_DIV_CD");
            entity.Property(e => e.SubDivDesc)
                .HasMaxLength(40)
                .HasColumnName("SUB_DIV_DESC");
            entity.Property(e => e.UnionCd)
                .HasMaxLength(3)
                .HasColumnName("UNION_CD");
        });

        modelBuilder.Entity<VwRotation>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwRotation", "cts");

            entity.Property(e => e.Abbreviation)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CourseNumber)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RotId).ValueGeneratedOnAdd();
            entity.Property(e => e.SubjectCode)
                .HasMaxLength(3)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwService>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwService", "cts");

            entity.Property(e => e.ServiceId).ValueGeneratedOnAdd();
            entity.Property(e => e.ServiceName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShortName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwServiceChief>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwServiceChiefs", "cts");

            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.MothraId)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.ServiceName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwSession>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwSession", "cts");

            entity.Property(e => e.AcademicYear)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.CourseTitle)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Description)
                .HasMaxLength(1500)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(31)
                .IsUnicode(false);
            entity.Property(e => e.Title)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TypeDescription)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwStudent>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwStudents", "students");

            entity.Property(e => e.ClassLevel)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.SpridenId)
                .HasMaxLength(11)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwStudentSchedule>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwStudentSchedule", "cts");

            entity.Property(e => e.Abbreviation)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CrseNumb)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.DateEnd).HasColumnType("datetime");
            entity.Property(e => e.DateStart).HasColumnType("datetime");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.FullName)
                .HasMaxLength(91)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.MailId)
                .HasMaxLength(32)
                .IsUnicode(false);
            entity.Property(e => e.MiddleName)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.MothraId)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.RotationName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ServiceName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.StudentScheduleId).HasColumnName("StudentScheduleID");
            entity.Property(e => e.SubjCode)
                .HasMaxLength(3)
                .IsUnicode(false);
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

        modelBuilder.Entity<VwTerm>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwTerms");

            entity.Property(e => e.Description)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.TermType)
                .HasMaxLength(1)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwWeek>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwWeeks", "cts");

            entity.Property(e => e.DateEnd).HasColumnType("datetime");
            entity.Property(e => e.DateStart).HasColumnType("datetime");
            entity.Property(e => e.WeekId)
                .ValueGeneratedOnAdd()
                .HasColumnName("Week_ID");
        });

        modelBuilder.Entity<VwWeekGradYear>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwWeekGradYears", "cts");

            entity.Property(e => e.WeekId).HasColumnName("Week_ID");
            entity.Property(e => e.WeekgradyearId)
                .ValueGeneratedOnAdd()
                .HasColumnName("Weekgradyear_ID");
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
            entity.Property(e => e.ClientFormUpload).HasColumnName("clientFormUpload");
            entity.Property(e => e.Message)
                .IsUnicode(false)
                .HasColumnName("message");
            entity.Property(e => e.Order).HasColumnName("order");
            entity.Property(e => e.PatientFormUpload).HasColumnName("patientFormUpload");
            entity.Property(e => e.Version).HasColumnName("version");
            entity.Property(e => e.WorkflowStageId).HasColumnName("workflowStageId");

            entity.HasOne(d => d.WorkflowStage).WithMany(p => p.WorkflowStageVersions)
                .HasForeignKey(d => d.WorkflowStageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkflowStageVersion_WorkflowStage");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
