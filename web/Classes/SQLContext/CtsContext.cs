using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Viper.Models.CTS;

namespace Viper.Classes.SQLContext;

public partial class VIPERContext : DbContext
{
    public virtual DbSet<Competency> Competencies { get; set; }

    public virtual DbSet<Domain> Domains { get; set; }

    /* Students */
    public virtual DbSet<DvmStudent> DvmStudent { get; set; }

    /* Clinical Scheduler */
    public virtual DbSet<InstructorSchedule> InstructorSchedule { get; set; }

    public virtual DbSet<StudentSchedule> StudentSchedule { get; set; }

    /* CREST */
    public virtual DbSet<CourseSessionOffering> CourseSessionOffering { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
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
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Competency_Domain");

            entity.HasOne(d => d.Parent).WithMany(p => p.Children)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK_Competency_Competency");
        });

        modelBuilder.Entity<Domain>(entity =>
        {
            entity.ToTable("Domain", "cts");
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
            entity.HasKey(e => e.MothraId);
            entity.ToTable("vwInstructorSchedule", schema: "cts");
            entity.Property(e => e.MiddleName).IsRequired(false);
            entity.Property(e => e.MailId).IsRequired(false);
            entity.Property(e => e.Role).IsRequired(false);
            entity.Property(e => e.SubjCode).IsRequired(false);
            entity.Property(e => e.CrseNumb).IsRequired(false);
        });

        modelBuilder.Entity<StudentSchedule>(entity =>
        {
            entity.HasKey(e => e.MothraId);
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
        });

        modelBuilder.Entity<CourseSessionOffering>(entity =>
        {
            entity.HasKey(e => new { e.CourseId, e.SessionId, e.EduTaskOfferid });
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
