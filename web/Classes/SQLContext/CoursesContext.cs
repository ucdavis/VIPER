using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Viper.Models.Courses;

namespace Viper.Classes.SQLContext;

public partial class CoursesContext : DbContext
{
    public CoursesContext()
    {
    }

    public CoursesContext(DbContextOptions<CoursesContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Baseinfo> Baseinfos { get; set; }

    public virtual DbSet<CurrentAndFutureTermCode> CurrentAndFutureTermCodes { get; set; }

    public virtual DbSet<CurrentAndFutureTermCodesInCurrentAcademicYear> CurrentAndFutureTermCodesInCurrentAcademicYears { get; set; }

    public virtual DbSet<CurrentNonSummerTermCode> CurrentNonSummerTermCodes { get; set; }

    public virtual DbSet<DlBaseinfo> DlBaseinfos { get; set; }

    public virtual DbSet<DlGrademode> DlGrademodes { get; set; }

    public virtual DbSet<DlPoa> DlPoas { get; set; }

    public virtual DbSet<DlRoster> DlRosters { get; set; }

    public virtual DbSet<DlSupportPerson> DlSupportPeople { get; set; }

    public virtual DbSet<Grademode> Grademodes { get; set; }

    public virtual DbSet<Poa> Poas { get; set; }

    public virtual DbSet<Roster> Rosters { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    public virtual DbSet<SupportPerson> SupportPeople { get; set; }

    public virtual DbSet<Terminfo> Terminfos { get; set; }

    public virtual DbSet<VStudentEnrollmentWithCourseType> VStudentEnrollmentWithCourseTypes { get; set; }

    public virtual DbSet<VwCurrentTerm> VwCurrentTerms { get; set; }

    public virtual DbSet<VwPoaPidmName> VwPoaPidmNames { get; set; }

    public virtual DbSet<VwXtndBaseinfo> VwXtndBaseinfos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (HttpHelper.Settings != null)
        {
            optionsBuilder.UseSqlServer(HttpHelper.Settings["ConnectionStrings:Courses"]);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Baseinfo>(entity =>
        {
            entity.HasKey(e => e.BaseinfoPkey);

            entity.ToTable("baseinfo");

            entity.HasIndex(e => e.BaseinfoPkey, "baseinfo")
                .IsUnique()
                .HasFillFactor(90);

            entity.HasIndex(e => new { e.BaseinfoSubjCode, e.BaseinfoCrseNumb }, "baseinfo_subjCode_crseNumb").HasFillFactor(90);

            entity.HasIndex(e => e.BaseinfoTermCode, "baseinfo_termCode").HasFillFactor(90);

            entity.HasIndex(e => new { e.BaseinfoTermCode, e.BaseinfoCrn }, "baseinfo_termCode_crn")
                .IsUnique()
                .HasFillFactor(90);

            entity.HasIndex(e => new { e.BaseinfoTermCode, e.BaseinfoSubjCode, e.BaseinfoCrseNumb, e.BaseinfoSeqNumb }, "baseinfo_termCode_subjCode_crseNumb_seqNumb")
                .IsUnique()
                .HasFillFactor(90);

            entity.Property(e => e.BaseinfoPkey)
                .HasMaxLength(11)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("baseinfo_pkey");
            entity.Property(e => e.BaseinfoCollCode)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValueSql("('')")
                .HasColumnName("baseinfo_coll_code");
            entity.Property(e => e.BaseinfoCrn)
                .HasMaxLength(5)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("baseinfo_crn");
            entity.Property(e => e.BaseinfoCrseNumb)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("baseinfo_crse_numb");
            entity.Property(e => e.BaseinfoDeptCode)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasDefaultValueSql("('')")
                .HasColumnName("baseinfo_dept_code");
            entity.Property(e => e.BaseinfoDescTitle)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("baseinfo_desc_title");
            entity.Property(e => e.BaseinfoEnrollment).HasColumnName("baseinfo_enrollment");
            entity.Property(e => e.BaseinfoSeqNumb)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("baseinfo_seq_numb");
            entity.Property(e => e.BaseinfoSubjCode)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("baseinfo_subj_code");
            entity.Property(e => e.BaseinfoTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("baseinfo_term_code");
            entity.Property(e => e.BaseinfoTitle)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("baseinfo_title");
            entity.Property(e => e.BaseinfoUnitHigh)
                .HasColumnType("numeric(7, 2)")
                .HasColumnName("baseinfo_unit_high");
            entity.Property(e => e.BaseinfoUnitLow)
                .HasColumnType("numeric(7, 2)")
                .HasColumnName("baseinfo_unit_low");
            entity.Property(e => e.BaseinfoUnitType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValueSql("('F')")
                .IsFixedLength()
                .HasColumnName("baseinfo_unit_type");
            entity.Property(e => e.BaseinfoXlistFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValueSql("('N')")
                .IsFixedLength()
                .HasColumnName("baseinfo_xlist_flag");
            entity.Property(e => e.BaseinfoXlistGroup)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValueSql("('')")
                .HasColumnName("baseinfo_xlist_group");
        });

        modelBuilder.Entity<CurrentAndFutureTermCode>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("currentAndFutureTermCodes");

            entity.Property(e => e.TermAcademicYear)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("term_academic_year");
            entity.Property(e => e.TermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("term_code");
        });

        modelBuilder.Entity<CurrentAndFutureTermCodesInCurrentAcademicYear>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("currentAndFutureTermCodesInCurrentAcademicYear");

            entity.Property(e => e.TermAcademicYear)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("term_academic_year");
            entity.Property(e => e.TermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("term_code");
            entity.Property(e => e.TermTermType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("term_term_type");
        });

        modelBuilder.Entity<CurrentNonSummerTermCode>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("currentNonSummerTermCode");

            entity.Property(e => e.TermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("term_code");
        });

        modelBuilder.Entity<DlBaseinfo>(entity =>
        {
            entity.HasKey(e => e.BaseinfoPkey);

            entity.ToTable("DL_baseinfo");

            entity.HasIndex(e => e.BaseinfoPkey, "IX_baseinfo")
                .IsUnique()
                .HasFillFactor(90);

            entity.HasIndex(e => new { e.BaseinfoSubjCode, e.BaseinfoCrseNumb }, "baseinfo_subjCode_crseNumb").HasFillFactor(90);

            entity.HasIndex(e => e.BaseinfoTermCode, "baseinfo_termCode").HasFillFactor(90);

            entity.HasIndex(e => new { e.BaseinfoTermCode, e.BaseinfoCrn }, "baseinfo_termCode_crn")
                .IsUnique()
                .HasFillFactor(90);

            entity.HasIndex(e => new { e.BaseinfoTermCode, e.BaseinfoSubjCode, e.BaseinfoCrseNumb, e.BaseinfoSeqNumb }, "baseinfo_termCode_subjCode_crseNumb_seqNumb")
                .IsUnique()
                .HasFillFactor(90);

            entity.Property(e => e.BaseinfoPkey)
                .HasMaxLength(11)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("baseinfo_pkey");
            entity.Property(e => e.BaseinfoCollCode)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValueSql("('')")
                .HasColumnName("baseinfo_coll_code");
            entity.Property(e => e.BaseinfoCrn)
                .HasMaxLength(5)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("baseinfo_crn");
            entity.Property(e => e.BaseinfoCrseNumb)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("baseinfo_crse_numb");
            entity.Property(e => e.BaseinfoDeptCode)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasDefaultValueSql("('')")
                .HasColumnName("baseinfo_dept_code");
            entity.Property(e => e.BaseinfoDescTitle)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("baseinfo_desc_title");
            entity.Property(e => e.BaseinfoEnrollment).HasColumnName("baseinfo_enrollment");
            entity.Property(e => e.BaseinfoSeqNumb)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("baseinfo_seq_numb");
            entity.Property(e => e.BaseinfoSubjCode)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("baseinfo_subj_code");
            entity.Property(e => e.BaseinfoTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("baseinfo_term_code");
            entity.Property(e => e.BaseinfoTitle)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("baseinfo_title");
            entity.Property(e => e.BaseinfoUnitHigh)
                .HasColumnType("numeric(7, 2)")
                .HasColumnName("baseinfo_unit_high");
            entity.Property(e => e.BaseinfoUnitLow)
                .HasColumnType("numeric(7, 2)")
                .HasColumnName("baseinfo_unit_low");
            entity.Property(e => e.BaseinfoUnitType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValueSql("('F')")
                .IsFixedLength()
                .HasColumnName("baseinfo_unit_type");
            entity.Property(e => e.BaseinfoXlistFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValueSql("('N')")
                .IsFixedLength()
                .HasColumnName("baseinfo_xlist_flag");
            entity.Property(e => e.BaseinfoXlistGroup)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValueSql("('')")
                .HasColumnName("baseinfo_xlist_group");
        });

        modelBuilder.Entity<DlGrademode>(entity =>
        {
            entity.HasKey(e => e.GmodePkey).HasName("DL_grademode_pkey");

            entity.ToTable("DL_grademode");

            entity.HasIndex(e => e.GmodePkey, "gmode_pkey").HasFillFactor(90);

            entity.HasIndex(e => new { e.GmodeTermCode, e.GmodeCrn }, "gmode_termCode_CRN").HasFillFactor(90);

            entity.Property(e => e.GmodePkey)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("gmode_pkey");
            entity.Property(e => e.GmodeCode)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("gmode_code");
            entity.Property(e => e.GmodeCrn)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("gmode_crn");
            entity.Property(e => e.GmodeTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("gmode_term_code");
        });

        modelBuilder.Entity<DlPoa>(entity =>
        {
            entity.HasKey(e => e.PoaPkey);

            entity.ToTable("DL_poa");

            entity.Property(e => e.PoaPkey)
                .HasMaxLength(19)
                .IsUnicode(false)
                .HasColumnName("poa_pkey");
            entity.Property(e => e.PoaCrn)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("poa_crn");
            entity.Property(e => e.PoaPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("poa_pidm");
            entity.Property(e => e.PoaRole).HasColumnName("poa_role");
            entity.Property(e => e.PoaTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("poa_term_code");
        });

        modelBuilder.Entity<DlRoster>(entity =>
        {
            entity.HasKey(e => e.RosterPkey);

            entity.ToTable("DL_roster");

            entity.HasIndex(e => e.RosterPkey, "IX_roster")
                .IsUnique()
                .HasFillFactor(90);

            entity.Property(e => e.RosterPkey)
                .HasMaxLength(19)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("roster_pkey");
            entity.Property(e => e.RosterCrn)
                .HasMaxLength(5)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("roster_crn");
            entity.Property(e => e.RosterEnrollStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("roster_enroll_status");
            entity.Property(e => e.RosterGradeMode)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("roster_grade_mode");
            entity.Property(e => e.RosterLevelCode)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("roster_level_code");
            entity.Property(e => e.RosterPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("roster_pidm");
            entity.Property(e => e.RosterTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("roster_term_code");
            entity.Property(e => e.RosterUnit).HasColumnName("roster_unit");
        });

        modelBuilder.Entity<DlSupportPerson>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("DL_supportPeople");

            entity.HasIndex(e => e.SupportCourseId, "IX_DL_supportPeople").HasFillFactor(90);

            entity.Property(e => e.SupportClientId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("support_clientID");
            entity.Property(e => e.SupportCourseId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("support_course_id");
            entity.Property(e => e.SupportPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("support_pidm");
            entity.Property(e => e.SupportRole)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("support_role");
            entity.Property(e => e.SupportTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("support_term_code");
        });

        modelBuilder.Entity<Grademode>(entity =>
        {
            entity.HasKey(e => e.GmodePkey).HasName("grademode_pkey");

            entity.ToTable("grademode");

            entity.HasIndex(e => new { e.GmodeTermCode, e.GmodeCrn }, "grademode_termCode_CRN").HasFillFactor(90);

            entity.Property(e => e.GmodePkey)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("gmode_pkey");
            entity.Property(e => e.GmodeCode)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("gmode_code");
            entity.Property(e => e.GmodeCrn)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("gmode_crn");
            entity.Property(e => e.GmodeTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("gmode_term_code");
        });

        modelBuilder.Entity<Poa>(entity =>
        {
            entity.HasKey(e => e.PoaPkey);

            entity.ToTable("poa");

            entity.HasIndex(e => e.PoaPkey, "poa_pKey").HasFillFactor(90);

            entity.HasIndex(e => new { e.PoaTermCode, e.PoaCrn }, "poa_termCodeCRN").HasFillFactor(90);

            entity.HasIndex(e => new { e.PoaTermCode, e.PoaCrn, e.PoaPidm }, "poa_termCodeCRNPidm").HasFillFactor(90);

            entity.Property(e => e.PoaPkey)
                .HasMaxLength(19)
                .IsUnicode(false)
                .HasColumnName("poa_pkey");
            entity.Property(e => e.PoaCrn)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("poa_crn");
            entity.Property(e => e.PoaPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("poa_pidm");
            entity.Property(e => e.PoaRole).HasColumnName("poa_role");
            entity.Property(e => e.PoaTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("poa_term_code");
        });

        modelBuilder.Entity<Roster>(entity =>
        {
            entity.HasKey(e => e.RosterPkey);

            entity.ToTable("roster");

            entity.HasIndex(e => e.RosterPkey, "roster_pKey")
                .IsUnique()
                .HasFillFactor(90);

            entity.HasIndex(e => e.RosterTermCode, "roster_termCode").HasFillFactor(90);

            entity.HasIndex(e => new { e.RosterTermCode, e.RosterCrn }, "roster_termCodeCRN").HasFillFactor(90);

            entity.HasIndex(e => new { e.RosterTermCode, e.RosterCrn, e.RosterPidm }, "roster_termCodeCRNPidm").HasFillFactor(90);

            entity.Property(e => e.RosterPkey)
                .HasMaxLength(19)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("roster_pkey");
            entity.Property(e => e.RosterCrn)
                .HasMaxLength(5)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("roster_crn");
            entity.Property(e => e.RosterEnrollStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("roster_enroll_status");
            entity.Property(e => e.RosterGradeMode)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("roster_grade_mode");
            entity.Property(e => e.RosterLevelCode)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("roster_level_code");
            entity.Property(e => e.RosterPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("roster_pidm");
            entity.Property(e => e.RosterTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("roster_term_code");
            entity.Property(e => e.RosterUnit).HasColumnName("roster_unit");
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.StatusRecordId).HasName("PK_DL_status");

            entity.ToTable("status");

            entity.HasIndex(e => e.StatusRecordId, "status_recordID").HasFillFactor(90);

            entity.HasIndex(e => e.StatusTermCode, "status_term_code").HasFillFactor(90);

            entity.HasIndex(e => new { e.StatusTermCode, e.StatusTableName }, "status_timestampTermCodeTableName").HasFillFactor(90);

            entity.Property(e => e.StatusRecordId).HasColumnName("status_recordID");
            entity.Property(e => e.StatusDatetime)
                .HasColumnType("datetime")
                .HasColumnName("status_datetime");
            entity.Property(e => e.StatusRecordCount).HasColumnName("status_recordCount");
            entity.Property(e => e.StatusTableName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status_table_name");
            entity.Property(e => e.StatusTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("status_term_code");
        });

        modelBuilder.Entity<SupportPerson>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("supportPeople");

            entity.HasIndex(e => e.SupportCourseId, "support_courseID").HasFillFactor(90);

            entity.HasIndex(e => e.SupportPidm, "support_pidm").HasFillFactor(90);

            entity.HasIndex(e => e.SupportTermCode, "support_termCode").HasFillFactor(90);

            entity.HasIndex(e => new { e.SupportTermCode, e.SupportPidm }, "support_termCodePidm").HasFillFactor(90);

            entity.HasIndex(e => new { e.SupportTermCode, e.SupportCourseId }, "support_termCourseID").HasFillFactor(90);

            entity.HasIndex(e => new { e.SupportTermCode, e.SupportRole }, "support_termRole").HasFillFactor(90);

            entity.Property(e => e.SupportClientId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("support_clientID");
            entity.Property(e => e.SupportCourseId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("support_course_id");
            entity.Property(e => e.SupportPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("support_pidm");
            entity.Property(e => e.SupportRole)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("support_role");
            entity.Property(e => e.SupportTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("support_term_code");
        });

        modelBuilder.Entity<Terminfo>(entity =>
        {
            entity.HasKey(e => new { e.TermCode, e.TermCollCode });

            entity.ToTable("terminfo");

            entity.HasIndex(e => e.TermAcademicYear, "academicYear").HasFillFactor(90);

            entity.HasIndex(e => e.TermCode, "termCode").HasFillFactor(90);

            entity.HasIndex(e => new { e.TermCode, e.TermCollCode }, "termCode_collegeCode")
                .IsUnique()
                .HasFillFactor(90);

            entity.HasIndex(e => e.TermAidYear, "term_aid_year").HasFillFactor(90);

            entity.Property(e => e.TermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("term_code");
            entity.Property(e => e.TermCollCode)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("term_coll_code");
            entity.Property(e => e.TermAcademicYear)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("term_academic_year");
            entity.Property(e => e.TermAcademicYearDescription)
                .HasMaxLength(14)
                .IsUnicode(false)
                .HasComputedColumnSql("((CONVERT([varchar](9),CONVERT([int],[term_academic_year],(0))-(1),(0))+'-')+[term_academic_year])", false)
                .HasColumnName("term_academic_year_description");
            entity.Property(e => e.TermAidYear)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasComputedColumnSql("(right(CONVERT([varchar](4),CONVERT([int],[term_academic_year]-(1),0),0),(2))+right([term_academic_year],(2)))", false)
                .HasColumnName("term_aid_year");
            entity.Property(e => e.TermCurrentTerm).HasColumnName("term_current_term");
            entity.Property(e => e.TermCurrentTermMulti).HasColumnName("term_current_term_multi");
            entity.Property(e => e.TermDesc)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("term_desc");
            entity.Property(e => e.TermEndDate)
                .HasColumnType("datetime")
                .HasColumnName("term_end_date");
            entity.Property(e => e.TermStartDate)
                .HasColumnType("datetime")
                .HasColumnName("term_start_date");
            entity.Property(e => e.TermTermType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValueSql("('Q')")
                .HasColumnName("term_term_type");
        });

        modelBuilder.Entity<VStudentEnrollmentWithCourseType>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vStudentEnrollmentWithCourseType");

            entity.Property(e => e.BaseinfoCrseNumb)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("baseinfo_crse_numb");
            entity.Property(e => e.BaseinfoSeqNumb)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("baseinfo_seq_numb");
            entity.Property(e => e.BaseinfoSubjCode)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("baseinfo_subj_code");
            entity.Property(e => e.BaseinfoTitle)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("baseinfo_title");
            entity.Property(e => e.Blocktype)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("blocktype");
            entity.Property(e => e.RosterCrn)
                .HasMaxLength(5)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("roster_crn");
            entity.Property(e => e.RosterGradeMode)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("roster_grade_mode");
            entity.Property(e => e.RosterPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("roster_pidm");
            entity.Property(e => e.RosterPkey)
                .HasMaxLength(19)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("roster_pkey");
            entity.Property(e => e.RosterTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("roster_term_code");
            entity.Property(e => e.RosterUnit).HasColumnName("roster_unit");
            entity.Property(e => e.UcdipcBeginTerm)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("ucdipc_begin_term");
            entity.Property(e => e.UcdipcEndTerm)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("ucdipc_end_term");
        });

        modelBuilder.Entity<VwCurrentTerm>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_current_term");

            entity.Property(e => e.TermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("term_code");
        });

        modelBuilder.Entity<VwPoaPidmName>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_poa_pidm_name");

            entity.Property(e => e.IdsPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_pidm");
            entity.Property(e => e.PersonClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_clientid");
            entity.Property(e => e.PersonDisplayFullName)
                .HasMaxLength(91)
                .IsUnicode(false)
                .HasColumnName("person_display_full_name");
            entity.Property(e => e.PersonTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_term_code");
        });

        modelBuilder.Entity<VwXtndBaseinfo>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_xtnd_baseinfo");

            entity.Property(e => e.BaseinfoCollCode)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("baseinfo_coll_code");
            entity.Property(e => e.BaseinfoCrn)
                .HasMaxLength(5)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("baseinfo_crn");
            entity.Property(e => e.BaseinfoCrseNumb)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("baseinfo_crse_numb");
            entity.Property(e => e.BaseinfoDeptCode)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("baseinfo_dept_code");
            entity.Property(e => e.BaseinfoEnrollment).HasColumnName("baseinfo_enrollment");
            entity.Property(e => e.BaseinfoPkey)
                .HasMaxLength(11)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("baseinfo_pkey");
            entity.Property(e => e.BaseinfoSeqNumb)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("baseinfo_seq_numb");
            entity.Property(e => e.BaseinfoSubjCode)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("baseinfo_subj_code");
            entity.Property(e => e.BaseinfoTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("baseinfo_term_code");
            entity.Property(e => e.CustodialDeptCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("custodial_dept_code");
            entity.Property(e => e.PoaClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("poa_clientid");
            entity.Property(e => e.PoaMailid)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("poa_mailid");
            entity.Property(e => e.PoaPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("poa_pidm");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
