using Microsoft.EntityFrameworkCore;
using Viper.Models.CTS;

namespace Viper.Classes.SQLContext;

public class ClinicalSchedulerContext : DbContext
{
    public ClinicalSchedulerContext(DbContextOptions<ClinicalSchedulerContext> options) : base(options)
    {
    }

    public virtual DbSet<Rotation> Rotations { get; set; }
    public virtual DbSet<Service> Services { get; set; }
    public virtual DbSet<InstructorSchedule> InstructorSchedules { get; set; }
    public virtual DbSet<Week> Weeks { get; set; }
    public virtual DbSet<WeekGradYear> WeekGradYears { get; set; }
    // ScheduleAudit temporarily removed - will be added back in Phase 7 (Edit Functionality - Backend)
    // public virtual DbSet<ScheduleAudit> ScheduleAudits { get; set; }
    // Status and VWeek are accessed via raw SQL queries from ClinicalScheduler database
    // public virtual DbSet<Models.ClinicalScheduler.Status> Statuses { get; set; }
    // public virtual DbSet<Models.ClinicalScheduler.VWeek> VWeeks { get; set; }

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

        modelBuilder.Entity<Rotation>(entity =>
        {
            entity.HasKey(e => e.RotId);
            entity.ToTable("vwRotation", schema: "cts");
            entity.Property(e => e.SubjectCode).IsRequired(false);
            entity.Property(e => e.CourseNumber).IsRequired(false);
            entity.HasOne(e => e.Service).WithMany(s => s.Rotations)
               .HasForeignKey(e => e.ServiceId)
               .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId);
            entity.ToTable("vwService", schema: "cts");
            // Ignore navigation properties we don't need for clinical scheduler
            entity.Ignore(e => e.Encounters);
            entity.Ignore(e => e.Epas);
        });

        modelBuilder.Entity<InstructorSchedule>(entity =>
        {
            entity.HasKey(e => e.InstructorScheduleId);
            entity.ToTable("vwInstructorSchedule", schema: "cts");
            entity.Property(e => e.MiddleName).IsRequired(false);
            entity.Property(e => e.MailId).IsRequired(false);
            entity.Property(e => e.Role).IsRequired(false);
            entity.Property(e => e.SubjCode).IsRequired(false);
            entity.Property(e => e.CrseNumb).IsRequired(false);
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

        modelBuilder.Entity<Week>(entity =>
        {
            entity.HasKey(e => e.WeekId);
            entity.ToTable("vwWeeks", schema: "cts");
            entity.Property(e => e.WeekId).HasColumnName("Week_ID");
        });

        modelBuilder.Entity<WeekGradYear>(entity =>
        {
            entity.HasKey(e => e.WeekGradYearId);
            entity.ToTable("vwWeekGradYears", schema: "cts");
            entity.Property(e => e.WeekGradYearId).HasColumnName("Weekgradyear_ID");
            entity.Property(e => e.WeekId).HasColumnName("Week_ID");
            entity.HasOne(e => e.Week).WithMany(w => w.WeekGradYears)
               .HasForeignKey(e => e.WeekId)
               .OnDelete(DeleteBehavior.ClientSetNull);
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

        // Status and VWeek tables are accessed via raw SQL queries from ClinicalScheduler database
        // Entity Framework configurations removed since we query cross-database
    }
}