using Microsoft.EntityFrameworkCore;
using Viper.Models.CTS;

namespace Viper.Classes.SQLContext;

public partial class VIPERContext : DbContext
{
    /* CTS */
    public virtual DbSet<Competency> Competencies { get; set; }
    public virtual DbSet<Domain> Domains { get; set; }
    public virtual DbSet<Level> Levels { get; set; }
    public virtual DbSet<Epa> Epas { get; set; }
    public virtual DbSet<EpaService> EpaServices { get; set; }
    public virtual DbSet<Encounter> Encounters { get; set; }
    public virtual DbSet<EncounterInstructor> EncounterInstructors { get; set; } 
    public virtual DbSet<CtsAudit> CtsAudits { get; set; }

    /* Students */
    public virtual DbSet<DvmStudent> DvmStudent { get; set; }

    /* Clinical Scheduler */
    public virtual DbSet<InstructorSchedule> InstructorSchedules { get; set; }
    public virtual DbSet<StudentSchedule> StudentSchedules { get; set; }
    public virtual DbSet<Rotation> Rotations { get; set; }
    public virtual DbSet<Service> Services { get; set; }
    public virtual DbSet<WeekGradYear> WeekGradYears { get; set; }
    public virtual DbSet<Week> Weeks { get; set; }

    /* CREST */
    public virtual DbSet<CourseSessionOffering> CourseSessionOffering { get; set; }

    partial void OnModelCreatingCTS(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Competency>(entity =>
        {
            entity.ToTable("Competency", "cts");

            entity.Property(e => e.Description).IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .IsUnicode(false);

            entity.HasOne(d => d.Domain).WithMany(p => p.Competencies)
                .HasForeignKey(d => d.DomainId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Competency_Domain");

            entity.HasOne(d => d.Parent).WithMany(p => p.Children)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Competency_Competency");
        });

        modelBuilder.Entity<Domain>(entity =>
        {
            entity.ToTable("Domain", "cts");
        });

        modelBuilder.Entity<Level>(entity =>
        {
            entity.ToTable("Level", "cts");
        });

        modelBuilder.Entity<Epa>(entity =>
        {
            entity.ToTable("Epa", "cts");
            entity.HasMany(e => e.Services)
                .WithMany(s => s.Epas)
                .UsingEntity<Dictionary<string, object>>(
                    "EpaService",
                    r => r.HasOne<Service>().WithMany().HasForeignKey("ServiceId"),
                    l => l.HasOne<Epa>().WithMany().HasForeignKey("EpaId"),
                    j =>
                    {
                        j.HasKey("EpaId", "ServiceId");
                        j.ToTable("EpaService", "cts");
                    });
        });

        modelBuilder.Entity<Encounter>(entity =>
        {
            entity.ToTable("Encounter", "cts");
            entity.HasOne(e => e.Service).WithMany(e => e.Encounters)
                .HasForeignKey(e => e.ServiceId);
            entity.HasOne(e => e.Offering).WithMany()
                .HasForeignKey(e => e.OfferingId)
                .HasPrincipalKey(e => e.EduTaskOfferid);
            entity.HasOne(e => e.Clinician).WithMany()
                .HasForeignKey(e => e.ClinicianId);
            entity.HasOne(e => e.Student).WithMany()
                .HasForeignKey(e => e.StudentUserId);
            entity.HasOne(e => e.EnteredByPerson).WithMany()
                .HasForeignKey(e => e.EnteredBy);
            entity.HasOne(e => e.Epa).WithMany()
                .HasForeignKey(e => e.EpaId);
            entity.HasOne(e => e.Level).WithMany()
                .HasForeignKey(e => e.LevelId);
        });

        modelBuilder.Entity<EncounterInstructor>(entity =>
        {
            entity.ToTable("EncounterInstructor", "cts");
            entity.HasOne(e => e.Instructor).WithMany()
                .HasForeignKey(e => e.InstructorId);
            entity.HasOne(e => e.Encounter).WithMany(e => e.EncounterInstructors)
                .HasForeignKey(e => e.EncounterId);
        });

        modelBuilder.Entity<CtsAudit>(entity =>
        {
            entity.ToTable("CtsAudit", "cts");
            entity.HasOne(e => e.Modifier).WithMany()
                .HasForeignKey(e => e.ModifiedBy);
            entity.HasOne(e => e.Encounter).WithMany()
                .HasForeignKey(e => e.EncounterId);
        });

        /* "Exteral" entities */
        modelBuilder.Entity<DvmStudent>(entity =>
        {
            entity.ToTable("vwDvmStudents", schema: "cts");
            entity.HasKey(e => e.MothraId);
            entity.Property(e => e.LastName).HasColumnName("person_last_name");
            entity.Property(e => e.FirstName).HasColumnName("person_first_name");
            entity.Property(e => e.MiddleName).HasColumnName("person_middle_name");
            entity.Property(e => e.LoginId).HasColumnName("ids_loginId");
            entity.Property(e => e.Pidm).HasColumnName("ids_pidm");
            entity.Property(e => e.MailId).HasColumnName("ids_mailid");
            entity.Property(e => e.MothraId).HasColumnName("ids_mothraId");
            entity.Property(e => e.ClassLevel).HasColumnName("students_class_level");
            entity.Property(e => e.TermCode).HasColumnName("students_term_code");
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
                .HasForeignKey(e => e.ServiceId);
            entity.HasOne(e => e.Rotation).WithMany()
                .HasForeignKey(r => r.RotationId);
        });

        modelBuilder.Entity<StudentSchedule>(entity =>
        {
            entity.HasKey(e => e.StudentScheduleId);
            entity.ToTable("vwStudentSchedule", schema: "cts");
            entity.Property(e => e.MiddleName).IsRequired(false);
            entity.Property(e => e.MailId).IsRequired(false);
            entity.Property(e => e.Pidm).IsRequired(false);
            entity.Property(e => e.NotGraded).IsRequired(false);
            entity.Property(e => e.NotEnrolled).IsRequired(false);
            entity.Property(e => e.MakeUp).IsRequired(false);
            entity.Property(e => e.Incomplete).IsRequired(false);
            entity.Property(e => e.SubjCode).IsRequired(false);
            entity.Property(e => e.CrseNumb).IsRequired(false);
            entity.HasOne(e => e.Service).WithMany()
                .HasForeignKey(e => e.ServiceId);
            entity.HasOne(e => e.Rotation).WithMany()
                .HasForeignKey(r => r.RotationId);
        });

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
        });

        modelBuilder.Entity<Week>(entity =>
        {
            entity.HasKey(e => e.WeekId);
            entity.ToTable("vwWeeks", schema: "cts");
            entity.Property(e => e.WeekId).HasColumnName("Week_ID");
            entity.HasMany(e => e.WeekGradYears).WithOne(e => e.Week)
                .HasForeignKey(w => w.WeekId);
        });

        modelBuilder.Entity<WeekGradYear>(entity =>
        {
            entity.HasKey(e => e.WeekGradYearId);
            entity.ToTable("vwWeekGradYears", schema: "cts");
            entity.Property(e => e.WeekGradYearId).HasColumnName("Weekgradyear_ID");
            entity.Property(e => e.WeekId).HasColumnName("Week_ID");
        });

        modelBuilder.Entity<CourseSessionOffering>(entity =>
        {
            entity.HasKey(e => new { e.CourseId, e.SessionId, e.EduTaskOfferid });
            entity.HasAlternateKey(e => e.EduTaskOfferid);
            entity.ToTable("vwCourseSessionOffering", schema: "cts");
			entity.Property(e => e.Crn).IsRequired(false);
			entity.Property(e => e.SsaCourseNum).IsRequired(false);
			entity.Property(e => e.SessionType).IsRequired(false);
			entity.Property(e => e.FromDate).IsRequired(false);
			entity.Property(e => e.ThruDate).IsRequired(false);
			entity.Property(e => e.FromTime).IsRequired(false);
			entity.Property(e => e.ThruTime).IsRequired(false);
			entity.Property(e => e.Room).IsRequired(false);
			entity.Property(e => e.TypeOrder).HasColumnName("type_order").IsRequired(false);
			entity.Property(e => e.StudentGroup).IsRequired(false);
			entity.Property(e => e.ReadingRequired).HasColumnName("reading_required").IsRequired(false);
			entity.Property(e => e.ReadingRecommended).HasColumnName("reading_recommended").IsRequired(false);
			entity.Property(e => e.ReadingSessionMaterial).HasColumnName("reading_sessionmaterial").IsRequired(false);
			entity.Property(e => e.KeyConcept).IsRequired(false);
			entity.Property(e => e.Equipment).IsRequired(false);
			entity.Property(e => e.Notes).IsRequired(false);
			entity.Property(e => e.ModifyDate).IsRequired(false);
			entity.Property(e => e.ModifyPersonId).IsRequired(false);
			entity.Property(e => e.PaceOrder).HasColumnName("pace_order").IsRequired(false);
			entity.Property(e => e.Vocabulary).IsRequired(false);
			entity.Property(e => e.Supplemental).IsRequired(false);
			entity.Property(e => e.OfferingNotes).IsRequired(false);
			entity.Property(e => e.SeqNumb).IsRequired(false);
			entity.Property(e => e.SvmBlockId).HasColumnName("SVM_blockID").IsRequired(false);
			entity.Property(e => e.MediasiteSchedule).IsRequired(false);
			entity.Property(e => e.MediasitePresentation).IsRequired(false);
			entity.Property(e => e.MediasiteLive).IsRequired(false);
			entity.Property(e => e.MediasiteTemplate).IsRequired(false);
			entity.Property(e => e.CanvasCourseId).IsRequired(false);
			entity.Property(e => e.CanvasEventId).IsRequired(false);
		});
    }
}
