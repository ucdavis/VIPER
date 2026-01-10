using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Models.Entities;

namespace Viper.Areas.Effort;

/// <summary>
/// Entity Framework DbContext for the Effort system.
/// All tables are in the [effort] schema in the VIPER database.
/// </summary>
public class EffortDbContext : DbContext
{
    public EffortDbContext(DbContextOptions<EffortDbContext> options) : base(options)
    {
    }

    // Core data tables
    public virtual DbSet<EffortTerm> Terms { get; set; }
    public virtual DbSet<EffortPerson> Persons { get; set; }
    public virtual DbSet<EffortCourse> Courses { get; set; }
    public virtual DbSet<EffortRecord> Records { get; set; }

    // Lookup tables
    public virtual DbSet<EffortRole> Roles { get; set; }
    public virtual DbSet<PercentAssignType> PercentAssignTypes { get; set; }
    public virtual DbSet<EffortType> EffortTypes { get; set; }
    public virtual DbSet<Unit> Units { get; set; }
    public virtual DbSet<JobCode> JobCodes { get; set; }
    public virtual DbSet<ReportUnit> ReportUnits { get; set; }

    // Supporting tables
    public virtual DbSet<Percentage> Percentages { get; set; }
    public virtual DbSet<Sabbatical> Sabbaticals { get; set; }
    public virtual DbSet<UserAccess> UserAccess { get; set; }
    public virtual DbSet<AlternateTitle> AlternateTitles { get; set; }
    public virtual DbSet<CourseRelationship> CourseRelationships { get; set; }
    public virtual DbSet<Audit> Audits { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (HttpHelper.Settings != null)
        {
            optionsBuilder.UseSqlServer(HttpHelper.Settings["ConnectionStrings:VIPER"]);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // EffortTerm (effort.TermStatus)
        modelBuilder.Entity<EffortTerm>(entity =>
        {
            entity.HasKey(e => e.TermCode);
            entity.ToTable("TermStatus", schema: "effort");

            entity.Property(e => e.TermCode).HasColumnName("TermCode");
            entity.Property(e => e.Status).HasColumnName("Status").HasMaxLength(20);
            entity.Property(e => e.HarvestedDate).HasColumnName("HarvestedDate");
            entity.Property(e => e.OpenedDate).HasColumnName("OpenedDate");
            entity.Property(e => e.ClosedDate).HasColumnName("ClosedDate");
            entity.Property(e => e.CreatedDate).HasColumnName("CreatedDate");
            entity.Property(e => e.ModifiedDate).HasColumnName("ModifiedDate");
            entity.Property(e => e.ModifiedBy).HasColumnName("ModifiedBy");
        });

        // EffortPerson (effort.Persons)
        modelBuilder.Entity<EffortPerson>(entity =>
        {
            entity.HasKey(e => new { e.PersonId, e.TermCode });
            entity.ToTable("Persons", schema: "effort");

            entity.Property(e => e.PersonId).HasColumnName("PersonId");
            entity.Property(e => e.TermCode).HasColumnName("TermCode");
            entity.Property(e => e.FirstName).HasColumnName("FirstName").HasMaxLength(50);
            entity.Property(e => e.LastName).HasColumnName("LastName").HasMaxLength(50);
            entity.Property(e => e.MiddleInitial).HasColumnName("MiddleInitial").HasMaxLength(1);
            entity.Property(e => e.EffortTitleCode).HasColumnName("EffortTitleCode").HasMaxLength(6);
            entity.Property(e => e.EffortDept).HasColumnName("EffortDept").HasMaxLength(6);
            entity.Property(e => e.PercentAdmin).HasColumnName("PercentAdmin");
            entity.Property(e => e.JobGroupId).HasColumnName("JobGroupId").HasMaxLength(3).IsFixedLength();
            entity.Property(e => e.Title).HasColumnName("Title").HasMaxLength(50);
            entity.Property(e => e.AdminUnit).HasColumnName("AdminUnit").HasMaxLength(25);
            entity.Property(e => e.EffortVerified).HasColumnName("EffortVerified");
            entity.Property(e => e.ReportUnit).HasColumnName("ReportUnit").HasMaxLength(50);
            entity.Property(e => e.VolunteerWos).HasColumnName("VolunteerWos");
            entity.Property(e => e.PercentClinical).HasColumnName("PercentClinical");

            entity.HasOne(e => e.Term)
                .WithMany(t => t.Persons)
                .HasForeignKey(e => e.TermCode);
        });

        // EffortCourse (effort.Courses)
        modelBuilder.Entity<EffortCourse>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Courses", schema: "effort");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Crn).HasColumnName("Crn").HasMaxLength(5).IsFixedLength();
            entity.Property(e => e.TermCode).HasColumnName("TermCode");
            entity.Property(e => e.SubjCode).HasColumnName("SubjCode").HasMaxLength(3).IsFixedLength();
            entity.Property(e => e.CrseNumb).HasColumnName("CrseNumb").HasMaxLength(5).IsFixedLength();
            entity.Property(e => e.SeqNumb).HasColumnName("SeqNumb").HasMaxLength(3).IsFixedLength();
            entity.Property(e => e.Enrollment).HasColumnName("Enrollment");
            entity.Property(e => e.Units).HasColumnName("Units").HasColumnType("decimal(4,2)");
            entity.Property(e => e.CustDept).HasColumnName("CustDept").HasMaxLength(6);

            entity.HasOne(e => e.Term)
                .WithMany(t => t.Courses)
                .HasForeignKey(e => e.TermCode);
        });

        // EffortRecord (effort.Records)
        modelBuilder.Entity<EffortRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Records", schema: "effort");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.CourseId).HasColumnName("CourseId");
            entity.Property(e => e.PersonId).HasColumnName("PersonId");
            entity.Property(e => e.TermCode).HasColumnName("TermCode");
            entity.Property(e => e.EffortTypeId).HasColumnName("EffortTypeId").HasMaxLength(3);
            entity.Property(e => e.RoleId).HasColumnName("RoleId");
            entity.Property(e => e.Hours).HasColumnName("Hours");
            entity.Property(e => e.Weeks).HasColumnName("Weeks");
            entity.Property(e => e.Crn).HasColumnName("Crn").HasMaxLength(5);
            entity.Property(e => e.ModifiedDate).HasColumnName("ModifiedDate");
            entity.Property(e => e.ModifiedBy).HasColumnName("ModifiedBy");

            entity.HasOne(e => e.Course)
                .WithMany(c => c.Records)
                .HasForeignKey(e => e.CourseId);

            entity.HasOne(e => e.Person)
                .WithMany(p => p.Records)
                .HasForeignKey(e => new { e.PersonId, e.TermCode });

            entity.HasOne(e => e.Term)
                .WithMany(t => t.Records)
                .HasForeignKey(e => e.TermCode);

            entity.HasOne(e => e.RoleNavigation)
                .WithMany(r => r.Records)
                .HasForeignKey(e => e.RoleId);

            entity.HasOne(e => e.EffortTypeNavigation)
                .WithMany(s => s.Records)
                .HasForeignKey(e => e.EffortTypeId);
        });

        // EffortRole (effort.Roles)
        modelBuilder.Entity<EffortRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Roles", schema: "effort");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Description).HasColumnName("Description").HasMaxLength(50);
            entity.Property(e => e.IsActive).HasColumnName("IsActive");
            entity.Property(e => e.SortOrder).HasColumnName("SortOrder");
        });

        // PercentAssignType (effort.PercentAssignTypes)
        modelBuilder.Entity<PercentAssignType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("PercentAssignTypes", schema: "effort");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Class).HasColumnName("Class").HasMaxLength(20);
            entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(50);
            entity.Property(e => e.ShowOnTemplate).HasColumnName("ShowOnTemplate");
            entity.Property(e => e.IsActive).HasColumnName("IsActive");
        });

        // EffortType (effort.EffortTypes)
        modelBuilder.Entity<EffortType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("EffortTypes", schema: "effort");

            entity.Property(e => e.Id).HasColumnName("Id").HasMaxLength(3);
            entity.Property(e => e.Description).HasColumnName("Description").HasMaxLength(50);
            entity.Property(e => e.UsesWeeks).HasColumnName("UsesWeeks");
            entity.Property(e => e.IsActive).HasColumnName("IsActive");
            entity.Property(e => e.FacultyCanEnter).HasColumnName("FacultyCanEnter");
            entity.Property(e => e.AllowedOnDvm).HasColumnName("AllowedOnDvm");
            entity.Property(e => e.AllowedOn199299).HasColumnName("AllowedOn199299");
            entity.Property(e => e.AllowedOnRCourses).HasColumnName("AllowedOnRCourses");
        });

        // Unit (effort.Units)
        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Units", schema: "effort");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(20);
            entity.Property(e => e.IsActive).HasColumnName("IsActive");
        });

        // JobCode (effort.JobCodes)
        modelBuilder.Entity<JobCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("JobCodes", schema: "effort");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Code).HasColumnName("Code").HasMaxLength(6);
            entity.Property(e => e.IncludeClinSchedule).HasColumnName("IncludeClinSchedule");
            entity.Property(e => e.IsActive).HasColumnName("IsActive");
        });

        // ReportUnit (effort.ReportUnits)
        modelBuilder.Entity<ReportUnit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ReportUnits", schema: "effort");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.UnitCode).HasColumnName("UnitCode").HasMaxLength(10);
            entity.Property(e => e.UnitName).HasColumnName("UnitName").HasMaxLength(100);
            entity.Property(e => e.ParentUnitId).HasColumnName("ParentUnitId");
            entity.Property(e => e.IsActive).HasColumnName("IsActive");
            entity.Property(e => e.SortOrder).HasColumnName("SortOrder");

            entity.HasOne(e => e.ParentUnit)
                .WithMany(p => p.ChildUnits)
                .HasForeignKey(e => e.ParentUnitId);
        });

        // Percentage (effort.Percentages)
        modelBuilder.Entity<Percentage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Percentages", schema: "effort");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.PersonId).HasColumnName("PersonId");
            entity.Property(e => e.AcademicYear).HasColumnName("AcademicYear").HasMaxLength(9).IsFixedLength();
            entity.Property(e => e.PercentageValue).HasColumnName("Percentage").HasColumnType("decimal(5,2)");
            entity.Property(e => e.PercentAssignTypeId).HasColumnName("PercentAssignTypeId");
            entity.Property(e => e.UnitId).HasColumnName("UnitId");
            entity.Property(e => e.Modifier).HasColumnName("Modifier").HasMaxLength(50);
            entity.Property(e => e.Comment).HasColumnName("Comment").HasMaxLength(100);
            entity.Property(e => e.StartDate).HasColumnName("StartDate");
            entity.Property(e => e.EndDate).HasColumnName("EndDate");
            entity.Property(e => e.ModifiedDate).HasColumnName("ModifiedDate");
            entity.Property(e => e.ModifiedBy).HasColumnName("ModifiedBy");
            entity.Property(e => e.Compensated).HasColumnName("Compensated");

            entity.HasOne(e => e.PercentAssignType)
                .WithMany(t => t.Percentages)
                .HasForeignKey(e => e.PercentAssignTypeId);

            entity.HasOne(e => e.Unit)
                .WithMany()
                .HasForeignKey(e => e.UnitId);
        });

        // Sabbatical (effort.Sabbaticals)
        modelBuilder.Entity<Sabbatical>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Sabbaticals", schema: "effort");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.PersonId).HasColumnName("PersonId");
            entity.Property(e => e.ExcludeClinicalTerms).HasColumnName("ExcludeClinicalTerms").HasMaxLength(2000);
            entity.Property(e => e.ExcludeDidacticTerms).HasColumnName("ExcludeDidacticTerms").HasMaxLength(2000);
            entity.Property(e => e.ModifiedDate).HasColumnName("ModifiedDate");
            entity.Property(e => e.ModifiedBy).HasColumnName("ModifiedBy");
        });

        // UserAccess (effort.UserAccess)
        modelBuilder.Entity<UserAccess>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("UserAccess", schema: "effort");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.PersonId).HasColumnName("PersonId");
            entity.Property(e => e.DepartmentCode).HasColumnName("DepartmentCode").HasMaxLength(6);
            entity.Property(e => e.ModifiedDate).HasColumnName("ModifiedDate");
            entity.Property(e => e.ModifiedBy).HasColumnName("ModifiedBy");
            entity.Property(e => e.IsActive).HasColumnName("IsActive");
        });

        // AlternateTitle (effort.AlternateTitles)
        modelBuilder.Entity<AlternateTitle>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("AlternateTitles", schema: "effort");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.PersonId).HasColumnName("PersonId");
            entity.Property(e => e.AlternateTitleText).HasColumnName("AlternateTitle").HasMaxLength(100);
            entity.Property(e => e.EffectiveDate).HasColumnName("EffectiveDate");
            entity.Property(e => e.ExpirationDate).HasColumnName("ExpirationDate");
            entity.Property(e => e.ModifiedDate).HasColumnName("ModifiedDate");
            entity.Property(e => e.ModifiedBy).HasColumnName("ModifiedBy");
            entity.Property(e => e.IsActive).HasColumnName("IsActive");
        });

        // CourseRelationship (effort.CourseRelationships)
        modelBuilder.Entity<CourseRelationship>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("CourseRelationships", schema: "effort");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.ParentCourseId).HasColumnName("ParentCourseId");
            entity.Property(e => e.ChildCourseId).HasColumnName("ChildCourseId");
            entity.Property(e => e.RelationshipType).HasColumnName("RelationshipType").HasMaxLength(20);

            // Each child course can only have one parent
            entity.HasIndex(e => e.ChildCourseId, "IX_CourseRelationships_ChildCourseId")
                .IsUnique();

            entity.HasOne(e => e.ParentCourse)
                .WithMany(c => c.ParentRelationships)
                .HasForeignKey(e => e.ParentCourseId);

            entity.HasOne(e => e.ChildCourse)
                .WithMany(c => c.ChildRelationships)
                .HasForeignKey(e => e.ChildCourseId);
        });

        // Audit (effort.Audits)
        modelBuilder.Entity<Audit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Audits", schema: "effort");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.TableName).HasColumnName("TableName").HasMaxLength(50);
            entity.Property(e => e.RecordId).HasColumnName("RecordId");
            entity.Property(e => e.Action).HasColumnName("Action").HasMaxLength(50);
            entity.Property(e => e.ChangedBy).HasColumnName("ChangedBy");
            entity.Property(e => e.ChangedDate).HasColumnName("ChangedDate");
            entity.Property(e => e.Changes).HasColumnName("Changes");
            entity.Property(e => e.MigratedDate).HasColumnName("MigratedDate");
            entity.Property(e => e.UserAgent).HasColumnName("UserAgent").HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasColumnName("IpAddress").HasMaxLength(50);
            entity.Property(e => e.LegacyAction).HasColumnName("LegacyAction").HasMaxLength(100);
            entity.Property(e => e.LegacyCRN).HasColumnName("LegacyCRN").HasMaxLength(20);
            entity.Property(e => e.LegacyMothraID).HasColumnName("LegacyMothraID").HasMaxLength(20);
            entity.Property(e => e.TermCode).HasColumnName("TermCode");
        });
    }
}
