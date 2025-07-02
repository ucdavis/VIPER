using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Viper.Models.AAUD;

namespace Viper.Classes.SQLContext;

public partial class AAUDContext : DbContext
{
#pragma warning disable CS8618
    public AAUDContext()
    {
    }

    public AAUDContext(DbContextOptions<AAUDContext> options)
        : base(options)
    {
    }
#pragma warning restore CS8618

    public virtual DbSet<AaudOverride> AaudOverrides { get; set; }

    public virtual DbSet<AaudOverrideJob> AaudOverrideJobs { get; set; }

    public virtual DbSet<AaudUser> AaudUsers { get; set; }

    public virtual DbSet<Clicker> Clickers { get; set; }

    public virtual DbSet<DlEmployee> DlEmployees { get; set; }

    public virtual DbSet<DlFlag> DlFlags { get; set; }

    public virtual DbSet<DlGradEmployee> DlGradEmployees { get; set; }

    public virtual DbSet<DlGradFlag> DlGradFlags { get; set; }

    public virtual DbSet<DlGradId> DlGradIds { get; set; }

    public virtual DbSet<DlGradPerson> DlGradPeople { get; set; }

    public virtual DbSet<DlGradStudent> DlGradStudents { get; set; }

    public virtual DbSet<DlId> DlIds { get; set; }

    public virtual DbSet<DlJob> DlJobs { get; set; }

    public virtual DbSet<DlPerson> DlPeople { get; set; }

    public virtual DbSet<DlStudent> DlStudents { get; set; }

    public virtual DbSet<DlUnexPerson> DlUnexPeople { get; set; }

    public virtual DbSet<DlUnexRoster> DlUnexRosters { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<ExceptionDeactivate> ExceptionDeactivates { get; set; }

    public virtual DbSet<Exceptionemployee> Exceptionemployees { get; set; }

    public virtual DbSet<Exceptionflag> Exceptionflags { get; set; }

    public virtual DbSet<Exceptionid> Exceptionids { get; set; }

    public virtual DbSet<Exceptionperson> Exceptionpeople { get; set; }

    public virtual DbSet<Exceptionstudent> Exceptionstudents { get; set; }

    public virtual DbSet<ExternalId> ExternalIds { get; set; }

    public virtual DbSet<FakeEmployee> FakeEmployees { get; set; }

    public virtual DbSet<FakeFlag> FakeFlags { get; set; }

    public virtual DbSet<FakeId> FakeIds { get; set; }

    public virtual DbSet<FakePerson> FakePeople { get; set; }

    public virtual DbSet<Flag> Flags { get; set; }

    public virtual DbSet<HelpDeskUser> HelpDeskUsers { get; set; }

    public virtual DbSet<Id> Ids { get; set; }

    public virtual DbSet<Job> Jobs { get; set; }

    public virtual DbSet<Ldap> Ldaps { get; set; }

    public virtual DbSet<LdapDepartment> LdapDepartments { get; set; }

    public virtual DbSet<LdapFacilityLink> LdapFacilityLinks { get; set; }

    public virtual DbSet<NewEmployeeNotification> NewEmployeeNotifications { get; set; }

    public virtual DbSet<NightlyJob> NightlyJobs { get; set; }

    public virtual DbSet<Person> People { get; set; }

    public virtual DbSet<Relationship> Relationships { get; set; }

    public virtual DbSet<RelationshipType> RelationshipTypes { get; set; }

    public virtual DbSet<RelationshipsAudit> RelationshipsAudits { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Studentgrp> Studentgrps { get; set; }

    public virtual DbSet<TestEmployee> TestEmployees { get; set; }

    public virtual DbSet<TestFlag> TestFlags { get; set; }

    public virtual DbSet<TestId> TestIds { get; set; }

    public virtual DbSet<TestPerson> TestPeople { get; set; }

    public virtual DbSet<TestStudent> TestStudents { get; set; }

    public virtual DbSet<UnexPerson> UnexPeople { get; set; }

    public virtual DbSet<UnexRoster> UnexRosters { get; set; }

    public virtual DbSet<VwAdStaff> VwAdStaffs { get; set; }

    public virtual DbSet<VwAdStudent> VwAdStudents { get; set; }

    public virtual DbSet<VwAdTeachingFaculty> VwAdTeachingFaculties { get; set; }

    public virtual DbSet<VwAdVetstaff> VwAdVetstaffs { get; set; }

    public virtual DbSet<VwAdVmssacscheduler> VwAdVmssacschedulers { get; set; }

    public virtual DbSet<VwAdconstituent> VwAdconstituents { get; set; }

    public virtual DbSet<VwCurrentAffiliate> VwCurrentAffiliates { get; set; }

    public virtual DbSet<VwCurrentAffiliatesForPf> VwCurrentAffiliatesForPfs { get; set; }

    public virtual DbSet<VwCurrentAffiliatesForPfBk> VwCurrentAffiliatesForPfBks { get; set; }

    public virtual DbSet<VwDvmStudent> VwDvmStudents { get; set; }

    public virtual DbSet<VwDvmStudentsHistory> VwDvmStudentsHistories { get; set; }

    public virtual DbSet<VwDvmStudentsMaxTerm> VwDvmStudentsMaxTerms { get; set; }

    public virtual DbSet<VwDvmStudentsMaxTermBk> VwDvmStudentsMaxTermBks { get; set; }

    public virtual DbSet<VwDvmStudentsMaxTermNew> VwDvmStudentsMaxTermNews { get; set; }

    public virtual DbSet<VwEmployeesForAaud> VwEmployeesForAauds { get; set; }

    public virtual DbSet<VwException> VwExceptions { get; set; }

    public virtual DbSet<VwJobsForAaud> VwJobsForAauds { get; set; }

    public virtual DbSet<VwMailIdsForStudent> VwMailIdsForStudents { get; set; }

    public virtual DbSet<VwPapercutUser> VwPapercutUsers { get; set; }

    public virtual DbSet<VwPerfectFormsConstituent> VwPerfectFormsConstituents { get; set; }

    public virtual DbSet<VwSftFaculty> VwSftFaculties { get; set; }

    public virtual DbSet<VwSpSvmemployee> VwSpSvmemployees { get; set; }

    public virtual DbSet<VwSpSvmperson> VwSpSvmpeople { get; set; }

    public virtual DbSet<VwUConnect> VwUConnects { get; set; }

    public virtual DbSet<VwUConnectNew> VwUConnectNews { get; set; }

    public virtual DbSet<VwUConnectUnit> VwUConnectUnits { get; set; }

    public virtual DbSet<VwVmthAllEmployesExcludingClinician> VwVmthAllEmployesExcludingClinicians { get; set; }

    public virtual DbSet<VwVmthClinician> VwVmthClinicians { get; set; }

    public virtual DbSet<VwVmthConstituent> VwVmthConstituents { get; set; }

    public virtual DbSet<VwVmthStaff> VwVmthStaffs { get; set; }

    public virtual DbSet<VwVmthStudent> VwVmthStudents { get; set; }

    public virtual DbSet<VwVmthStudentsForPerfectForm> VwVmthStudentsForPerfectForms { get; set; }

    public virtual DbSet<ExampleComment> ExampleComments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (HttpHelper.Settings != null)
        {
            optionsBuilder.UseSqlServer(HttpHelper.Settings["ConnectionStrings:AAUD"]);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AaudOverride>(entity =>
        {
            entity.HasKey(e => e.OverrideId).HasName("PK_aaudOverride");

            entity.ToTable("aaud_override");

            entity.Property(e => e.OverrideId).HasColumnName("override_id");
            entity.Property(e => e.EffectiveDate)
                .HasColumnType("datetime")
                .HasColumnName("effective_date");
            entity.Property(e => e.EmpAltDeptCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("emp_alt_dept_code");
            entity.Property(e => e.EmpEffortHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("emp_effort_home_dept");
            entity.Property(e => e.EmpEffortTitleCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("emp_effort_title_code");
            entity.Property(e => e.EmpHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("emp_home_dept");
            entity.Property(e => e.EmpPrimaryTitle)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("emp_primary_title");
            entity.Property(e => e.EmpTeachingHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("emp_teaching_home_dept");
            entity.Property(e => e.EmpTeachingPercentFulltime)
                .HasColumnType("numeric(5, 4)")
                .HasColumnName("emp_teaching_percent_fulltime");
            entity.Property(e => e.EmpTeachingTitleCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("emp_teaching_title_code");
            entity.Property(e => e.EndDate)
                .HasColumnType("datetime")
                .HasColumnName("end_date");
            entity.Property(e => e.EnteredBy)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("entered_by");
            entity.Property(e => e.EnteredOn)
                .HasColumnType("datetime")
                .HasColumnName("entered_on");
            entity.Property(e => e.FlagsAcademic).HasColumnName("flags_academic");
            entity.Property(e => e.FlagsStaff).HasColumnName("flags_staff");
            entity.Property(e => e.FlagsStudent).HasColumnName("flags_student");
            entity.Property(e => e.FlagsSvmPeople).HasColumnName("flags_svm_people");
            entity.Property(e => e.FlagsSvmStudent).HasColumnName("flags_svm_student");
            entity.Property(e => e.FlagsTeachingFaculty).HasColumnName("flags_teaching_faculty");
            entity.Property(e => e.FlagsWosemp).HasColumnName("flags_wosemp");
            entity.Property(e => e.MothraId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("mothraID");
            entity.Property(e => e.Note)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("note");
        });

        modelBuilder.Entity<AaudOverrideJob>(entity =>
        {
            entity.HasKey(e => e.OverrideJobsId);

            entity.ToTable("aaud_override_jobs");

            entity.Property(e => e.OverrideJobsId).HasColumnName("override_jobs_id");
            entity.Property(e => e.JobBargainingUnit)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("job_bargaining_unit");
            entity.Property(e => e.JobDepartmentCode)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("job_department_code");
            entity.Property(e => e.JobPercentFulltime)
                .HasColumnType("numeric(7, 3)")
                .HasColumnName("job_percent_fulltime");
            entity.Property(e => e.JobSchoolDivision)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("job_school_division");
            entity.Property(e => e.JobSeqNum).HasColumnName("job_seq_num");
            entity.Property(e => e.JobTitleCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("job_title_code");
            entity.Property(e => e.OverrideId).HasColumnName("override_id");

            entity.HasOne(d => d.Override).WithMany(p => p.AaudOverrideJobs)
                .HasForeignKey(d => d.OverrideId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_aaud_override_jobs_aaud_override");
        });

        modelBuilder.Entity<AaudUser>(entity =>
        {
            entity.ToTable("aaudUser");

            entity.HasIndex(e => e.LoginId, "IX_aaudUser_loginID");

            entity.HasIndex(e => e.MothraId, "IX_aaudUser_mothraID");

            entity.HasIndex(e => e.CurrentEmployee, "idx_aauduser_currentemployee");

            entity.HasIndex(e => e.SpridenId, "idx_aauduser_spridenid");

            entity.Property(e => e.AaudUserId).HasColumnName("aaudUserID");
            entity.Property(e => e.Added)
                .HasColumnType("datetime")
                .HasColumnName("added");
            entity.Property(e => e.ClientId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("clientID");
            entity.Property(e => e.Current)
                .HasComputedColumnSql("(case when [current_student]=(1) OR [current_employee]=(1) then (1) else (0) end)", false)
                .HasColumnName("current");
            entity.Property(e => e.CurrentEmployee).HasColumnName("current_employee");
            entity.Property(e => e.CurrentStudent).HasColumnName("current_student");
            entity.Property(e => e.DisplayFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("display_first_name");
            entity.Property(e => e.DisplayFullName)
                .HasMaxLength(91)
                .IsUnicode(false)
                .HasComputedColumnSql("(([display_first_name]+' ')+[display_last_name])", false)
                .HasColumnName("display_full_name");
            entity.Property(e => e.DisplayLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("display_last_name");
            entity.Property(e => e.DisplayMiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("display_middle_name");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("employee_id");
            entity.Property(e => e.EmployeePKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("employee_pKey");
            entity.Property(e => e.EmployeeTerm).HasColumnName("employee_term");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("first_name");
            entity.Property(e => e.Future)
                .HasComputedColumnSql("(case when [future_student]=(1) OR [future_employee]=(1) then (1) else (0) end)", false)
                .HasColumnName("future");
            entity.Property(e => e.FutureEmployee).HasColumnName("future_employee");
            entity.Property(e => e.FutureStudent).HasColumnName("future_student");
            entity.Property(e => e.IamId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("iam_id");
            entity.Property(e => e.LastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("last_name");
            entity.Property(e => e.LoginId)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("loginID");
            entity.Property(e => e.MailId)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("mailID");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("middle_name");
            entity.Property(e => e.MivId).HasColumnName("miv_id");
            entity.Property(e => e.MothraId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("mothraID");
            entity.Property(e => e.Pidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("PIDM");
            entity.Property(e => e.PpsId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("pps_id");
            entity.Property(e => e.Ross).HasColumnName("ross");
            entity.Property(e => e.SpridenId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("spriden_id");
            entity.Property(e => e.StudentPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("student_pKey");
            entity.Property(e => e.StudentTerm).HasColumnName("student_term");
            entity.Property(e => e.UnexId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("unex_id");
            entity.Property(e => e.VmacsId).HasColumnName("vmacs_id");
            entity.Property(e => e.VmcasId)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("vmcas_id");
        });

        modelBuilder.Entity<Clicker>(entity =>
        {
            entity.HasKey(e => e.ClickerStudentId);

            entity.Property(e => e.ClickerStudentId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("Clicker_Student_ID");
            entity.Property(e => e.ClickerClickerId)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("Clicker_Clicker_ID");
        });

        modelBuilder.Entity<DlEmployee>(entity =>
        {
            entity.HasKey(e => e.EmpPKey);

            entity.ToTable("DL_employees");

            entity.HasIndex(e => e.EmpPKey, "IX_DL_employees")
                .IsUnique()
                .HasFillFactor(90);

            entity.HasIndex(e => e.EmpClientid, "IX_DL_employees_clientid").HasFillFactor(90);

            entity.HasIndex(e => e.EmpTermCode, "IX_DL_employees_term").HasFillFactor(90);

            entity.HasIndex(e => new { e.EmpTermCode, e.EmpClientid }, "IX_DL_employees_term_clientid").HasFillFactor(90);

            entity.Property(e => e.EmpPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_pKey");
            entity.Property(e => e.EmpAltDeptCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasDefaultValueSql("('')")
                .IsFixedLength()
                .HasColumnName("emp_alt_dept_code");
            entity.Property(e => e.EmpCbuc)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("emp_cbuc");
            entity.Property(e => e.EmpClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_clientid");
            entity.Property(e => e.EmpEffortHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_effort_home_dept");
            entity.Property(e => e.EmpEffortTitleCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_effort_title_code");
            entity.Property(e => e.EmpHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_home_dept");
            entity.Property(e => e.EmpPrimaryTitle)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_primary_title");
            entity.Property(e => e.EmpSchoolDivision)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_school_division");
            entity.Property(e => e.EmpStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_status");
            entity.Property(e => e.EmpTeachingHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("emp_teaching_home_dept");
            entity.Property(e => e.EmpTeachingPercentFulltime)
                .HasColumnType("numeric(5, 4)")
                .HasColumnName("emp_teaching_percent_fulltime");
            entity.Property(e => e.EmpTeachingTitleCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasDefaultValueSql("('')")
                .HasColumnName("emp_teaching_title_code");
            entity.Property(e => e.EmpTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_term_code");
        });

        modelBuilder.Entity<DlFlag>(entity =>
        {
            entity.HasKey(e => e.FlagsPKey);

            entity.ToTable("DL_flags");

            entity.HasIndex(e => e.FlagsClientid, "IX_DL_flags_clientid").HasFillFactor(90);

            entity.HasIndex(e => e.FlagsPKey, "IX_DL_flags_pKey")
                .IsUnique()
                .HasFillFactor(90);

            entity.HasIndex(e => e.FlagsTermCode, "IX_DL_flags_term").HasFillFactor(90);

            entity.HasIndex(e => new { e.FlagsTermCode, e.FlagsClientid }, "IX_DL_flags_term_clientid").HasFillFactor(90);

            entity.Property(e => e.FlagsPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("flags_pKey");
            entity.Property(e => e.FlagsAcademic).HasColumnName("flags_academic");
            entity.Property(e => e.FlagsClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("flags_clientid");
            entity.Property(e => e.FlagsConfidential).HasColumnName("flags_confidential");
            entity.Property(e => e.FlagsRossStudent).HasColumnName("flags_ross_student");
            entity.Property(e => e.FlagsStaff).HasColumnName("flags_staff");
            entity.Property(e => e.FlagsStudent).HasColumnName("flags_student");
            entity.Property(e => e.FlagsSvmPeople).HasColumnName("flags_svm_people");
            entity.Property(e => e.FlagsSvmStudent).HasColumnName("flags_svm_student");
            entity.Property(e => e.FlagsTeachingFaculty).HasColumnName("flags_teaching_faculty");
            entity.Property(e => e.FlagsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("flags_term_code");
            entity.Property(e => e.FlagsWosemp).HasColumnName("flags_wosemp");
        });

        modelBuilder.Entity<DlGradEmployee>(entity =>
        {
            entity.HasKey(e => e.GradEmpPKey);

            entity.ToTable("DL_gradEmployees");

            entity.HasIndex(e => e.GradEmpPKey, "IX_DL_gradEmployees")
                .IsUnique()
                .HasFillFactor(90);

            entity.HasIndex(e => e.GradEmpClientid, "IX_DL_gradEmployees_clientid").HasFillFactor(90);

            entity.HasIndex(e => e.GradEmpTermCode, "IX_DL_gradEmployees_term").HasFillFactor(90);

            entity.HasIndex(e => new { e.GradEmpTermCode, e.GradEmpClientid }, "IX_DL_gradEmployees_term_clientid").HasFillFactor(90);

            entity.Property(e => e.GradEmpPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradEmp_pKey");
            entity.Property(e => e.GradEmpAltDeptCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasDefaultValueSql("('')")
                .IsFixedLength()
                .HasColumnName("gradEmp_alt_dept_code");
            entity.Property(e => e.GradEmpCbuc)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("gradEmp_cbuc");
            entity.Property(e => e.GradEmpClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradEmp_clientid");
            entity.Property(e => e.GradEmpEffortHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradEmp_effort_home_dept");
            entity.Property(e => e.GradEmpEffortTitleCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("gradEmp_effort_title_code");
            entity.Property(e => e.GradEmpHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradEmp_home_dept");
            entity.Property(e => e.GradEmpPrimaryTitle)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("gradEmp_primary_title");
            entity.Property(e => e.GradEmpSchoolDivision)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradEmp_school_division");
            entity.Property(e => e.GradEmpStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradEmp_status");
            entity.Property(e => e.GradEmpTeachingHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("gradEmp_teaching_home_dept");
            entity.Property(e => e.GradEmpTeachingPercentFulltime)
                .HasColumnType("numeric(5, 4)")
                .HasColumnName("gradEmp_teaching_percent_fulltime");
            entity.Property(e => e.GradEmpTeachingTitleCode)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasDefaultValueSql("('')")
                .HasColumnName("gradEmp_teaching_title_code");
            entity.Property(e => e.GradEmpTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradEmp_term_code");
        });

        modelBuilder.Entity<DlGradFlag>(entity =>
        {
            entity.HasKey(e => e.GradFlagsPKey);

            entity.ToTable("DL_gradFlags");

            entity.HasIndex(e => e.GradFlagsClientid, "IX_DL_gradFlags_clientid").HasFillFactor(90);

            entity.HasIndex(e => e.GradFlagsPKey, "IX_DL_gradFlags_pKey")
                .IsUnique()
                .HasFillFactor(90);

            entity.HasIndex(e => e.GradFlagsTermCode, "IX_DL_gradFlags_term").HasFillFactor(90);

            entity.HasIndex(e => new { e.GradFlagsTermCode, e.GradFlagsClientid }, "IX_DL_gradFlags_term_clientid").HasFillFactor(90);

            entity.Property(e => e.GradFlagsPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradFlags_pKey");
            entity.Property(e => e.GradFlagsAcademic).HasColumnName("gradFlags_academic");
            entity.Property(e => e.GradFlagsClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradFlags_clientid");
            entity.Property(e => e.GradFlagsConfidential).HasColumnName("gradFlags_confidential");
            entity.Property(e => e.GradFlagsRossStudent)
                .HasDefaultValueSql("((0))")
                .HasColumnName("gradFlags_ross_student");
            entity.Property(e => e.GradFlagsStaff).HasColumnName("gradFlags_staff");
            entity.Property(e => e.GradFlagsStudent).HasColumnName("gradFlags_student");
            entity.Property(e => e.GradFlagsSvmPeople).HasColumnName("gradFlags_svm_people");
            entity.Property(e => e.GradFlagsSvmStudent).HasColumnName("gradFlags_svm_student");
            entity.Property(e => e.GradFlagsTeachingFaculty).HasColumnName("gradFlags_teaching_faculty");
            entity.Property(e => e.GradFlagsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradFlags_term_code");
            entity.Property(e => e.GradFlagsWosemp).HasColumnName("gradFlags_wosemp");
        });

        modelBuilder.Entity<DlGradId>(entity =>
        {
            entity.HasKey(e => e.GradIdsPKey);

            entity.ToTable("DL_gradIds");

            entity.HasIndex(e => e.GradIdsMothraid, "DL_gradIds_MothraID").HasFillFactor(90);

            entity.HasIndex(e => new { e.GradIdsMothraid, e.GradIdsTermCode }, "DL_gradIds_mothraID_termCode").HasFillFactor(90);

            entity.HasIndex(e => new { e.GradIdsTermCode, e.GradIdsMothraid }, "DL_gradIds_termCode_mothraID").HasFillFactor(90);

            entity.HasIndex(e => e.GradIdsClientid, "IX_DL_gradIds_clientid").HasFillFactor(90);

            entity.HasIndex(e => e.GradIdsLoginid, "IX_DL_gradIds_loginID").HasFillFactor(90);

            entity.HasIndex(e => e.GradIdsMailid, "IX_DL_gradIds_mailId").HasFillFactor(90);

            entity.HasIndex(e => e.GradIdsPKey, "IX_DL_gradIds_pKey")
                .IsUnique()
                .HasFillFactor(90);

            entity.HasIndex(e => e.GradIdsTermCode, "IX_DL_gradIds_term").HasFillFactor(90);

            entity.HasIndex(e => new { e.GradIdsTermCode, e.GradIdsPidm }, "IX_DL_gradIds_termPidm")
                .IsDescending(true, false)
                .HasFillFactor(90);

            entity.HasIndex(e => new { e.GradIdsTermCode, e.GradIdsMothraid }, "IX_DL_gradIds_term_client").HasFillFactor(90);

            entity.HasIndex(e => new { e.GradIdsTermCode, e.GradIdsClientid }, "IX_DL_gradIds_term_clientid").HasFillFactor(90);

            entity.HasIndex(e => e.GradIdsUnexId, "gradIds_unexID").HasFillFactor(90);

            entity.Property(e => e.GradIdsPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradIds_pKey");
            entity.Property(e => e.GradIdsClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradIds_clientid");
            entity.Property(e => e.GradIdsEmployeeId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("gradIds_employee_id");
            entity.Property(e => e.GradIdsIamId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("gradIDs_iam_id");
            entity.Property(e => e.GradIdsLoginid)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("gradIds_loginid");
            entity.Property(e => e.GradIdsMailid)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("gradIds_mailid");
            entity.Property(e => e.GradIdsMivId).HasColumnName("gradIds_miv_id");
            entity.Property(e => e.GradIdsMothraid)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasDefaultValueSql("('')")
                .HasColumnName("gradIds_mothraid");
            entity.Property(e => e.GradIdsPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("gradIds_pidm");
            entity.Property(e => e.GradIdsPpsId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("gradIDs_pps_id");
            entity.Property(e => e.GradIdsSpridenId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradIds_spriden_id");
            entity.Property(e => e.GradIdsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradIds_term_code");
            entity.Property(e => e.GradIdsUnexId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("gradIds_unex_id");
            entity.Property(e => e.GradIdsVmacsId).HasColumnName("gradIds_vmacs_id");
            entity.Property(e => e.GradIdsVmcasId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("gradIds_vmcas_id");
        });

        modelBuilder.Entity<DlGradPerson>(entity =>
        {
            entity.HasKey(e => e.GradPersonPKey);

            entity.ToTable("DL_gradPerson");

            entity.HasIndex(e => e.GradPersonClientid, "IX_DL_gradPerson_clientid").HasFillFactor(90);

            entity.HasIndex(e => e.GradPersonPKey, "IX_DL_gradPerson_pKey")
                .IsUnique()
                .HasFillFactor(90);

            entity.HasIndex(e => new { e.GradPersonTermCode, e.GradPersonClientid }, "IX_DL_gradPerson_termClient").HasFillFactor(90);

            entity.HasIndex(e => new { e.GradPersonTermCode, e.GradPersonClientid }, "IX_DL_gradPerson_term_clientid").HasFillFactor(90);

            entity.Property(e => e.GradPersonPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradPerson_pKey");
            entity.Property(e => e.GradPersonClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradPerson_clientid");
            entity.Property(e => e.GradPersonDisplayFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("gradPerson_display_first_name");
            entity.Property(e => e.GradPersonDisplayFullName)
                .HasMaxLength(91)
                .IsUnicode(false)
                .HasComputedColumnSql("(([gradPerson_display_first_name]+' ')+[gradPerson_display_last_name])", false)
                .HasColumnName("gradPerson_display_full_name");
            entity.Property(e => e.GradPersonDisplayLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("gradPerson_display_last_name");
            entity.Property(e => e.GradPersonDisplayMiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("gradPerson_display_middle_name");
            entity.Property(e => e.GradPersonFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("gradPerson_first_name");
            entity.Property(e => e.GradPersonLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("gradPerson_last_name");
            entity.Property(e => e.GradPersonMiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("gradPerson_middle_name");
            entity.Property(e => e.GradPersonTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradPerson_term_code");
        });

        modelBuilder.Entity<DlGradStudent>(entity =>
        {
            entity.HasKey(e => e.GradStudentsPKey);

            entity.ToTable("DL_gradStudents");

            entity.HasIndex(e => e.GradStudentsClientid, "IX_DL_gradStudents_clientid").HasFillFactor(90);

            entity.HasIndex(e => e.GradStudentsPKey, "IX_DL_gradStudents_pKey").HasFillFactor(90);

            entity.HasIndex(e => e.GradStudentsTermCode, "IX_DL_gradStudents_term").HasFillFactor(90);

            entity.HasIndex(e => new { e.GradStudentsTermCode, e.GradStudentsClientid }, "IX_DL_gradStudents_term_clientid").HasFillFactor(90);

            entity.Property(e => e.GradStudentsPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradStudents_pKey");
            entity.Property(e => e.GradStudentsClassLevel)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("gradStudents_class_level");
            entity.Property(e => e.GradStudentsClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradStudents_clientid");
            entity.Property(e => e.GradStudentsCollCode1)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradStudents_coll_code_1");
            entity.Property(e => e.GradStudentsCollCode2)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradStudents_coll_code_2");
            entity.Property(e => e.GradStudentsDegreeCode1)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("gradStudents_degree_code_1");
            entity.Property(e => e.GradStudentsDegreeCode2)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("gradStudents_degree_code_2");
            entity.Property(e => e.GradStudentsLevelCode1)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradStudents_level_code_1");
            entity.Property(e => e.GradStudentsLevelCode2)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradStudents_level_code_2");
            entity.Property(e => e.GradStudentsMajorCode1)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("gradStudents_major_code_1");
            entity.Property(e => e.GradStudentsMajorCode2)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("gradStudents_major_code_2");
            entity.Property(e => e.GradStudentsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("gradStudents_term_code");
        });

        modelBuilder.Entity<DlId>(entity =>
        {
            entity.HasKey(e => e.IdsPKey);

            entity.ToTable("DL_ids");

            entity.HasIndex(e => e.IdsMothraid, "DL_ids_MothraID").HasFillFactor(90);

            entity.HasIndex(e => new { e.IdsMothraid, e.IdsTermCode }, "DL_ids_mothraID_termCode").HasFillFactor(90);

            entity.HasIndex(e => new { e.IdsTermCode, e.IdsMothraid }, "DL_ids_termCode_mothraID").HasFillFactor(90);

            entity.HasIndex(e => e.IdsClientid, "IX_DL_ids_clientid").HasFillFactor(90);

            entity.HasIndex(e => e.IdsLoginid, "IX_DL_ids_loginID").HasFillFactor(90);

            entity.HasIndex(e => e.IdsMailid, "IX_DL_ids_mailId").HasFillFactor(90);

            entity.HasIndex(e => e.IdsPKey, "IX_DL_ids_pKey")
                .IsUnique()
                .HasFillFactor(90);

            entity.HasIndex(e => e.IdsTermCode, "IX_DL_ids_term").HasFillFactor(90);

            entity.HasIndex(e => new { e.IdsTermCode, e.IdsPidm }, "IX_DL_ids_termPidm")
                .IsDescending(true, false)
                .HasFillFactor(90);

            entity.HasIndex(e => new { e.IdsTermCode, e.IdsMothraid }, "IX_DL_ids_term_client").HasFillFactor(90);

            entity.HasIndex(e => new { e.IdsTermCode, e.IdsClientid }, "IX_DL_ids_term_clientid").HasFillFactor(90);

            entity.HasIndex(e => e.IdsUnexId, "unexID").HasFillFactor(90);

            entity.Property(e => e.IdsPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ids_pKey");
            entity.Property(e => e.IdsClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ids_clientid");
            entity.Property(e => e.IdsEmployeeId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("ids_employee_id");
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
            entity.Property(e => e.IdsMivId).HasColumnName("ids_miv_id");
            entity.Property(e => e.IdsMothraid)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasDefaultValueSql("('')")
                .HasColumnName("ids_mothraid");
            entity.Property(e => e.IdsPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_pidm");
            entity.Property(e => e.IdsPpsId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("ids_pps_id");
            entity.Property(e => e.IdsSpridenId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ids_spriden_id");
            entity.Property(e => e.IdsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ids_term_code");
            entity.Property(e => e.IdsUnexId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("ids_unex_id");
            entity.Property(e => e.IdsVmacsId).HasColumnName("ids_vmacs_id");
            entity.Property(e => e.IdsVmcasId)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("ids_vmcas_id");
        });

        modelBuilder.Entity<DlJob>(entity =>
        {
            entity.HasKey(e => new { e.JobPKey, e.JobSeqNum });

            entity.ToTable("DL_jobs");

            entity.Property(e => e.JobPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("job_pKey");
            entity.Property(e => e.JobSeqNum).HasColumnName("job_seq_num");
            entity.Property(e => e.JobBargainingUnit)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("job_bargaining_unit");
            entity.Property(e => e.JobClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("job_clientid");
            entity.Property(e => e.JobDepartmentCode)
                .HasMaxLength(7)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("job_department_code");
            entity.Property(e => e.JobPercentFulltime)
                .HasColumnType("numeric(7, 3)")
                .HasColumnName("job_percent_fulltime");
            entity.Property(e => e.JobSchoolDivision)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("job_school_division");
            entity.Property(e => e.JobTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("job_term_code");
            entity.Property(e => e.JobTitleCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("job_title_code");
        });

        modelBuilder.Entity<DlPerson>(entity =>
        {
            entity.HasKey(e => e.PersonPKey);

            entity.ToTable("DL_person");

            entity.HasIndex(e => e.PersonClientid, "IX_DL_person_clientid").HasFillFactor(90);

            entity.HasIndex(e => e.PersonPKey, "IX_DL_person_pKey")
                .IsUnique()
                .HasFillFactor(90);

            entity.HasIndex(e => new { e.PersonTermCode, e.PersonClientid }, "IX_DL_person_termClient").HasFillFactor(90);

            entity.HasIndex(e => new { e.PersonTermCode, e.PersonClientid }, "IX_DL_person_term_clientid").HasFillFactor(90);

            entity.Property(e => e.PersonPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_pKey");
            entity.Property(e => e.PersonClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_clientid");
            entity.Property(e => e.PersonDisplayFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_first_name");
            entity.Property(e => e.PersonDisplayFullName)
                .HasMaxLength(91)
                .IsUnicode(false)
                .HasComputedColumnSql("(([person_display_first_name]+' ')+[person_display_last_name])", false)
                .HasColumnName("person_display_full_name");
            entity.Property(e => e.PersonDisplayLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("person_display_last_name");
            entity.Property(e => e.PersonDisplayMiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_middle_name");
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
            entity.Property(e => e.PersonTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_term_code");
        });

        modelBuilder.Entity<DlStudent>(entity =>
        {
            entity.HasKey(e => e.StudentsPKey);

            entity.ToTable("DL_students");

            entity.HasIndex(e => e.StudentsClientid, "IX_DL_students_clientid").HasFillFactor(90);

            entity.HasIndex(e => e.StudentsPKey, "IX_DL_students_pKey").HasFillFactor(90);

            entity.HasIndex(e => e.StudentsTermCode, "IX_DL_students_term").HasFillFactor(90);

            entity.HasIndex(e => new { e.StudentsTermCode, e.StudentsClientid }, "IX_DL_students_term_clientid").HasFillFactor(90);

            entity.Property(e => e.StudentsPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_pKey");
            entity.Property(e => e.StudentsClassLevel)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("students_class_level");
            entity.Property(e => e.StudentsClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_clientid");
            entity.Property(e => e.StudentsCollCode1)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_coll_code_1");
            entity.Property(e => e.StudentsCollCode2)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_coll_code_2");
            entity.Property(e => e.StudentsDegreeCode1)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("students_degree_code_1");
            entity.Property(e => e.StudentsDegreeCode2)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("students_degree_code_2");
            entity.Property(e => e.StudentsLevelCode1)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_level_code_1");
            entity.Property(e => e.StudentsLevelCode2)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_level_code_2");
            entity.Property(e => e.StudentsMajorCode1)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("students_major_code_1");
            entity.Property(e => e.StudentsMajorCode2)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("students_major_code_2");
            entity.Property(e => e.StudentsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_term_code");
        });

        modelBuilder.Entity<DlUnexPerson>(entity =>
        {
            entity.HasKey(e => e.UnexPersonRecordId);

            entity.ToTable("DL_unexPerson");

            entity.HasIndex(e => e.UnexPersonRecordId, "record_ID").HasFillFactor(90);

            entity.HasIndex(e => e.UnexPersonUnexId, "unex_ID").HasFillFactor(90);

            entity.Property(e => e.UnexPersonRecordId).HasColumnName("unexPerson_record_ID");
            entity.Property(e => e.UnexPersonDisplayFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("unexPerson_display_first_Name");
            entity.Property(e => e.UnexPersonDisplayLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("unexPerson_display_last_Name");
            entity.Property(e => e.UnexPersonDisplayMiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("unexPerson_display_middle_Name");
            entity.Property(e => e.UnexPersonEmployeeId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("unexPerson_employee_ID");
            entity.Property(e => e.UnexPersonFirstName)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("unexPerson_first_name");
            entity.Property(e => e.UnexPersonIsSvm).HasColumnName("unexPerson_isSVM");
            entity.Property(e => e.UnexPersonLastName)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("unexPerson_last_name");
            entity.Property(e => e.UnexPersonLoginId)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("unexPerson_login_ID");
            entity.Property(e => e.UnexPersonMailId)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("unexPerson_mail_ID");
            entity.Property(e => e.UnexPersonMiddleName)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("unexPerson_middle_name");
            entity.Property(e => e.UnexPersonMothraId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("unexPerson_mothra_ID");
            entity.Property(e => e.UnexPersonPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("unexPerson_pidm");
            entity.Property(e => e.UnexPersonPpsId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("unexPerson_ppsID");
            entity.Property(e => e.UnexPersonSpridenId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("unexPerson_spriden_ID");
            entity.Property(e => e.UnexPersonTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("unexPerson_term_code");
            entity.Property(e => e.UnexPersonUnexId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("unexPerson_unex_ID");
        });

        modelBuilder.Entity<DlUnexRoster>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("DL_unexRoster");

            entity.HasIndex(e => e.UnexRosterId, "unex_ID").HasFillFactor(90);

            entity.Property(e => e.UnexRosterCrn)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("unexRoster_CRN");
            entity.Property(e => e.UnexRosterId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("unexRoster_ID");
            entity.Property(e => e.UnexRosterRecordId)
                .ValueGeneratedOnAdd()
                .HasColumnName("unexRoster_record_ID");
            entity.Property(e => e.UnexRosterTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("unexRoster_term_code");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmpPKey);

            entity.ToTable("employees");

            entity.HasIndex(e => e.EmpPKey, "IX_employees")
                .IsUnique()
                .HasFillFactor(90);

            entity.HasIndex(e => e.EmpClientid, "IX_employees_clientid");

            entity.HasIndex(e => e.EmpTermCode, "IX_employees_term").HasFillFactor(90);

            entity.HasIndex(e => new { e.EmpTermCode, e.EmpClientid }, "IX_employees_term_clientid");

            entity.Property(e => e.EmpPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_pKey");
            entity.Property(e => e.EmpAltDeptCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasDefaultValueSql("('')")
                .IsFixedLength()
                .HasColumnName("emp_alt_dept_code");
            entity.Property(e => e.EmpCbuc)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("emp_cbuc");
            entity.Property(e => e.EmpClientid)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("emp_clientid");
            entity.Property(e => e.EmpEffortHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_effort_home_dept");
            entity.Property(e => e.EmpEffortTitleCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("emp_effort_title_code");
            entity.Property(e => e.EmpHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_home_dept");
            entity.Property(e => e.EmpPrimaryTitle)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("emp_primary_title");
            entity.Property(e => e.EmpSchoolDivision)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_school_division");
            entity.Property(e => e.EmpStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_status");
            entity.Property(e => e.EmpTeachingHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("emp_teaching_home_dept");
            entity.Property(e => e.EmpTeachingPercentFulltime)
                .HasColumnType("numeric(5, 4)")
                .HasColumnName("emp_teaching_percent_fulltime");
            entity.Property(e => e.EmpTeachingTitleCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasDefaultValueSql("('')")
                .HasColumnName("emp_teaching_title_code");
            entity.Property(e => e.EmpTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_term_code");
        });

        modelBuilder.Entity<ExceptionDeactivate>(entity =>
        {
            entity.HasKey(e => e.DeactivatePKey);

            entity.ToTable("ExceptionDeactivate");

            entity.HasIndex(e => e.DeactivateClientId, "deactivate_clientid").HasFillFactor(90);

            entity.HasIndex(e => e.DeactivatePKey, "deactivate_pKey").HasFillFactor(90);

            entity.HasIndex(e => e.DeactivateTermCode, "deactivate_termCode").HasFillFactor(90);

            entity.HasIndex(e => new { e.DeactivateTermCode, e.DeactivateClientId }, "deactivate_termCode_clientID").HasFillFactor(90);

            entity.Property(e => e.DeactivatePKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("deactivate_pKey");
            entity.Property(e => e.DeactivateBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("deactivate_by");
            entity.Property(e => e.DeactivateClientId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("deactivate_clientID");
            entity.Property(e => e.DeactivateDate)
                .HasColumnType("datetime")
                .HasColumnName("deactivate_date");
            entity.Property(e => e.DeactivateName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("deactivate_name");
            entity.Property(e => e.DeactivateTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("deactivate_term_code");
        });

        modelBuilder.Entity<Exceptionemployee>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.EmpAltDeptCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_alt_dept_code");
            entity.Property(e => e.EmpCbuc)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("emp_cbuc");
            entity.Property(e => e.EmpClientid)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("emp_clientid");
            entity.Property(e => e.EmpEffortHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_effort_home_dept");
            entity.Property(e => e.EmpEffortTitleCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_effort_title_code");
            entity.Property(e => e.EmpHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_home_dept");
            entity.Property(e => e.EmpPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_pKey");
            entity.Property(e => e.EmpPrimaryTitle)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_primary_title");
            entity.Property(e => e.EmpSchoolDivision)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_school_division");
            entity.Property(e => e.EmpStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_status");
            entity.Property(e => e.EmpTeachingHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("emp_teaching_home_dept");
            entity.Property(e => e.EmpTeachingPercentFulltime)
                .HasColumnType("decimal(5, 4)")
                .HasColumnName("emp_teaching_percent_fulltime");
            entity.Property(e => e.EmpTeachingTitleCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("emp_teaching_title_code");
            entity.Property(e => e.EmpTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_term_code");
        });

        modelBuilder.Entity<Exceptionflag>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.FlagsAcademic).HasColumnName("flags_academic");
            entity.Property(e => e.FlagsClientid)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("flags_clientid");
            entity.Property(e => e.FlagsConfidential).HasColumnName("flags_confidential");
            entity.Property(e => e.FlagsPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("flags_pKey");
            entity.Property(e => e.FlagsStaff).HasColumnName("flags_staff");
            entity.Property(e => e.FlagsStudent).HasColumnName("flags_student");
            entity.Property(e => e.FlagsSvmPeople).HasColumnName("flags_svm_people");
            entity.Property(e => e.FlagsSvmRossStudent)
                .HasDefaultValueSql("((0))")
                .HasColumnName("flags_svm_ross_student");
            entity.Property(e => e.FlagsSvmStudent).HasColumnName("flags_svm_student");
            entity.Property(e => e.FlagsTeachingFaculty).HasColumnName("flags_teaching_faculty");
            entity.Property(e => e.FlagsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("flags_term_code");
            entity.Property(e => e.FlagsWosemp).HasColumnName("flags_wosemp");
        });

        modelBuilder.Entity<Exceptionid>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.IdsClientid)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("ids_clientid");
            entity.Property(e => e.IdsEmployeeId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("ids_employee_id");
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
            entity.Property(e => e.IdsPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ids_pKey");
            entity.Property(e => e.IdsPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_pidm");
            entity.Property(e => e.IdsPpsId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("ids_pps_id");
            entity.Property(e => e.IdsSpridenId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("ids_spriden_id");
            entity.Property(e => e.IdsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ids_term_code");
            entity.Property(e => e.IdsVmacsId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ids_vmacs_id");
        });

        modelBuilder.Entity<Exceptionperson>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("Exceptionperson");

            entity.Property(e => e.PersonClientid)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("person_clientid");
            entity.Property(e => e.PersonCreatedBy)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("person_createdBy");
            entity.Property(e => e.PersonDepartmentOverride)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("person_department_override");
            entity.Property(e => e.PersonDisplayFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_first_name");
            entity.Property(e => e.PersonDisplayFullName)
                .HasMaxLength(70)
                .IsUnicode(false)
                .HasColumnName("person_display_full_name");
            entity.Property(e => e.PersonDisplayLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("person_display_last_name");
            entity.Property(e => e.PersonDisplayMiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_middle_name");
            entity.Property(e => e.PersonEndDate)
                .HasColumnType("datetime")
                .HasColumnName("person_endDate");
            entity.Property(e => e.PersonExceptionCreateDate)
                .HasColumnType("datetime")
                .HasColumnName("person_exceptionCreateDate");
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
            entity.Property(e => e.PersonPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_pKey");
            entity.Property(e => e.PersonReasonForException)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("person_ReasonForException");
            entity.Property(e => e.PersonRequestedBy)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("person_requestedBy");
            entity.Property(e => e.PersonTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_term_code");
            entity.Property(e => e.PersonTitleOverride)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("person_title_override");
        });

        modelBuilder.Entity<Exceptionstudent>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.StudentsClassLevel)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("students_class_level");
            entity.Property(e => e.StudentsClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_clientid");
            entity.Property(e => e.StudentsCollCode1)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_coll_code_1");
            entity.Property(e => e.StudentsCollCode2)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_coll_code_2");
            entity.Property(e => e.StudentsDegreeCode1)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("students_degree_code_1");
            entity.Property(e => e.StudentsDegreeCode2)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("students_degree_code_2");
            entity.Property(e => e.StudentsLevelCode1)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_level_code_1");
            entity.Property(e => e.StudentsLevelCode2)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_level_code_2");
            entity.Property(e => e.StudentsMajorCode1)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("students_major_code_1");
            entity.Property(e => e.StudentsMajorCode2)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("students_major_code_2");
            entity.Property(e => e.StudentsMajorProfMothraId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("students_major_prof_mothra_id");
            entity.Property(e => e.StudentsPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_pKey");
            entity.Property(e => e.StudentsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_term_code");
        });

        modelBuilder.Entity<ExternalId>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ExternalId");

            entity.Property(e => e.ClientId).HasColumnName("clientID");
        });

        modelBuilder.Entity<FakeEmployee>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.EmpAltDeptCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_alt_dept_code");
            entity.Property(e => e.EmpCbuc)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("emp_cbuc");
            entity.Property(e => e.EmpClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_clientid");
            entity.Property(e => e.EmpEffortHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_effort_home_dept");
            entity.Property(e => e.EmpEffortTitleCode)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_effort_title_code");
            entity.Property(e => e.EmpHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_home_dept");
            entity.Property(e => e.EmpPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_pKey");
            entity.Property(e => e.EmpPrimaryTitle)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_primary_title");
            entity.Property(e => e.EmpSchoolDivision)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_school_division");
            entity.Property(e => e.EmpStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_status");
            entity.Property(e => e.EmpTeachingHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("emp_teaching_home_dept");
            entity.Property(e => e.EmpTeachingPercentFulltime)
                .HasColumnType("decimal(5, 4)")
                .HasColumnName("emp_teaching_percent_fulltime");
            entity.Property(e => e.EmpTeachingTitleCode)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("emp_teaching_title_code");
            entity.Property(e => e.EmpTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_term_code");
        });

        modelBuilder.Entity<FakeFlag>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.FlagsAcademic).HasColumnName("flags_academic");
            entity.Property(e => e.FlagsClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("flags_clientid");
            entity.Property(e => e.FlagsConfidential).HasColumnName("flags_confidential");
            entity.Property(e => e.FlagsPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("flags_pKey");
            entity.Property(e => e.FlagsRossStudent)
                .HasDefaultValueSql("((0))")
                .HasColumnName("flags_ross_student");
            entity.Property(e => e.FlagsStaff).HasColumnName("flags_staff");
            entity.Property(e => e.FlagsStudent).HasColumnName("flags_student");
            entity.Property(e => e.FlagsSvmPeople).HasColumnName("flags_svm_people");
            entity.Property(e => e.FlagsSvmStudent).HasColumnName("flags_svm_student");
            entity.Property(e => e.FlagsTeachingFaculty).HasColumnName("flags_teaching_faculty");
            entity.Property(e => e.FlagsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("flags_term_code");
            entity.Property(e => e.FlagsWosemp).HasColumnName("flags_wosemp");
        });

        modelBuilder.Entity<FakeId>(entity =>
        {
            entity.HasKey(e => e.IdsPKey);

            entity.Property(e => e.IdsClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ids_clientid");
            entity.Property(e => e.IdsEmployeeId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("ids_employee_id");
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
            entity.Property(e => e.IdsMivId).HasColumnName("ids_miv_id");
            entity.Property(e => e.IdsMothraid)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_mothraid");
            entity.Property(e => e.IdsPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ids_pKey");
            entity.Property(e => e.IdsPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_pidm");
            entity.Property(e => e.IdsPpsId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("ids_pps_id");
            entity.Property(e => e.IdsSpridenId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("ids_spriden_id");
            entity.Property(e => e.IdsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ids_term_code");
            entity.Property(e => e.IdsUnexId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("ids_unex_id");
            entity.Property(e => e.IdsVmacsId).HasColumnName("ids_vmacs_id");
            entity.Property(e => e.IdsVmcasId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("ids_vmcas_id");
        });

        modelBuilder.Entity<FakePerson>(entity =>
        {
            entity
                .ToTable("FakePerson")
                .HasKey(e => e.PersonPKey);

            entity.HasOne(e => e.FakeId)
                .WithOne(e => e.FakePerson)
                .HasForeignKey<FakeId>(e => e.IdsPKey);

            entity.Property(e => e.PersonClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_clientid");
            entity.Property(e => e.PersonDisplayFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_first_name");
            entity.Property(e => e.PersonDisplayFullName)
                .HasMaxLength(91)
                .IsUnicode(false)
                .HasComputedColumnSql("(([person_display_last_name]+' ')+[person_display_first_name])", false)
                .HasColumnName("person_display_full_name");
            entity.Property(e => e.PersonDisplayLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("person_display_last_name");
            entity.Property(e => e.PersonDisplayMiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_middle_name");
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
            entity.Property(e => e.PersonPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("person_pKey");
            entity.Property(e => e.PersonTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_term_code");
        });

        modelBuilder.Entity<Flag>(entity =>
        {
            entity.HasKey(e => e.FlagsPKey);

            entity.ToTable("flags");

            entity.HasIndex(e => e.FlagsClientid, "IX_flags_clientid").HasFillFactor(90);

            entity.HasIndex(e => e.FlagsPKey, "IX_flags_pKey")
                .IsUnique()
                .HasFillFactor(90);

            entity.HasIndex(e => e.FlagsTermCode, "IX_flags_term").HasFillFactor(90);

            entity.HasIndex(e => new { e.FlagsTermCode, e.FlagsClientid }, "IX_flags_term_clientid").HasFillFactor(90);

            entity.Property(e => e.FlagsPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("flags_pKey");
            entity.Property(e => e.FlagsAcademic).HasColumnName("flags_academic");
            entity.Property(e => e.FlagsClientid)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("flags_clientid");
            entity.Property(e => e.FlagsConfidential).HasColumnName("flags_confidential");
            entity.Property(e => e.FlagsRossStudent)
                .HasDefaultValueSql("((0))")
                .HasColumnName("flags_ross_student");
            entity.Property(e => e.FlagsStaff).HasColumnName("flags_staff");
            entity.Property(e => e.FlagsStudent).HasColumnName("flags_student");
            entity.Property(e => e.FlagsSvmPeople).HasColumnName("flags_svm_people");
            entity.Property(e => e.FlagsSvmStudent).HasColumnName("flags_svm_student");
            entity.Property(e => e.FlagsTeachingFaculty).HasColumnName("flags_teaching_faculty");
            entity.Property(e => e.FlagsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("flags_term_code");
            entity.Property(e => e.FlagsWosemp).HasColumnName("flags_wosemp");
        });

        modelBuilder.Entity<HelpDeskUser>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("HelpDesk_Users");

            entity.Property(e => e.IdsMailid)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("ids_mailid");
            entity.Property(e => e.PersonDisplayFullName)
                .HasMaxLength(91)
                .IsUnicode(false)
                .HasColumnName("person_display_full_name");
            entity.Property(e => e.PersonFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_first_name");
            entity.Property(e => e.PersonLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("person_last_name");
        });

        modelBuilder.Entity<Id>(entity =>
        {
            entity.HasKey(e => e.IdsPKey);

            entity.ToTable("ids");

            entity.HasIndex(e => e.IdsEmployeeId, "IX_employeeID");

            entity.HasIndex(e => e.IdsClientid, "IX_ids_clientid").HasFillFactor(90);

            entity.HasIndex(e => e.IdsLoginid, "IX_ids_loginID").HasFillFactor(90);

            entity.HasIndex(e => e.IdsMailid, "IX_ids_mailId").HasFillFactor(90);

            entity.HasIndex(e => e.IdsPKey, "IX_ids_pKey")
                .IsUnique()
                .HasFillFactor(90);

            entity.HasIndex(e => e.IdsTermCode, "IX_ids_term").HasFillFactor(90);

            entity.HasIndex(e => new { e.IdsTermCode, e.IdsPidm }, "IX_ids_termPidm")
                .IsDescending(true, false)
                .HasFillFactor(90);

            entity.HasIndex(e => new { e.IdsTermCode, e.IdsMothraid }, "IX_ids_term_client").HasFillFactor(90);

            entity.HasIndex(e => new { e.IdsTermCode, e.IdsClientid }, "IX_ids_term_clientid").HasFillFactor(90);

            entity.HasIndex(e => e.IdsMothraid, "ids_MothraID").HasFillFactor(90);

            entity.HasIndex(e => new { e.IdsMothraid, e.IdsTermCode }, "ids_mothraID_termCode").HasFillFactor(90);

            entity.HasIndex(e => new { e.IdsTermCode, e.IdsMothraid }, "ids_termCode_mothraID").HasFillFactor(90);

            entity.HasIndex(e => e.IdsLoginid, "ix_ids_loginid_term_mail_pidm");

            entity.HasIndex(e => e.IdsUnexId, "unexID").HasFillFactor(90);

            entity.Property(e => e.IdsPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ids_pKey");
            entity.Property(e => e.IdsClientid)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("ids_clientid");
            entity.Property(e => e.IdsEmployeeId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("ids_employee_id");
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
            entity.Property(e => e.IdsMivId).HasColumnName("ids_miv_id");
            entity.Property(e => e.IdsMothraid)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasDefaultValueSql("('')")
                .HasColumnName("ids_mothraid");
            entity.Property(e => e.IdsPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_pidm");
            entity.Property(e => e.IdsPpsId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("ids_pps_id");
            entity.Property(e => e.IdsSpridenId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ids_spriden_id");
            entity.Property(e => e.IdsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ids_term_code");
            entity.Property(e => e.IdsUnexId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("ids_unex_id");
            entity.Property(e => e.IdsVmacsId).HasColumnName("ids_vmacs_id");
            entity.Property(e => e.IdsVmcasId)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("ids_vmcas_id");
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => new { e.JobPKey, e.JobSeqNum });

            entity.ToTable("jobs");

            entity.Property(e => e.JobPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("job_pKey");
            entity.Property(e => e.JobSeqNum).HasColumnName("job_seq_num");
            entity.Property(e => e.JobBargainingUnit)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("job_bargaining_unit");
            entity.Property(e => e.JobClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("job_clientid");
            entity.Property(e => e.JobDepartmentCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("job_department_code");
            entity.Property(e => e.JobPercentFulltime)
                .HasColumnType("numeric(7, 6)")
                .HasColumnName("job_percent_fulltime");
            entity.Property(e => e.JobSchoolDivision)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("job_school_division");
            entity.Property(e => e.JobTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("job_term_code");
            entity.Property(e => e.JobTitleCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("job_title_code");
        });

        modelBuilder.Entity<Ldap>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ldap");

            entity.HasIndex(e => e.LdapUcdPersonUuid, "IX_ldap").HasFillFactor(90);

            entity.HasIndex(e => new { e.LdapName, e.LdapSeqNumber }, "NameSequence").HasFillFactor(90);

            entity.Property(e => e.LdapAddress)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ldap_address");
            entity.Property(e => e.LdapDepartment)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ldap_department");
            entity.Property(e => e.LdapDepartmentCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("ldap_department_code");
            entity.Property(e => e.LdapEmail)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ldap_email");
            entity.Property(e => e.LdapMobile)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ldap_mobile");
            entity.Property(e => e.LdapName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ldap_name");
            entity.Property(e => e.LdapPager)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ldap_pager");
            entity.Property(e => e.LdapSeqNumber).HasColumnName("ldap_seq_Number");
            entity.Property(e => e.LdapTelephoneNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ldap_telephoneNumber");
            entity.Property(e => e.LdapTitle)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ldap_title");
            entity.Property(e => e.LdapUcdPersonUuid)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ldap_ucdPersonUUID");
        });

        modelBuilder.Entity<LdapDepartment>(entity =>
        {
            entity.HasKey(e => e.LdapDeptRecordId);

            entity.ToTable("ldapDepartment");

            entity.HasIndex(e => e.LdapDeptCode, "departmentCode").HasFillFactor(90);

            entity.HasIndex(e => e.LdapDeptName, "departmentName").HasFillFactor(90);

            entity.HasIndex(e => e.LdapDeptRecordId, "recordID").HasFillFactor(90);

            entity.Property(e => e.LdapDeptRecordId).HasColumnName("ldapDept_recordID");
            entity.Property(e => e.LdapDeptCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("ldapDept_code");
            entity.Property(e => e.LdapDeptInUse).HasColumnName("ldapDept_inUse");
            entity.Property(e => e.LdapDeptName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ldapDept_name");
        });

        modelBuilder.Entity<LdapFacilityLink>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ldap_FacilityLink");

            entity.HasIndex(e => new { e.LdapUcdPersonUuid, e.LdapSeqNumber }, "personUUID_Sequence").HasFillFactor(90);

            entity.HasIndex(e => e.LdapUcdPersonUuid, "ucdPersonUUID").HasFillFactor(90);

            entity.Property(e => e.LdapBldgKey)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("ldap_bldg_key");
            entity.Property(e => e.LdapCity)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ldap_city");
            entity.Property(e => e.LdapDepartment)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ldap_department");
            entity.Property(e => e.LdapDepartmentCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("ldap_departmentCode");
            entity.Property(e => e.LdapEmail)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ldap_email");
            entity.Property(e => e.LdapFirstName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ldap_firstName");
            entity.Property(e => e.LdapLastName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ldap_lastName");
            entity.Property(e => e.LdapMobile)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ldap_mobile");
            entity.Property(e => e.LdapName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ldap_name");
            entity.Property(e => e.LdapPager)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ldap_pager");
            entity.Property(e => e.LdapPostalCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ldap_postalCode");
            entity.Property(e => e.LdapSeqNumber).HasColumnName("ldap_seq_number");
            entity.Property(e => e.LdapState)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ldap_state");
            entity.Property(e => e.LdapStreet)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ldap_street");
            entity.Property(e => e.LdapTelephoneNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ldap_telephoneNumber");
            entity.Property(e => e.LdapTitle)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ldap_title");
            entity.Property(e => e.LdapUcdPersonUuid)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ldap_ucdPersonUUID");
        });

        modelBuilder.Entity<NewEmployeeNotification>(entity =>
        {
            entity.HasKey(e => e.NotificationId);

            entity.ToTable("newEmployeeNotification");

            entity.Property(e => e.NotificationId).HasColumnName("notificationId");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("employeeId");
            entity.Property(e => e.EmployeeName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("employeeName");
            entity.Property(e => e.Notes)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("notes");
            entity.Property(e => e.NotificationSent)
                .HasColumnType("datetime")
                .HasColumnName("notificationSent");
            entity.Property(e => e.SupervisorId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("supervisorId");
            entity.Property(e => e.SupervisorName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("supervisorName");
        });

        modelBuilder.Entity<NightlyJob>(entity =>
        {
            entity.HasKey(e => e.NightlyJobRecordId);

            entity.ToTable("nightlyJob");

            entity.HasIndex(e => e.NightlyJobRecordId, "nightlyJob_recordID").HasFillFactor(90);

            entity.HasIndex(e => e.NightlyJobTermCode, "nightlyJob_termCode").HasFillFactor(90);

            entity.Property(e => e.NightlyJobRecordId).HasColumnName("nightlyJob_recordID");
            entity.Property(e => e.NightlyJobActive).HasColumnName("nightlyJob_active");
            entity.Property(e => e.NightlyJobTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("nightlyJob_termCode");
        });

        modelBuilder.Entity<Person>(entity =>
        {
            entity.HasKey(e => e.PersonPKey);

            entity.ToTable("person");

            entity.HasIndex(e => e.PersonClientid, "IX_person_clientid").HasFillFactor(90);

            entity.HasIndex(e => e.PersonPKey, "IX_person_pKey")
                .IsUnique()
                .HasFillFactor(90);

            entity.HasIndex(e => new { e.PersonTermCode, e.PersonClientid }, "IX_person_termClient").HasFillFactor(90);

            entity.HasIndex(e => new { e.PersonTermCode, e.PersonClientid }, "IX_person_term_clientid").HasFillFactor(90);

            entity.Property(e => e.PersonPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_pKey");
            entity.Property(e => e.PersonClientid)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("person_clientid");
            entity.Property(e => e.PersonDisplayFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_first_name");
            entity.Property(e => e.PersonDisplayFullName)
                .HasMaxLength(91)
                .IsUnicode(false)
                .HasComputedColumnSql("(([person_display_first_name]+' ')+[person_display_last_name])", false)
                .HasColumnName("person_display_full_name");
            entity.Property(e => e.PersonDisplayLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("person_display_last_name");
            entity.Property(e => e.PersonDisplayMiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_middle_name");
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
            entity.Property(e => e.PersonTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_term_code");
        });

        modelBuilder.Entity<Relationship>(entity =>
        {
            entity.HasKey(e => e.RelId).HasName("pk_relationships_1");

            entity.Property(e => e.RelId).HasColumnName("Rel_ID");
            entity.Property(e => e.RelChildMothraId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("Rel_Child_Mothra_ID");
            entity.Property(e => e.RelParentMothraId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("Rel_Parent_Mothra_ID");
            entity.Property(e => e.RelTypeId).HasColumnName("Rel_Type_ID");
        });

        modelBuilder.Entity<RelationshipType>(entity =>
        {
            entity.HasKey(e => e.TypeId).HasName("pk_constraint");

            entity.Property(e => e.TypeId).HasColumnName("Type_ID");
            entity.Property(e => e.TypeAbbreviation)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("Type_Abbreviation");
            entity.Property(e => e.TypeActive).HasColumnName("Type_Active");
            entity.Property(e => e.TypeChildPhrase)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("Type_Child_Phrase");
            entity.Property(e => e.TypeParentPhrase)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("Type_Parent_Phrase");
        });

        modelBuilder.Entity<RelationshipsAudit>(entity =>
        {
            entity.HasKey(e => e.AuditId).HasName("pk_relaudit_1");

            entity.ToTable("RelationshipsAudit");

            entity.Property(e => e.AuditId).HasColumnName("Audit_ID");
            entity.Property(e => e.AuditChange)
                .HasMaxLength(5000)
                .IsUnicode(false)
                .HasColumnName("Audit_Change");
            entity.Property(e => e.AuditModBy)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("Audit_Mod_By");
            entity.Property(e => e.AuditModDate)
                .HasColumnType("datetime")
                .HasColumnName("Audit_Mod_Date");
            entity.Property(e => e.AuditRelChildMothraId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("Audit_Rel_Child_Mothra_ID");
            entity.Property(e => e.AuditRelParentMothraId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("Audit_Rel_Parent_Mothra_ID");
            entity.Property(e => e.AuditRelTypeId).HasColumnName("Audit_Rel_Type_ID");

            entity.HasOne(d => d.AuditRelType).WithMany(p => p.RelationshipsAudits)
                .HasForeignKey(d => d.AuditRelTypeId)
                .HasConstraintName("fk_relaudit_1");
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
                .HasColumnType("date")
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

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentsPKey);

            entity.ToTable("students");

            entity.HasIndex(e => e.StudentsClassLevel, "IX_STD_CL_INC_TERM");

            entity.HasIndex(e => e.StudentsClassLevel, "IX_students_classlevel");

            entity.HasIndex(e => e.StudentsClientid, "IX_students_clientid").HasFillFactor(90);

            entity.HasIndex(e => e.StudentsPKey, "IX_students_pKey").HasFillFactor(90);

            entity.HasIndex(e => e.StudentsTermCode, "IX_students_term").HasFillFactor(90);

            entity.HasIndex(e => new { e.StudentsTermCode, e.StudentsClientid }, "IX_students_term_clientid").HasFillFactor(90);

            entity.HasIndex(e => e.StudentsTermCode, "ix_students_term_with_class_and_degree");

            entity.HasIndex(e => e.StudentsLevelCode1, "levelCode1").HasFillFactor(90);

            entity.HasIndex(e => e.StudentsMajorCode1, "primaryMajor").HasFillFactor(90);

            entity.Property(e => e.StudentsPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_pKey");
            entity.Property(e => e.StudentsClassLevel)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("students_class_level");
            entity.Property(e => e.StudentsClientid)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("students_clientid");
            entity.Property(e => e.StudentsCollCode1)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_coll_code_1");
            entity.Property(e => e.StudentsCollCode2)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_coll_code_2");
            entity.Property(e => e.StudentsDegreeCode1)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("students_degree_code_1");
            entity.Property(e => e.StudentsDegreeCode2)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("students_degree_code_2");
            entity.Property(e => e.StudentsLevelCode1)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_level_code_1");
            entity.Property(e => e.StudentsLevelCode2)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_level_code_2");
            entity.Property(e => e.StudentsMajorCode1)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("students_major_code_1");
            entity.Property(e => e.StudentsMajorCode2)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("students_major_code_2");
            entity.Property(e => e.StudentsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_term_code");
        });

        modelBuilder.Entity<Studentgrp>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("studentgrp");

            entity.Property(e => e.Studentgrp20)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("studentgrp_20");
            entity.Property(e => e.StudentgrpGrp)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("studentgrp_grp");
            entity.Property(e => e.StudentgrpPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("studentgrp_pidm");
            entity.Property(e => e.StudentgrpSurgeryTeam)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("studentgrp_surgeryTeam");
            entity.Property(e => e.StudentgrpTeamno)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("studentgrp_teamno");
            entity.Property(e => e.StudentgrpV220)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("studentgrp_V220");
            entity.Property(e => e.StudentgrpV2grp)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("studentgrp_V2Grp");
            entity.Property(e => e.StudentgrpV3grp)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("studentgrp_V3Grp");
        });

        modelBuilder.Entity<TestEmployee>(entity =>
        {
            entity.HasKey(e => e.EmpPKey);

            entity.ToTable("test_employees");

            entity.Property(e => e.EmpPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_pKey");
            entity.Property(e => e.EmpAltDeptCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_alt_dept_code");
            entity.Property(e => e.EmpCbuc)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("emp_cbuc");
            entity.Property(e => e.EmpClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_clientid");
            entity.Property(e => e.EmpEffortHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_effort_home_dept");
            entity.Property(e => e.EmpEffortTitleCode)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_effort_title_code");
            entity.Property(e => e.EmpHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_home_dept");
            entity.Property(e => e.EmpPrimaryTitle)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_primary_title");
            entity.Property(e => e.EmpSchoolDivision)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_school_division");
            entity.Property(e => e.EmpStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_status");
            entity.Property(e => e.EmpTeachingHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("emp_teaching_home_dept");
            entity.Property(e => e.EmpTeachingPercentFulltime)
                .HasColumnType("numeric(5, 4)")
                .HasColumnName("emp_teaching_percent_fulltime");
            entity.Property(e => e.EmpTeachingTitleCode)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("emp_teaching_title_code");
            entity.Property(e => e.EmpTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_term_code");
        });

        modelBuilder.Entity<TestFlag>(entity =>
        {
            entity.HasKey(e => e.FlagsPKey);

            entity.ToTable("test_flags");

            entity.Property(e => e.FlagsPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("flags_pKey");
            entity.Property(e => e.FlagsAcademic).HasColumnName("flags_academic");
            entity.Property(e => e.FlagsClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("flags_clientid");
            entity.Property(e => e.FlagsConfidential).HasColumnName("flags_confidential");
            entity.Property(e => e.FlagsStaff).HasColumnName("flags_staff");
            entity.Property(e => e.FlagsStudent).HasColumnName("flags_student");
            entity.Property(e => e.FlagsSvmPeople).HasColumnName("flags_svm_people");
            entity.Property(e => e.FlagsSvmStudent).HasColumnName("flags_svm_student");
            entity.Property(e => e.FlagsTeachingFaculty).HasColumnName("flags_teaching_faculty");
            entity.Property(e => e.FlagsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("flags_term_code");
            entity.Property(e => e.FlagsWosemp).HasColumnName("flags_wosemp");
        });

        modelBuilder.Entity<TestId>(entity =>
        {
            entity.HasKey(e => e.IdsPKey);

            entity.ToTable("test_ids");

            entity.Property(e => e.IdsPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ids_pKey");
            entity.Property(e => e.IdsClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ids_clientid");
            entity.Property(e => e.IdsEmployeeId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("ids_employee_id");
            entity.Property(e => e.IdsLoginid)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("ids_loginid");
            entity.Property(e => e.IdsMailid)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("ids_mailid");
            entity.Property(e => e.IdsMivId).HasColumnName("ids_miv_id");
            entity.Property(e => e.IdsMothraid)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_mothraid");
            entity.Property(e => e.IdsPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_pidm");
            entity.Property(e => e.IdsSpridenId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ids_spriden_id");
            entity.Property(e => e.IdsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ids_term_code");
            entity.Property(e => e.IdsUnexId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("ids_unex_id");
            entity.Property(e => e.IdsVmacsId).HasColumnName("ids_vmacs_id");
            entity.Property(e => e.IdsVmcasId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("ids_vmcas_id");
        });

        modelBuilder.Entity<TestPerson>(entity =>
        {
            entity.HasKey(e => e.PersonPKey);

            entity.ToTable("test_person");

            entity.Property(e => e.PersonPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_pKey");
            entity.Property(e => e.PersonClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_clientid");
            entity.Property(e => e.PersonDisplayFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_first_name");
            entity.Property(e => e.PersonDisplayFullName)
                .HasMaxLength(91)
                .IsUnicode(false)
                .HasColumnName("person_display_full_name");
            entity.Property(e => e.PersonDisplayLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("person_display_last_name");
            entity.Property(e => e.PersonDisplayMiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_middle_name");
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
            entity.Property(e => e.PersonTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_term_code");
        });

        modelBuilder.Entity<TestStudent>(entity =>
        {
            entity.HasKey(e => e.StudentsPKey);

            entity.ToTable("test_students");

            entity.Property(e => e.StudentsPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_pKey");
            entity.Property(e => e.StudentsClassLevel)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("students_class_level");
            entity.Property(e => e.StudentsClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_clientid");
            entity.Property(e => e.StudentsCollCode1)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_coll_code_1");
            entity.Property(e => e.StudentsCollCode2)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_coll_code_2");
            entity.Property(e => e.StudentsDegreeCode1)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("students_degree_code_1");
            entity.Property(e => e.StudentsDegreeCode2)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("students_degree_code_2");
            entity.Property(e => e.StudentsLevelCode1)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_level_code_1");
            entity.Property(e => e.StudentsLevelCode2)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_level_code_2");
            entity.Property(e => e.StudentsMajorCode1)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("students_major_code_1");
            entity.Property(e => e.StudentsMajorCode2)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("students_major_code_2");
            entity.Property(e => e.StudentsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_term_code");
        });

        modelBuilder.Entity<UnexPerson>(entity =>
        {
            entity.HasKey(e => e.UnexPersonRecordId);

            entity.ToTable("unexPerson");

            entity.HasIndex(e => e.UnexPersonRecordId, "record_ID").HasFillFactor(90);

            entity.HasIndex(e => e.UnexPersonUnexId, "unex_ID").HasFillFactor(90);

            entity.Property(e => e.UnexPersonRecordId).HasColumnName("unexPerson_record_ID");
            entity.Property(e => e.UnexPersonDisplayFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("unexPerson_display_first_name");
            entity.Property(e => e.UnexPersonDisplayLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("unexPerson_display_last_name");
            entity.Property(e => e.UnexPersonDisplayMiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("unexPerson_display_middle_name");
            entity.Property(e => e.UnexPersonEmployeeId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("unexPerson_employee_ID");
            entity.Property(e => e.UnexPersonFirstName)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("unexPerson_first_name");
            entity.Property(e => e.UnexPersonIsSvm).HasColumnName("unexPerson_isSVM");
            entity.Property(e => e.UnexPersonLastName)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("unexPerson_last_name");
            entity.Property(e => e.UnexPersonLoginId)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("unexPerson_login_ID");
            entity.Property(e => e.UnexPersonMailId)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("unexPerson_mail_ID");
            entity.Property(e => e.UnexPersonMiddleName)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("unexPerson_middle_name");
            entity.Property(e => e.UnexPersonMothraId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("unexPerson_mothra_ID");
            entity.Property(e => e.UnexPersonPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("unexPerson_pidm");
            entity.Property(e => e.UnexPersonPpsId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("unexPerson_ppsID");
            entity.Property(e => e.UnexPersonSpridenId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("unexPerson_spriden_ID");
            entity.Property(e => e.UnexPersonTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("unexPerson_term_code");
            entity.Property(e => e.UnexPersonUnexId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("unexPerson_unex_ID");
        });

        modelBuilder.Entity<UnexRoster>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("unexRoster");

            entity.HasIndex(e => e.UnexRosterId, "unex_ID").HasFillFactor(90);

            entity.Property(e => e.UnexRosterCrn)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("unexRoster_CRN");
            entity.Property(e => e.UnexRosterId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("unexRoster_ID");
            entity.Property(e => e.UnexRosterRecordId)
                .ValueGeneratedOnAdd()
                .HasColumnName("unexRoster_record_ID");
            entity.Property(e => e.UnexRosterTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("unexRoster_term_code");
        });

        modelBuilder.Entity<VwAdStaff>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_AD_Staff");

            entity.Property(e => e.EmpAltDeptCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_alt_dept_code");
            entity.Property(e => e.EmpHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_home_dept");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("first_name");
            entity.Property(e => e.FlagsStaff).HasColumnName("flags_staff");
            entity.Property(e => e.FlagsTeachingFaculty).HasColumnName("flags_teaching_faculty");
            entity.Property(e => e.LastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("last_name");
            entity.Property(e => e.Loginid)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("loginid");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("middle_name");
        });

        modelBuilder.Entity<VwAdStudent>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_AD_Students");

            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("last_name");
            entity.Property(e => e.LevelCode)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("level_code");
            entity.Property(e => e.Loginid)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("loginid");
            entity.Property(e => e.PersonMiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_middle_name");
            entity.Property(e => e.PersonTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_term_code");
        });

        modelBuilder.Entity<VwAdTeachingFaculty>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_AD_Teaching_Faculty");

            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("last_name");
            entity.Property(e => e.Loginid)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("loginid");
            entity.Property(e => e.PersonDisplayMiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_middle_name");
            entity.Property(e => e.PersonTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_term_code");
        });

        modelBuilder.Entity<VwAdVetstaff>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_AD_VETStaff");

            entity.Property(e => e.EmpAltDeptCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_alt_dept_code");
            entity.Property(e => e.EmpHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_home_dept");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("last_name");
            entity.Property(e => e.Loginid)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("loginid");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("middle_name");
            entity.Property(e => e.PersonTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_term_code");
        });

        modelBuilder.Entity<VwAdVmssacscheduler>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_AD_VMSSACSchedulers");

            entity.Property(e => e.Email)
                .HasMaxLength(44)
                .IsUnicode(false);
            entity.Property(e => e.EmpAltDeptCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_alt_dept_code");
            entity.Property(e => e.EmpHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_home_dept");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("first_name");
            entity.Property(e => e.IdCardLine2)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("idCard_Line2");
            entity.Property(e => e.LastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("last_name");
            entity.Property(e => e.Loginid)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("loginid");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("middle_name");
        });

        modelBuilder.Entity<VwAdconstituent>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ADConstituents");

            entity.Property(e => e.AltDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("altDept");
            entity.Property(e => e.DisplayFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("display_first_name");
            entity.Property(e => e.DisplayLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("display_last_name");
            entity.Property(e => e.EffortDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("effortDept");
            entity.Property(e => e.EffortTitle)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("effortTitle");
            entity.Property(e => e.EmailAddress)
                .HasMaxLength(44)
                .IsUnicode(false)
                .HasColumnName("emailAddress");
            entity.Property(e => e.Faculty).HasColumnName("faculty");
            entity.Property(e => e.HomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("homeDept");
            entity.Property(e => e.LoginId)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("loginID");
            entity.Property(e => e.Staff).HasColumnName("staff");
            entity.Property(e => e.Student).HasColumnName("student");
            entity.Property(e => e.TeachingDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("teachingDept");
            entity.Property(e => e.TeachingTitle)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("teachingTitle");
            entity.Property(e => e.Title)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("title");
        });

        modelBuilder.Entity<VwCurrentAffiliate>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_CurrentAffiliates");

            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("first_name");
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
            entity.Property(e => e.IdsPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_pidm");
            entity.Property(e => e.LastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("last_name");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("middle_name");
        });

        modelBuilder.Entity<VwCurrentAffiliatesForPf>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_CurrentAffiliatesForPF");

            entity.Property(e => e.CellPhone)
                .HasMaxLength(12)
                .IsUnicode(false);
            entity.Property(e => e.ChairEmail)
                .HasMaxLength(112)
                .HasColumnName("Chair_Email");
            entity.Property(e => e.ChairName)
                .HasMaxLength(50)
                .HasColumnName("Chair_Name");
            entity.Property(e => e.Department)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(44)
                .IsUnicode(false);
            entity.Property(e => e.EmpHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_home_dept");
            entity.Property(e => e.FlagsStaff).HasColumnName("flags_staff");
            entity.Property(e => e.FlagsStudent).HasColumnName("flags_student");
            entity.Property(e => e.FlagsTeachingFaculty).HasColumnName("flags_teaching_faculty");
            entity.Property(e => e.FullName)
                .HasMaxLength(91)
                .IsUnicode(false);
            entity.Property(e => e.IdsLoginid)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("ids_loginid");
            entity.Property(e => e.IdsMothraid)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_mothraid");
            entity.Property(e => e.IdsPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_pidm");
            entity.Property(e => e.MsoEmail)
                .HasMaxLength(112)
                .HasColumnName("MSO_Email");
            entity.Property(e => e.MsoName)
                .HasMaxLength(100)
                .HasColumnName("MSO_Name");
            entity.Property(e => e.PersonDisplayFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_first_name");
            entity.Property(e => e.PersonDisplayLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("person_display_last_name");
            entity.Property(e => e.WorkPhone)
                .HasMaxLength(12)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwCurrentAffiliatesForPfBk>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_CurrentAffiliatesForPF_bk");

            entity.Property(e => e.CellPhone)
                .HasMaxLength(12)
                .IsUnicode(false);
            entity.Property(e => e.ChairEmail)
                .HasMaxLength(112)
                .HasColumnName("Chair_Email");
            entity.Property(e => e.ChairName)
                .HasMaxLength(50)
                .HasColumnName("Chair_Name");
            entity.Property(e => e.Department)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(44)
                .IsUnicode(false);
            entity.Property(e => e.EmpHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_home_dept");
            entity.Property(e => e.FlagsStaff).HasColumnName("flags_staff");
            entity.Property(e => e.FlagsStudent).HasColumnName("flags_student");
            entity.Property(e => e.FlagsTeachingFaculty).HasColumnName("flags_teaching_faculty");
            entity.Property(e => e.FullName)
                .HasMaxLength(91)
                .IsUnicode(false);
            entity.Property(e => e.IdsLoginid)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("ids_loginid");
            entity.Property(e => e.IdsMothraid)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_mothraid");
            entity.Property(e => e.IdsPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_pidm");
            entity.Property(e => e.MsoEmail)
                .HasMaxLength(112)
                .HasColumnName("MSO_Email");
            entity.Property(e => e.MsoName)
                .HasMaxLength(100)
                .HasColumnName("MSO_Name");
            entity.Property(e => e.PersonDisplayFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_first_name");
            entity.Property(e => e.PersonDisplayLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("person_display_last_name");
            entity.Property(e => e.WorkPhone)
                .HasMaxLength(12)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwDvmStudent>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_DVM_Students");

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
            entity.Property(e => e.IdsPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_pidm");
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
        });

        modelBuilder.Entity<VwDvmStudentsHistory>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_DVM_Students_History");

            entity.Property(e => e.EnteringGradYear).HasColumnName("enteringGradYear");
            entity.Property(e => e.FirstClassLevel)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("firstClassLevel");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("first_name");
            entity.Property(e => e.FirstTermcode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("firstTermcode");
            entity.Property(e => e.LastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("last_name");
            entity.Property(e => e.LatestClassLevel)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("latestClassLevel");
            entity.Property(e => e.LatestGradYear).HasColumnName("latestGradYear");
            entity.Property(e => e.LatestTermcode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("latestTermcode");
            entity.Property(e => e.Pidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("pidm");
        });

        modelBuilder.Entity<VwDvmStudentsMaxTerm>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_DVM_Students_maxTerm");

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
            entity.Property(e => e.IdsPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_pidm");
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
            entity.Property(e => e.StudentsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_term_code");
        });

        modelBuilder.Entity<VwDvmStudentsMaxTermBk>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_DVM_Students_maxTerm_bk");

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
            entity.Property(e => e.IdsPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_pidm");
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
        });

        modelBuilder.Entity<VwDvmStudentsMaxTermNew>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_DVM_Students_maxTerm_new");

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
            entity.Property(e => e.IdsPidm)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_pidm");
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
            entity.Property(e => e.StudentsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_term_code");
        });

        modelBuilder.Entity<VwEmployeesForAaud>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_employeesForAAUD");

            entity.Property(e => e.Academic)
                .HasMaxLength(1)
                .HasColumnName("ACADEMIC");
            entity.Property(e => e.AcademicFederation)
                .HasMaxLength(1)
                .HasColumnName("ACADEMIC_FEDERATION");
            entity.Property(e => e.AcademicSenate)
                .HasMaxLength(1)
                .HasColumnName("ACADEMIC_SENATE");
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
            entity.Property(e => e.Ppsid)
                .HasMaxLength(254)
                .HasColumnName("PPSID");
            entity.Property(e => e.TeachingFaculty)
                .HasMaxLength(1)
                .HasColumnName("TEACHING_FACULTY");
            entity.Property(e => e.Wosemp)
                .HasMaxLength(1)
                .HasColumnName("WOSEMP");
        });

        modelBuilder.Entity<VwException>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_exceptions");

            entity.Property(e => e.FlagsStudent).HasColumnName("flags_student");
            entity.Property(e => e.IdsLoginid)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("ids_loginid");
            entity.Property(e => e.IdsMailid)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("ids_mailid");
            entity.Property(e => e.PersonCreatedBy)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("person_createdBy");
            entity.Property(e => e.PersonEndDate)
                .HasColumnType("datetime")
                .HasColumnName("person_endDate");
            entity.Property(e => e.PersonExceptionCreateDate)
                .HasColumnType("datetime")
                .HasColumnName("person_exceptionCreateDate");
            entity.Property(e => e.PersonFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_first_name");
            entity.Property(e => e.PersonLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("person_last_name");
            entity.Property(e => e.PersonPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_pKey");
            entity.Property(e => e.PersonReasonForException)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("person_ReasonForException");
            entity.Property(e => e.PersonRequestedBy)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("person_requestedBy");
            entity.Property(e => e.PersonTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_term_code");
        });

        modelBuilder.Entity<VwJobsForAaud>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_jobsForAAUD");

            entity.Property(e => e.AnnualRt)
                .HasColumnType("numeric(18, 3)")
                .HasColumnName("ANNUAL_RT");
            entity.Property(e => e.DeptCd)
                .HasMaxLength(6)
                .HasColumnName("DEPT_CD");
            entity.Property(e => e.DeptDesc)
                .HasMaxLength(40)
                .HasColumnName("DEPT_DESC");
            entity.Property(e => e.EffDateActive).HasColumnName("EFF_DATE_ACTIVE");
            entity.Property(e => e.Effdt).HasColumnName("EFFDT");
            entity.Property(e => e.Effseq)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EFFSEQ");
            entity.Property(e => e.EmplRcd)
                .HasColumnType("numeric(38, 0)")
                .HasColumnName("EMPL_RCD");
            entity.Property(e => e.Emplid)
                .HasMaxLength(11)
                .HasColumnName("EMPLID");
            entity.Property(e => e.ExpectedEndDate).HasColumnName("EXPECTED_END_DATE");
            entity.Property(e => e.Fte)
                .HasColumnType("numeric(7, 6)")
                .HasColumnName("FTE");
            entity.Property(e => e.JobStatus)
                .HasMaxLength(1)
                .HasColumnName("JOB_STATUS");
            entity.Property(e => e.Jobcode)
                .HasMaxLength(6)
                .HasColumnName("JOBCODE");
            entity.Property(e => e.Ppsid)
                .HasMaxLength(254)
                .HasColumnName("PPSID");
            entity.Property(e => e.Primaryindex)
                .HasMaxLength(2)
                .HasColumnName("PRIMARYINDEX");
            entity.Property(e => e.SubDivCd)
                .HasMaxLength(6)
                .HasColumnName("SUB_DIV_CD");
            entity.Property(e => e.UnionCd)
                .HasMaxLength(3)
                .HasColumnName("UNION_CD");
        });

        modelBuilder.Entity<VwMailIdsForStudent>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_mailIDsForStudents");

            entity.Property(e => e.IdsMailid)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("ids_mailid");
            entity.Property(e => e.PersonDisplayFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_first_name");
            entity.Property(e => e.PersonFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_first_name");
            entity.Property(e => e.PersonLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("person_last_name");
            entity.Property(e => e.StudentsClassLevel)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("students_class_level");
            entity.Property(e => e.StudentsDegreeCode1)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("students_degree_code_1");
            entity.Property(e => e.StudentsDegreeCode2)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("students_degree_code_2");
            entity.Property(e => e.StudentsMajorCode1)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("students_major_code_1");
            entity.Property(e => e.StudentsMajorCode2)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("students_major_code_2");
            entity.Property(e => e.StudentsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("students_term_code");
        });

        modelBuilder.Entity<VwPapercutUser>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_PapercutUsers");

            entity.Property(e => e.Email)
                .HasMaxLength(44)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.LoginId)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("loginID");
            entity.Property(e => e.Name)
                .HasMaxLength(92)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.Role)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("role");
        });

        modelBuilder.Entity<VwPerfectFormsConstituent>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_PerfectFormsConstituents");

            entity.Property(e => e.Email)
                .HasMaxLength(44)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("first_name");
            entity.Property(e => e.FullName)
                .HasMaxLength(91)
                .IsUnicode(false);
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

        modelBuilder.Entity<VwSftFaculty>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_SFT_Faculty");

            entity.Property(e => e.EmpClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_clientid");
            entity.Property(e => e.EmpTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_term_code");
            entity.Property(e => e.HomeDept)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("home_dept");
            entity.Property(e => e.PersonFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_first_name");
            entity.Property(e => e.PersonLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("person_last_name");
        });

        modelBuilder.Entity<VwSpSvmemployee>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_SP_SVMEmployees");

            entity.Property(e => e.PersonClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_clientid");
            entity.Property(e => e.PersonDisplayFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_first_name");
            entity.Property(e => e.PersonDisplayFullName)
                .HasMaxLength(91)
                .IsUnicode(false)
                .HasColumnName("person_display_full_name");
            entity.Property(e => e.PersonDisplayLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("person_display_last_name");
            entity.Property(e => e.PersonDisplayMiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_middle_name");
            entity.Property(e => e.PersonPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_pKey");
            entity.Property(e => e.PersonTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_term_code");
        });

        modelBuilder.Entity<VwSpSvmperson>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_SP_SVMPeople");

            entity.Property(e => e.PersonClientid)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_clientid");
            entity.Property(e => e.PersonDisplayFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_first_name");
            entity.Property(e => e.PersonDisplayFullName)
                .HasMaxLength(91)
                .IsUnicode(false)
                .HasColumnName("person_display_full_name");
            entity.Property(e => e.PersonDisplayLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("person_display_last_name");
            entity.Property(e => e.PersonDisplayMiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_middle_name");
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
            entity.Property(e => e.PersonPKey)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_pKey");
            entity.Property(e => e.PersonTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_term_code");
        });

        modelBuilder.Entity<VwUConnect>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_uConnect");

            entity.Property(e => e.EmpAltDeptCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_alt_dept_code");
            entity.Property(e => e.EmpHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_home_dept");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("first_name");
            entity.Property(e => e.FlagsAcademic).HasColumnName("flags_academic");
            entity.Property(e => e.FlagsStaff).HasColumnName("flags_staff");
            entity.Property(e => e.FlagsStudent).HasColumnName("flags_student");
            entity.Property(e => e.FlagsSvmStudent).HasColumnName("flags_svm_student");
            entity.Property(e => e.FlagsTeachingFaculty).HasColumnName("flags_teaching_faculty");
            entity.Property(e => e.IdCardNumber).HasColumnName("idCard_Number");
            entity.Property(e => e.LastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("last_name");
            entity.Property(e => e.Loginid)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("loginid");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("middle_name");
            entity.Property(e => e.PersonTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_term_code");
            entity.Property(e => e.StudentsClassLevel)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("students_class_level");
            entity.Property(e => e.VmdoPeopleUnitId).HasColumnName("vmdoPeople_unitID");
        });

        modelBuilder.Entity<VwUConnectNew>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_uConnect_New");

            entity.Property(e => e.EmpAltDeptCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_alt_dept_code");
            entity.Property(e => e.EmpHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_home_dept");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("first_name");
            entity.Property(e => e.FlagsAcademic).HasColumnName("flags_academic");
            entity.Property(e => e.FlagsStaff).HasColumnName("flags_staff");
            entity.Property(e => e.FlagsStudent).HasColumnName("flags_student");
            entity.Property(e => e.FlagsSvmStudent).HasColumnName("flags_svm_student");
            entity.Property(e => e.FlagsTeachingFaculty).HasColumnName("flags_teaching_faculty");
            entity.Property(e => e.IdCardNumber).HasColumnName("idCard_Number");
            entity.Property(e => e.LastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("last_name");
            entity.Property(e => e.Loginid)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("loginid");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("middle_name");
            entity.Property(e => e.PersonTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("person_term_code");
            entity.Property(e => e.StudentsClassLevel)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("students_class_level");
            entity.Property(e => e.VmdoPeopleUnitId).HasColumnName("vmdoPeople_unitID");
        });

        modelBuilder.Entity<VwUConnectUnit>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_uConnect_Units");

            entity.Property(e => e.AdminEmail)
                .HasMaxLength(100)
                .HasColumnName("Admin_Email");
            entity.Property(e => e.DeanDirectorMailId)
                .HasMaxLength(100)
                .HasColumnName("Dean_Director_MailID");
            entity.Property(e => e.DvtUnitDeptCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("dvtUnit_deptCode");
            entity.Property(e => e.DvtUnitUnitName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("dvtUnit_unitName");
            entity.Property(e => e.UnitId).HasColumnName("unitID");
        });

        modelBuilder.Entity<VwVmthAllEmployesExcludingClinician>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_vmth_AllEmployesExcludingClinicians");

            entity.Property(e => e.Email)
                .HasMaxLength(44)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(91)
                .IsUnicode(false)
                .HasColumnName("fullName");
            entity.Property(e => e.Mothraid)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("mothraid");
        });

        modelBuilder.Entity<VwVmthClinician>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMTH_Clinicians");

            entity.Property(e => e.Email)
                .HasMaxLength(44)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(91)
                .IsUnicode(false);
            entity.Property(e => e.IdsMothraid)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_mothraid");
            entity.Property(e => e.PersonDisplayFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_first_name");
            entity.Property(e => e.PersonDisplayLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("person_display_last_name");
            entity.Property(e => e.UserActive)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_active");
            entity.Property(e => e.UserCellPhone)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_CellPhone");
            entity.Property(e => e.UserDeactivateDate)
                .HasColumnType("date")
                .HasColumnName("User_deactivate_date");
            entity.Property(e => e.UserHomePhone)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_HomePhone");
            entity.Property(e => e.UserId).HasColumnName("User_ID");
            entity.Property(e => e.UserLoginId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("User_loginID");
            entity.Property(e => e.UserName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("User_name");
            entity.Property(e => e.UserRecordId).HasColumnName("User_recordID");
            entity.Property(e => e.UserStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_status");
            entity.Property(e => e.UserStudent).HasColumnName("User_student");
            entity.Property(e => e.UserUnit)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_unit");
            entity.Property(e => e.UserWorkPhone1)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_WorkPhone1");
            entity.Property(e => e.UserWorkPhone2)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_WorkPhone2");
        });

        modelBuilder.Entity<VwVmthConstituent>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMTH_Constituents");

            entity.Property(e => e.Email)
                .HasMaxLength(44)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(91)
                .IsUnicode(false);
            entity.Property(e => e.IdsMothraid)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_mothraid");
            entity.Property(e => e.PersonDisplayFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_first_name");
            entity.Property(e => e.PersonDisplayLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("person_display_last_name");
            entity.Property(e => e.UserActive)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_active");
            entity.Property(e => e.UserCellPhone)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_CellPhone");
            entity.Property(e => e.UserDeactivateDate)
                .HasColumnType("date")
                .HasColumnName("User_deactivate_date");
            entity.Property(e => e.UserHomePhone)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_HomePhone");
            entity.Property(e => e.UserId).HasColumnName("User_ID");
            entity.Property(e => e.UserLoginId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("User_loginID");
            entity.Property(e => e.UserName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("User_name");
            entity.Property(e => e.UserRecordId).HasColumnName("User_recordID");
            entity.Property(e => e.UserStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_status");
            entity.Property(e => e.UserStudent).HasColumnName("User_student");
            entity.Property(e => e.UserUnit)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_unit");
            entity.Property(e => e.UserWorkPhone1)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_WorkPhone1");
            entity.Property(e => e.UserWorkPhone2)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_WorkPhone2");
        });

        modelBuilder.Entity<VwVmthStaff>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMTH_Staff");

            entity.Property(e => e.Email)
                .HasMaxLength(44)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(91)
                .IsUnicode(false);
            entity.Property(e => e.IdsMothraid)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("ids_mothraid");
            entity.Property(e => e.PersonDisplayFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_first_name");
            entity.Property(e => e.PersonDisplayLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("person_display_last_name");
            entity.Property(e => e.UserActive)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_active");
            entity.Property(e => e.UserCellPhone)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_CellPhone");
            entity.Property(e => e.UserDeactivateDate)
                .HasColumnType("date")
                .HasColumnName("User_deactivate_date");
            entity.Property(e => e.UserHomePhone)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_HomePhone");
            entity.Property(e => e.UserId).HasColumnName("User_ID");
            entity.Property(e => e.UserLoginId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("User_loginID");
            entity.Property(e => e.UserName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("User_name");
            entity.Property(e => e.UserRecordId).HasColumnName("User_recordID");
            entity.Property(e => e.UserStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_status");
            entity.Property(e => e.UserStudent).HasColumnName("User_student");
            entity.Property(e => e.UserUnit)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_unit");
            entity.Property(e => e.UserWorkPhone1)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_WorkPhone1");
            entity.Property(e => e.UserWorkPhone2)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_WorkPhone2");
        });

        modelBuilder.Entity<VwVmthStudent>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMTH_students");

            entity.Property(e => e.ClassLevel)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("classLevel");
            entity.Property(e => e.Email)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("firstName");
            entity.Property(e => e.KerberosId)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("kerberosID");
            entity.Property(e => e.LastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("lastName");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("middleName");
            entity.Property(e => e.MothraId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("mothraID");
            entity.Property(e => e.StudentId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("studentID");
            entity.Property(e => e.VmacsId).HasColumnName("vmacsID");
        });

        modelBuilder.Entity<VwVmthStudentsForPerfectForm>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMTH_StudentsForPerfectForms");

            entity.Property(e => e.Email)
                .HasMaxLength(44)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(91)
                .IsUnicode(false);
            entity.Property(e => e.PersonDisplayFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("person_display_first_name");
            entity.Property(e => e.PersonDisplayLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("person_display_last_name");
            entity.Property(e => e.UserActive)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_active");
            entity.Property(e => e.UserCellPhone)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_CellPhone");
            entity.Property(e => e.UserDeactivateDate)
                .HasColumnType("date")
                .HasColumnName("User_deactivate_date");
            entity.Property(e => e.UserHomePhone)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_HomePhone");
            entity.Property(e => e.UserId).HasColumnName("User_ID");
            entity.Property(e => e.UserLoginId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("User_loginID");
            entity.Property(e => e.UserName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("User_name");
            entity.Property(e => e.UserRecordId).HasColumnName("User_recordID");
            entity.Property(e => e.UserStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_status");
            entity.Property(e => e.UserStudent).HasColumnName("User_student");
            entity.Property(e => e.UserUnit)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_unit");
            entity.Property(e => e.UserWorkPhone1)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_WorkPhone1");
            entity.Property(e => e.UserWorkPhone2)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("User_WorkPhone2");
        });

        modelBuilder.Entity<ExampleComment>(entity =>
        {
            entity.HasKey(e => e.AaudUserId);
            entity.ToTable("examplecomment");
            entity.HasOne(e => e.AaudUser).WithOne(u => u.ExampleComment)
                .HasForeignKey<AaudUser>(e => e.AaudUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_examplecomment_aaudUser")
                .IsRequired(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
