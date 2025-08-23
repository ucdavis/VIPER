using Microsoft.EntityFrameworkCore;
using Viper.Models.ClinicalScheduler;

namespace Viper.Classes.SQLContext;

public class ClinicalSchedulerContext : DbContext
{
    public ClinicalSchedulerContext(DbContextOptions<ClinicalSchedulerContext> options) : base(options)
    {
    }

    // Clinical Scheduler models - map directly to Clinical Scheduler database tables
    public virtual DbSet<Rotation> Rotations { get; set; }
    public virtual DbSet<Service> Services { get; set; }
    public virtual DbSet<InstructorSchedule> InstructorSchedules { get; set; }
    public virtual DbSet<StudentSchedule> StudentSchedules { get; set; }
    public virtual DbSet<Week> Weeks { get; set; }
    public virtual DbSet<WeekGradYear> WeekGradYears { get; set; }
    public virtual DbSet<Person> Persons { get; set; } // Person data from vPerson view
    public virtual DbSet<Status> Statuses { get; set; }

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

            entity.Property(e => e.RotId).HasColumnName("Rot_ID");
            entity.Property(e => e.ServiceId).HasColumnName("Service_ID");
            entity.Property(e => e.Name).HasColumnName("Rot_Name");
            entity.Property(e => e.Abbreviation).HasColumnName("Rot_Abbrev");
            entity.Property(e => e.SubjectCode).HasColumnName("SubjCode").IsRequired(false);
            entity.Property(e => e.CourseNumber).HasColumnName("CrseNumb").IsRequired(false);
            entity.Property(e => e.Active).HasColumnName("active").HasDefaultValue(true);

            entity.HasOne(e => e.Service).WithMany(s => s.Rotations)
               .HasForeignKey(e => e.ServiceId)
               .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId);
            entity.ToTable("Service", schema: "dbo");

            entity.Property(e => e.ServiceId).HasColumnName("Service_ID");
            entity.Property(e => e.ServiceName).HasColumnName("ServiceName");
            entity.Property(e => e.ShortName).HasColumnName("ShortName");
            entity.Property(e => e.ScheduleEditPermission).HasColumnName("ScheduleEditPermission").IsRequired(false);
        });

        modelBuilder.Entity<InstructorSchedule>(entity =>
        {
            entity.HasKey(e => e.InstructorScheduleId);
            entity.ToTable("InstructorSchedule", schema: "dbo");

            entity.Property(e => e.InstructorScheduleId).HasColumnName("InstructorSchedule_ID");
            entity.Property(e => e.MothraId).HasColumnName("Mothra_ID");
            entity.Property(e => e.RotationId).HasColumnName("Rot_ID");
            entity.Property(e => e.WeekId).HasColumnName("Week_ID");
            entity.Property(e => e.Evaluator).HasColumnName("evaluator");
            entity.Property(e => e.Role).HasColumnName("role").IsRequired(false);

            entity.HasOne(e => e.Rotation).WithMany(r => r.InstructorSchedules)
               .HasForeignKey(e => e.RotationId)
               .OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(e => e.Week).WithMany(w => w.InstructorSchedules)
               .HasForeignKey(e => e.WeekId)
               .OnDelete(DeleteBehavior.ClientSetNull);
            // Person relationship uses MothraId as foreign key and IdsMothraId as principal key
            entity.HasOne(e => e.Person).WithMany()
               .HasForeignKey(e => e.MothraId)
               .HasPrincipalKey(p => p.IdsMothraId)
               .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Week>(entity =>
        {
            entity.HasKey(e => e.WeekId);
            entity.ToTable("vWeek", schema: "dbo"); // Maps to database view for week data

            entity.Property(e => e.WeekId).HasColumnName("Week_ID");
            entity.Property(e => e.DateStart).HasColumnName("DateStart");
            entity.Property(e => e.DateEnd).HasColumnName("DateEnd");
            entity.Property(e => e.WeekNumber).HasColumnName("weeknum");
            entity.Property(e => e.TermCode).HasColumnName("TermCode");
            entity.Property(e => e.ExtendedRotation).HasColumnName("ExtendedRotation");
            entity.Property(e => e.StartWeek).HasColumnName("StartWeek");
        });

        modelBuilder.Entity<WeekGradYear>(entity =>
        {
            entity.HasKey(e => e.WeekGradYearId);
            entity.ToTable("weekGradYear", schema: "dbo");
            entity.Property(e => e.WeekGradYearId).HasColumnName("Weekgradyear_ID");
            entity.Property(e => e.WeekId).HasColumnName("Week_ID");
            entity.HasOne(e => e.Week).WithMany(w => w.WeekGradYears)
               .HasForeignKey(e => e.WeekId)
               .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // StudentSchedule entity configuration
        modelBuilder.Entity<StudentSchedule>(entity =>
        {
            entity.HasKey(e => e.StudentScheduleId);
            entity.ToTable("studentSchedule", schema: "dbo");
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
            entity.ToTable("vPerson", schema: "dbo"); // Maps to person view for instructor data

            entity.Property(e => e.IdsMothraId).HasColumnName("ids_mothraID");
            entity.Property(e => e.PersonDisplayFullName).HasColumnName("person_display_full_name");
            entity.Property(e => e.PersonDisplayLastName).HasColumnName("person_display_last_name");
            entity.Property(e => e.PersonDisplayFirstName).HasColumnName("person_display_first_name");
            entity.Property(e => e.IdsMailId).HasColumnName("ids_mailID").IsRequired(false);
        });


        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.GradYear);
            entity.ToTable("Status", schema: "dbo"); // Contains grad year configuration and scheduling settings

            entity.Property(e => e.GradYear).HasColumnName("GradYear");
            entity.Property(e => e.OpenDate).HasColumnName("OpenDate");
            entity.Property(e => e.CloseDate).HasColumnName("CloseDate");
            entity.Property(e => e.NumWeeks).HasColumnName("NumWeeks");
            entity.Property(e => e.SAStreamCrn).HasColumnName("SAStreamCRN");
            entity.Property(e => e.LAStreamCrn).HasColumnName("LAStreamCRN");
            entity.Property(e => e.ExtRequestOpen).HasColumnName("ExtRequestOpen");
            entity.Property(e => e.ExtRequestDeadline).HasColumnName("ExtRequestDeadline");
            entity.Property(e => e.ExtRequestReopen).HasColumnName("ExtRequestReopen");
            entity.Property(e => e.DefaultGradYear).HasColumnName("DefaultGradYear");
            entity.Property(e => e.DefaultSelectionYear).HasColumnName("DefaultSelectionYear");
            entity.Property(e => e.PublishSchedule).HasColumnName("PublishSchedule");
        });

        // VWeek table is accessed through WeekGradYear + Week entities for better type safety
    }
}