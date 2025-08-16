using Microsoft.EntityFrameworkCore;
using Viper.Models.CTS;
using Viper.Models.ClinicalScheduler;

namespace Viper.Classes.SQLContext;

public class ClinicalSchedulerContext : DbContext
{
    public ClinicalSchedulerContext(DbContextOptions<ClinicalSchedulerContext> options) : base(options)
    {
    }

    public virtual DbSet<Rotation> Rotations { get; set; }
    public virtual DbSet<Service> Services { get; set; }
    public virtual DbSet<InstructorSchedule> InstructorSchedules { get; set; }
    public virtual DbSet<StudentSchedule> StudentSchedules { get; set; } // Phase 3.2: Added StudentSchedule
    public virtual DbSet<Week> Weeks { get; set; }
    public virtual DbSet<WeekGradYear> WeekGradYears { get; set; }
    public virtual DbSet<Person> Persons { get; set; } // Person data from vPerson view
    // ScheduleAudit temporarily removed - will be added back in Phase 7 (Edit Functionality - Backend)
    // public virtual DbSet<ScheduleAudit> ScheduleAudits { get; set; }
    public virtual DbSet<Models.ClinicalScheduler.Status> Statuses { get; set; }
    // VWeek is accessed through WeekGradYear + Week entities for better type safety
    // public virtual DbSet<Models.ClinicalScheduler.VWeek> VWeeks { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (HttpHelper.Settings != null)
        {
            optionsBuilder.UseSqlServer(HttpHelper.Settings["ConnectionStrings:ClinicalScheduler"]);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Rotation>(entity =>
        {
            entity.HasKey(e => e.RotId);
            entity.ToTable("Rotation", schema: "dbo");

            // Map Entity Framework property names to actual database column names
            entity.Property(e => e.RotId).HasColumnName("Rot_ID");
            entity.Property(e => e.ServiceId).HasColumnName("Service_ID");
            entity.Property(e => e.Name).HasColumnName("Rot_Name");
            entity.Property(e => e.Abbreviation).HasColumnName("Rot_Abbrev");
            entity.Property(e => e.SubjectCode).HasColumnName("SubjCode").IsRequired(false);
            entity.Property(e => e.CourseNumber).HasColumnName("CrseNumb").IsRequired(false);

            entity.HasOne(e => e.Service).WithMany(s => s.Rotations)
               .HasForeignKey(e => e.ServiceId)
               .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId);
            entity.ToTable("Service", schema: "dbo");

            // Map Entity Framework property names to actual database column names
            entity.Property(e => e.ServiceId).HasColumnName("Service_ID");
            entity.Property(e => e.ServiceName).HasColumnName("ServiceName"); // Already matches
            entity.Property(e => e.ShortName).HasColumnName("ShortName"); // Already matches

            // Ignore navigation properties we don't need for clinical scheduler
            entity.Ignore(e => e.Encounters);
            entity.Ignore(e => e.Epas);
        });

        modelBuilder.Entity<InstructorSchedule>(entity =>
        {
            entity.HasKey(e => e.InstructorScheduleId);
            entity.ToTable("InstructorSchedule", schema: "dbo");

            // Map Entity Framework property names to actual database column names
            entity.Property(e => e.InstructorScheduleId).HasColumnName("InstructorSchedule_ID");
            entity.Property(e => e.MothraId).HasColumnName("Mothra_ID");
            entity.Property(e => e.RotationId).HasColumnName("Rot_ID");
            entity.Property(e => e.WeekId).HasColumnName("Week_ID");
            entity.Property(e => e.Evaluator).HasColumnName("evaluator");
            entity.Property(e => e.Role).HasColumnName("role").IsRequired(false);

            // Ignore properties that exist in model but not in actual database table
            // These columns exist in DB but not in our model: Modified_by, Modified_date
            // These properties in model must come from joins/navigation properties:
            entity.Ignore(e => e.DateStart);     // Must come from Week.DateStart
            entity.Ignore(e => e.DateEnd);       // Must come from Week.DateEnd
            entity.Ignore(e => e.FirstName);     // Must come from person data
            entity.Ignore(e => e.LastName);      // Must come from person data
            entity.Ignore(e => e.MiddleName);    // Must come from person data
            entity.Ignore(e => e.FullName);      // Must come from person data
            entity.Ignore(e => e.MailId);        // Must come from person data
            entity.Ignore(e => e.SubjCode);      // Must come from Rotation data
            entity.Ignore(e => e.CrseNumb);      // Must come from Rotation data
            entity.Ignore(e => e.ServiceId);     // Must come from Rotation data
            entity.Ignore(e => e.RotationName);  // Must come from Rotation data
            entity.Ignore(e => e.Abbreviation);  // Must come from Rotation data
            entity.Ignore(e => e.ServiceName);   // Must come from Service data
            entity.Ignore(e => e.Service);       // Service navigation not needed in ClinicalScheduler context

            entity.HasOne(e => e.Rotation).WithMany()
               .HasForeignKey(e => e.RotationId)
               .OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(e => e.Week).WithMany()
               .HasForeignKey(e => e.WeekId)
               .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Week>(entity =>
        {
            entity.HasKey(e => e.WeekId);
            entity.ToTable("vWeek", schema: "dbo"); // This is a view in ClinicalScheduler DB
            entity.Property(e => e.WeekId).HasColumnName("Week_ID");
        });

        modelBuilder.Entity<WeekGradYear>(entity =>
        {
            entity.HasKey(e => e.WeekGradYearId);
            entity.ToTable("weekGradYear", schema: "dbo"); // Table name from diagnostic
            entity.Property(e => e.WeekGradYearId).HasColumnName("Weekgradyear_ID");
            entity.Property(e => e.WeekId).HasColumnName("Week_ID");
            entity.HasOne(e => e.Week).WithMany(w => w.WeekGradYears)
               .HasForeignKey(e => e.WeekId)
               .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // Phase 3.2: Added StudentSchedule entity configuration
        modelBuilder.Entity<StudentSchedule>(entity =>
        {
            entity.HasKey(e => e.StudentScheduleId);
            entity.ToTable("studentSchedule", schema: "dbo"); // Note: lowercase 's' from diagnostic
            entity.Property(e => e.MiddleName).IsRequired(false);
            entity.Property(e => e.MailId).IsRequired(false);
            entity.Property(e => e.Pidm).IsRequired(false);
            entity.Property(e => e.NotGraded).IsRequired(false);
            entity.Property(e => e.NotEnrolled).IsRequired(false);
            entity.Property(e => e.MakeUp).IsRequired(false);
            entity.Property(e => e.Incomplete).IsRequired(false);
            entity.HasOne(e => e.Service).WithMany()
               .HasForeignKey(e => e.ServiceId)
               .OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(e => e.Rotation).WithMany()
               .HasForeignKey(e => e.RotationId)
               .OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(e => e.Week).WithMany()
               .HasForeignKey(e => e.WeekId)
               .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Person>(entity =>
        {
            entity.HasKey(e => e.IdsMothraId);
            entity.ToTable("vPerson", schema: "dbo");

            // Map Entity Framework property names to actual database column names
            entity.Property(e => e.IdsMothraId).HasColumnName("ids_mothraID");
            entity.Property(e => e.PersonDisplayFullName).HasColumnName("person_display_full_name");
            entity.Property(e => e.PersonDisplayLastName).HasColumnName("person_display_last_name");
            entity.Property(e => e.PersonDisplayFirstName).HasColumnName("person_display_first_name");
            entity.Property(e => e.IdsMailId).HasColumnName("ids_mailID").IsRequired(false);
        });

        // ScheduleAudit configuration temporarily removed - will be added back in Phase 7
        // modelBuilder.Entity<ScheduleAudit>(entity =>
        // {
        //     entity.HasKey(e => e.ScheduleAuditId);
        //     entity.ToTable("ScheduleAudit", schema: "cts");
        //     entity.Property(e => e.Detail).IsRequired(false);
        //     entity.Property(e => e.MothraId).IsRequired(false);
        //     entity.HasOne(e => e.Modifier).WithMany()
        //        .HasForeignKey(e => e.ModifiedBy)
        //        .OnDelete(DeleteBehavior.ClientSetNull);
        //     entity.HasOne(e => e.InstructorSchedule).WithMany()
        //        .HasForeignKey(e => e.InstructorScheduleId)
        //        .OnDelete(DeleteBehavior.SetNull);
        //     entity.HasOne(e => e.Rotation).WithMany()
        //        .HasForeignKey(e => e.RotationId)
        //        .OnDelete(DeleteBehavior.SetNull);
        //     entity.HasOne(e => e.Week).WithMany()
        //        .HasForeignKey(e => e.WeekId)
        //        .OnDelete(DeleteBehavior.SetNull);
        // });

        modelBuilder.Entity<Models.ClinicalScheduler.Status>(entity =>
        {
            entity.HasKey(e => e.GradYear);
            entity.ToTable("Status", schema: "dbo");

            // Map Entity Framework property names to actual database column names
            entity.Property(e => e.SAStreamCrn).HasColumnName("SAStreamCRN");
            entity.Property(e => e.LAStreamCrn).HasColumnName("LAStreamCRN");
        });

        // VWeek table is accessed through WeekGradYear + Week entities for better type safety
    }
}