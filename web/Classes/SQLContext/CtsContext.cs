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
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<Bundle> Bundles { get; set; }
    public virtual DbSet<BundleCompetency> BundleCompetencies { get; set; }
    public virtual DbSet<BundleCompetencyGroup> BundleCompetencyGroups { get; set; }
    public virtual DbSet<BundleLevel> BundleLevels { get; set; }
    public virtual DbSet<BundleRole> BundleRoles { get; set; }
    public virtual DbSet<BundleService> BundleServices { get; set; }
    public virtual DbSet<CompetencyOutcome> CompetencyOutcomes { get; set; }
    public virtual DbSet<CourseCompetency> CourseCompetencies { get; set; }
    public virtual DbSet<CourseRole> CourseRoles { get; set; }
    public virtual DbSet<MilestoneLevel> MilestoneLevels { get; set; }
    public virtual DbSet<Patient> Patients { get; set; }
    public virtual DbSet<SessionCompetency> SessionCompetencies { get; set; }
    public virtual DbSet<StudentCompetency> StudentCompetencies { get; set; }

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

    /* Eval */
    public virtual DbSet<Instance> Instances { get; set; }
    public virtual DbSet<EvaluateesByInstance> EvaluateesByInstances { get; set; }

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

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role", "cts");

            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .IsUnicode(false);
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

        modelBuilder.Entity<BundleLevel>(entity =>
        {
            entity.ToTable("BundleLevel", "cts");

            entity.HasOne(d => d.Bundle).WithMany(p => p.BundleLevels)
                .HasForeignKey(d => d.BundleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BundleLevel_Bundle");

            entity.HasOne(d => d.Level).WithMany()
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
            entity.HasOne(d => d.Role).WithMany()
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
            entity.HasOne(d => d.Service).WithMany()
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CompetencyOutcome>(entity =>
        {
            entity.ToTable("CompetencyOutcome", "cts");
        });

        modelBuilder.Entity<CourseCompetency>(entity =>
        {
            entity.ToTable("CourseCompetency", "cts");
            entity.HasOne(d => d.Course).WithMany()
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CourseRole>(entity =>
        {
            entity.ToTable("CourseRole", "cts");
            entity.HasOne(d => d.Course).WithMany()
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.Role).WithMany()
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<MilestoneLevel>(entity =>
        {
            entity.ToTable("MilestoneLevel", "cts");

            entity.Property(e => e.Description).IsUnicode(false);

            entity.HasOne(d => d.Bundle).WithMany(p => p.MilestoneLevels)
                .HasForeignKey(d => d.BundleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MilestoneLevel_Bundle");
            entity.HasOne(d => d.Level).WithMany()
                    .HasForeignKey(d => d.LevelId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MilestoneLevel_MilestoneLevel");
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
            entity.HasOne(d => d.Encounter).WithOne(d => d.Patient)
                .HasForeignKey<Encounter>(d => d.PatientId)
                .IsRequired(false);

        });

        modelBuilder.Entity<SessionCompetency>(entity =>
        {
            entity.ToTable("SessionCompetency", "cts");
            entity.HasKey(e => e.SessionCompetencyId);
            entity.HasOne(d => d.Competency).WithMany()
                .HasForeignKey(d => d.CompetencyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SessionCompetency_Competency");
            entity.HasOne(d => d.Session).WithMany()
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.Level).WithMany()
                .HasForeignKey(d => d.LevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SessionCompetency_Level");
            entity.HasOne(d => d.Role).WithMany()
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SessionCompetency_Role")
                .IsRequired(false);

        });

        modelBuilder.Entity<StudentCompetency>(entity =>
        {
            entity.ToTable("StudentCompetency", "cts");

            entity.Property(e => e.Added).HasColumnType("datetime");
            entity.Property(e => e.Updated).HasColumnType("datetime");
            entity.Property(e => e.VerifiedTimestamp).HasColumnType("datetime");

            entity.HasOne(d => d.BundleGroup).WithMany()
                .HasForeignKey(d => d.BundleGroupId)
                .HasConstraintName("FK_StudentCompetency_BundleCompetencyGroupId");

            entity.HasOne(d => d.Bundle).WithMany()
                .HasForeignKey(d => d.BundleId)
                .HasConstraintName("FK_StudentCompetency_Bundle");

            entity.HasOne(d => d.Person).WithMany()
                .HasForeignKey(d => d.StudentUserId)
                .HasConstraintName("FK_StudentCompetency_Student")
                .OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.Competency).WithMany()
                .HasForeignKey(d => d.CompetencyId)
                .HasConstraintName("FK_StudentCompetency_Competency")
                .OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.Level).WithMany()
                .HasForeignKey(d => d.LevelId)
                .HasConstraintName("FK_StudentCompetency_Level")
                .OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.Encounter).WithMany()
                .HasForeignKey(d => d.EncounterId)
                .HasConstraintName("FK_StudentCompetency_Encounter")
                .OnDelete(DeleteBehavior.ClientSetNull)
                .IsRequired(false);
            entity.HasOne(d => d.Course).WithMany()
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .IsRequired(false);
            entity.HasOne(d => d.Session).WithMany()
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .IsRequired(false);
            entity.HasOne(d => d.Verifier).WithMany()
                .HasForeignKey(d => d.VerifiedBy)
                .HasConstraintName("FK_StudentCompetency_VerifiedPerson")
                .OnDelete(DeleteBehavior.ClientSetNull)
                .IsRequired(false);
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

        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("vwCourse", schema: "cts");
            entity.HasKey(e => e.CourseId);
            entity.Property(e => e.Description).IsRequired(false);
            entity.Property(e => e.Crn).IsRequired(false);
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("vwSession", schema: "cts");
            entity.HasKey(e => e.SessionId);
            entity.Property(e => e.Type).IsRequired(false);
            entity.Property(e => e.TypeDescription).IsRequired(false);
            entity.Property(e => e.Description).IsRequired(false);
        });

        /*
        modelBuilder.Entity<Outcome>(entity =>
        {

        });
        */

        modelBuilder.Entity<Instance>(entity =>
        {
            entity.HasKey(e => e.InstanceId);
            entity.ToTable("vwInstances", schema: "eval");
            entity.Property(e => e.InstanceMode).IsRequired(false);
            entity.Property(e => e.InstanceDueDate).IsRequired(false);
            entity.Property(e => e.InstanceStartWeek).IsRequired(false);
            entity.Property(e => e.InstanceStartWeekId).IsRequired(false);
        });


        modelBuilder.Entity<EvaluateesByInstance>(entity =>
        {
            entity.HasKey(e => e.EvaluateeId);
            entity.ToTable("vwEvaluateesByInstances", schema: "eval");
        });
    }
}
