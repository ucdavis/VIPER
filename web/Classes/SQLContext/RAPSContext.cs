using Microsoft.EntityFrameworkCore;
using Viper.Models.RAPS;

namespace Viper.Classes.SQLContext;

public partial class RAPSContext : DbContext
{
    public RAPSContext()
    {
    }

    public RAPSContext(DbContextOptions<RAPSContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ExcelGeneratorRequest> ExcelGeneratorRequests { get; set; }

    public virtual DbSet<OuGroup> OuGroups { get; set; }

    public virtual DbSet<OuGroupRole> OuGroupRoles { get; set; }

    public virtual DbSet<RoleTemplate> RoleTemplates { get; set; }

    public virtual DbSet<RoleTemplateRole> RoleTemplateRoles { get; set; }

    public virtual DbSet<TblAppRole> TblAppRoles { get; set; }

    public virtual DbSet<TblLog> TblLogs { get; set; }

    public virtual DbSet<TblMemberPermission> TblMemberPermissions { get; set; }

    public virtual DbSet<TblPermission> TblPermissions { get; set; }

    public virtual DbSet<TblRole> TblRoles { get; set; }

    public virtual DbSet<TblRoleMember> TblRoleMembers { get; set; }

    public virtual DbSet<TblRolePermission> TblRolePermissions { get; set; }

    public virtual DbSet<VmacsLog> VmacsLogs { get; set; }

    public virtual DbSet<VwAaudUser> VwAaudUser { get; set; }

    public virtual DbSet<VwCahfspersonnel> VwCahfspersonnel { get; set; }

    public virtual DbSet<VwCpl> VwCpls { get; set; }

    public virtual DbSet<VwEmployee> VwEmployees { get; set; }

    public virtual DbSet<VwFacultyAtLarge> VwFacultyAtLarges { get; set; }

    public virtual DbSet<VwFacultySvm> VwFacultySvms { get; set; }

    public virtual DbSet<VwFederationFacultyAdjunct> VwFederationFacultyAdjuncts { get; set; }

    public virtual DbSet<VwFederationFacultyCurrent> VwFederationFacultyCurrents { get; set; }

    public virtual DbSet<VwFederationFacultyHsclin> VwFederationFacultyHsclins { get; set; }

    public virtual DbSet<VwFederationFacultyLecturer> VwFederationFacultyLecturers { get; set; }

    public virtual DbSet<VwIdcardApprover> VwIdcardApprovers { get; set; }

    public virtual DbSet<VwMivUcd> VwMivUcds { get; set; }

    public virtual DbSet<VwRoss> VwRosses { get; set; }

    public virtual DbSet<VwSenateFaculty> VwSenateFaculties { get; set; }

    public virtual DbSet<VwSenateFacultyCurrent> VwSenateFacultyCurrents { get; set; }

    public virtual DbSet<VwSenateFacultyEmeritu> VwSenateFacultyEmeritus { get; set; }

    public virtual DbSet<VwSenateFacultyProf> VwSenateFacultyProfs { get; set; }

    public virtual DbSet<VwSenateFacultyProfClinBlank> VwSenateFacultyProfClinBlanks { get; set; }

    public virtual DbSet<VwSenateFacultyProfInResidence> VwSenateFacultyProfInResidences { get; set; }

    public virtual DbSet<VwServiceCredit> VwServiceCredits { get; set; }

    public virtual DbSet<VwStaffSvm> VwStaffSvms { get; set; }

    public virtual DbSet<VwStaffVet> VwStaffVets { get; set; }

    public virtual DbSet<VwStudentClubsMod> VwStudentClubsMods { get; set; }

    public virtual DbSet<VwStudentsDualDegree> VwStudentsDualDegrees { get; set; }

    public virtual DbSet<VwStudentsDualDegreeAway> VwStudentsDualDegreeAways { get; set; }

    public virtual DbSet<VwStudentsDvm> VwStudentsDvms { get; set; }

    public virtual DbSet<VwStudentsDvmV1> VwStudentsDvmV1s { get; set; }

    public virtual DbSet<VwStudentsDvmV2> VwStudentsDvmV2s { get; set; }

    public virtual DbSet<VwStudentsDvmV3> VwStudentsDvmV3s { get; set; }

    public virtual DbSet<VwStudentsDvmV4> VwStudentsDvmV4s { get; set; }

    public virtual DbSet<VwStudentsMpvm> VwStudentsMpvms { get; set; }

    public virtual DbSet<VwSvmAcademicDeptStaffAndFaculty> VwSvmAcademicDeptStaffAndFaculties { get; set; }

    public virtual DbSet<VwSvmConstituent> VwSvmConstituents { get; set; }

    public virtual DbSet<VwVmacsCape> VwVmacsCapes { get; set; }

    public virtual DbSet<VwVmacsCardiology> VwVmacsCardiologies { get; set; }

    public virtual DbSet<VwVmacsComputerService> VwVmacsComputerServices { get; set; }

    public virtual DbSet<VwVmacsDentistry> VwVmacsDentistries { get; set; }

    public virtual DbSet<VwVmacsDermatology> VwVmacsDermatologies { get; set; }

    public virtual DbSet<VwVmacsEqMedicine> VwVmacsEqMedicines { get; set; }

    public virtual DbSet<VwVmacsEqReproduction> VwVmacsEqReproductions { get; set; }

    public virtual DbSet<VwVmacsEqSurgery> VwVmacsEqSurgeries { get; set; }

    public virtual DbSet<VwVmacsFaMedicine> VwVmacsFaMedicines { get; set; }

    public virtual DbSet<VwVmacsLaIcu> VwVmacsLaIcus { get; set; }

    public virtual DbSet<VwVmacsNeurology> VwVmacsNeurologies { get; set; }

    public virtual DbSet<VwVmacsOncology> VwVmacsOncologies { get; set; }

    public virtual DbSet<VwVmacsOphthalmology> VwVmacsOphthalmologies { get; set; }

    public virtual DbSet<VwVmacsRadiationOncology> VwVmacsRadiationOncologies { get; set; }

    public virtual DbSet<VwVmacsSaEmergency> VwVmacsSaEmergencies { get; set; }

    public virtual DbSet<VwVmacsSaIcu> VwVmacsSaIcus { get; set; }

    public virtual DbSet<VwVmacsSaMedicine> VwVmacsSaMedicines { get; set; }

    public virtual DbSet<VwVmacsSaNursing> VwVmacsSaNursings { get; set; }

    public virtual DbSet<VwVmacsSaOrthopedicSurgery> VwVmacsSaOrthopedicSurgeries { get; set; }

    public virtual DbSet<VwVmacsSaSurgery> VwVmacsSaSurgeries { get; set; }

    public virtual DbSet<VwVmdo> VwVmdos { get; set; }

    public virtual DbSet<VwVmdoAp> VwVmdoAps { get; set; }

    public virtual DbSet<VwVmdoCat> VwVmdoCats { get; set; }

    public virtual DbSet<VwVmdoComm> VwVmdoComms { get; set; }

    public virtual DbSet<VwVmdoDevelopment> VwVmdoDevelopments { get; set; }

    public virtual DbSet<VwVmdoSp> VwVmdoSps { get; set; }

    public virtual DbSet<VwVmdoSvmIt> VwVmdoSvmIts { get; set; }

    public virtual DbSet<VwVmthClinician> VwVmthClinicians { get; set; }

    public virtual DbSet<VwVmthConstituent> VwVmthConstituents { get; set; }

    public virtual DbSet<VwVmthRe> VwVmthRes { get; set; }

    public virtual DbSet<VwVmthStaff> VwVmthStaffs { get; set; }

    public virtual DbSet<VwVmthStaffByJob> VwVmthStaffByJobs { get; set; }

    public virtual DbSet<VwVmthStudent> VwVmthStudents { get; set; }

    public virtual DbSet<VwVmthadmission> VwVmthadmissions { get; set; }

    public virtual DbSet<VwVmthinternsManual> VwVmthinternsManuals { get; set; }

    public virtual DbSet<VwVmthtech> VwVmthtechs { get; set; }

    public virtual DbSet<VwVstp> VwVstps { get; set; }

    public virtual DbSet<GetVmacsUserPermissionsResult> GetVMACSUserPermissionsResult { get; set; }

    public virtual DbSet<GetAllRapsViews> GetAllRapsViews { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (HttpHelper.Settings != null)
        {
            optionsBuilder.UseSqlServer(HttpHelper.Settings["ConnectionStrings:RAPS"]);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExcelGeneratorRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId);

            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.ArgumentList)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.Cfc)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("CFC");
            entity.Property(e => e.Method)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MothraId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MothraID");
            entity.Property(e => e.TheColumnList)
                .HasMaxLength(2000)
                .IsUnicode(false)
                .HasColumnName("theColumnList");
        });

        modelBuilder.Entity<OuGroup>(entity =>
        {
            entity.HasKey(e => e.OugroupId).HasName("PK_ouGroup");

            entity.ToTable("ouGroups");

            entity.Property(e => e.OugroupId).HasColumnName("ougroupID");
            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .IsUnicode(false)
                .HasColumnName("name");
        });

        modelBuilder.Entity<OuGroupRole>(entity =>
        {
            entity.HasKey(e => new { e.Ougroupid, e.Roleid });

            entity.ToTable("ouGroupRoles");

            entity.Property(e => e.Ougroupid).HasColumnName("ougroupid");
            entity.Property(e => e.Roleid).HasColumnName("roleid");
            entity.Property(e => e.IsGroupRole).HasColumnName("isGroupRole");

            entity.HasOne(d => d.Ougroup).WithMany(p => p.OuGroupRoles)
                .HasForeignKey(d => d.Ougroupid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ouGroupRoles_ouGroups");

            entity.HasOne(d => d.Role).WithMany(p => p.OuGroupRoles)
                .HasForeignKey(d => d.Roleid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ouGroupRoles_tblRoles");
        });

        modelBuilder.Entity<RoleTemplate>(entity =>
        {
            entity.ToTable("roleTemplate");

            entity.Property(e => e.RoleTemplateId).HasColumnName("roleTemplate_ID");
            entity.Property(e => e.Description)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.TemplateName)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("templateName");
        });

        modelBuilder.Entity<RoleTemplateRole>(entity =>
        {
            entity.ToTable("roleTemplateRole");

            entity.Property(e => e.RoleTemplateRoleId).HasColumnName("roleTemplateRole_ID");
            entity.Property(e => e.ModBy)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("modBy");
            entity.Property(e => e.ModTime)
                .HasColumnType("datetime")
                .HasColumnName("modTime");
            entity.Property(e => e.RoleTemplateRoleId1).HasColumnName("roleTemplate_RoleID");
            entity.Property(e => e.RoleTemplateTemplateId).HasColumnName("roleTemplate_templateID");

            entity.HasOne(d => d.RoleTemplateRoleId1Navigation).WithMany(p => p.RoleTemplateRoles)
                .HasForeignKey(d => d.RoleTemplateRoleId1)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_roleTemplateRole_tblRoles");

            entity.HasOne(d => d.RoleTemplateTemplate).WithMany(p => p.RoleTemplateRoles)
                .HasForeignKey(d => d.RoleTemplateTemplateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_roleTemplateRole_roleTemplate");
        });

        modelBuilder.Entity<TblAppRole>(entity =>
        {
            entity.HasKey(e => new { e.AppRoleId, e.RoleId }).HasName("PK_tblDelRole");

            entity.ToTable("tblAppRoles");

            entity.Property(e => e.AppRoleId).HasColumnName("AppRoleID");
            entity.Property(e => e.RoleId).HasColumnName("RoleID");

            entity.HasOne(e => e.Role)
                .WithMany(e => e.AppRoles)
                .HasForeignKey(e => e.RoleId);
            entity.HasOne(e => e.AppRole)
                .WithMany(e => e.ChildRoles)
                .HasForeignKey(e => e.AppRoleId);
        });

        modelBuilder.Entity<TblLog>(entity =>
        {
            entity.HasKey(e => e.AuditRecordId);
            entity.ToTable("tblLog");
            
            entity.Property(e => e.AuditRecordId).HasColumnName("auditRecordId");
            entity.Property(e => e.Audit)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Comment).IsUnicode(false);
            entity.Property(e => e.Detail)
                .HasMaxLength(4000)
                .IsUnicode(false);
            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MemberID");
            entity.Property(e => e.ModBy)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.ModTime).HasColumnType("datetime");
            entity.Property(e => e.OuGroupId).HasColumnName("ouGroupID");
            entity.Property(e => e.PermissionId).HasColumnName("PermissionID");
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.RoleTemplateId).HasColumnName("roleTemplateID");
        });

        modelBuilder.Entity<TblMemberPermission>(entity =>
        {
            entity.HasKey(e => new { e.MemberId, e.PermissionId });

            entity.ToTable("tblMemberPermissions");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MemberID");
            entity.Property(e => e.PermissionId).HasColumnName("PermissionID");
            entity.Property(e => e.Access).HasDefaultValueSql("(1)");
            entity.Property(e => e.AddDate).HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.ModBy)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.ModTime).HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.Permission).WithMany(p => p.TblMemberPermissions)
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblMemberPermissions_tblPermissions");
            
            entity.HasOne(e => e.Member).WithMany(m => m.TblMemberPermissions)
                .HasForeignKey(e => e.MemberId);
        });

        modelBuilder.Entity<TblPermission>(entity =>
        {
            entity.HasKey(e => e.PermissionId);

            entity.ToTable("tblPermissions");

            entity.Property(e => e.PermissionId).HasColumnName("PermissionID");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Permission)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblRole>(entity =>
        {
            entity.HasKey(e => e.RoleId);

			entity.ToTable("tblRoles");

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.AccessCode)
                .HasMaxLength(1)
                .IsUnicode(false)
                //.UseCollation("SQL_Latin1_General_CP1_CS_AS")
                .HasColumnName("accessCode");
            entity.Property(e => e.AllowAllUsers).HasColumnName("allowAllUsers");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.DisplayName)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("displayName");
            entity.Property(e => e.Role)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.UpdateFreq).HasComment("How frequently to update the role membership (0=Never, 1=Daily, 2=Hourly)");
            entity.Property(e => e.ViewName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblRoleMember>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.MemberId }).HasName("PK_tblRoleMember");

            entity.ToTable("tblRoleMembers");

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MemberID");
            entity.Property(e => e.AddDate).HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.ModBy)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.ModTime).HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.ViewName)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Role).WithMany(p => p.TblRoleMembers)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblRoleMembers_tblRoles");

            entity.HasOne(e => e.AaudUser).WithMany(m => m.TblRoleMembers)
                .HasForeignKey(e => e.MemberId);
        });

        modelBuilder.Entity<TblRolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId });

            entity.ToTable("tblRolePermissions");

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.PermissionId).HasColumnName("PermissionID");
            //entity.Property(e => e.Access).HasDefaultValueSql("(1)");
            entity.Property(e => e.ModBy)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.ModTime).HasColumnType("datetime");

            entity.HasOne(d => d.Permission).WithMany(p => p.TblRolePermissions)
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblRolePermissions_tblPermissions");

            entity.HasOne(d => d.Role).WithMany(p => p.TblRolePermissions)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblRolePermissions_tblRoles");
        });

        modelBuilder.Entity<VmacsLog>(entity =>
        {
            entity.ToTable("vmacsLog");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("action");
            entity.Property(e => e.ErrorDetail)
                .HasMaxLength(8000)
                .IsUnicode(false)
                .HasColumnName("errorDetail");
            entity.Property(e => e.FileContent)
                .HasMaxLength(8000)
                .IsUnicode(false)
                .HasColumnName("fileContent");
            entity.Property(e => e.MothraId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("mothraID");
            entity.Property(e => e.RapsServer)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("rapsServer");
            entity.Property(e => e.StatusCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("statusCode");
            entity.Property(e => e.Timestamp)
                .HasColumnType("datetime")
                .HasColumnName("timestamp");
            entity.Property(e => e.UserLoginids)
                .IsUnicode(false)
                .HasColumnName("userLoginids");
            entity.Property(e => e.VmacsInstance)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("vmacsInstance");
        });

        modelBuilder.Entity<VwAaudUser>(entity =>
        { 
            entity.Property(e => e.AaudUserId).HasColumnName("aaudUserID");
            entity.Property(e => e.IamId).HasColumnName("iam_id");
            entity.Property(e => e.MothraId).HasColumnName("mothraId");
            entity.Property(e => e.LoginId).HasColumnName("loginId");
            entity.Property(e => e.MailId).HasColumnName("mailId");
            entity.Property(e => e.SpridenId).HasColumnName("spriden_id");
            entity.Property(e => e.Pidm).HasColumnName("pidm");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.VmacsId).HasColumnName("vmacs_id");
            entity.Property(e => e.VmcasId).HasColumnName("vmcas_id");
            entity.Property(e => e.MivId).HasColumnName("miv_id");
            entity.Property(e => e.DisplayFirstName).HasColumnName("display_first_name");
            entity.Property(e => e.DisplayLastName).HasColumnName("display_last_name");
            entity.Property(e => e.DisplayMiddleName).HasColumnName("display_middle_name");
            entity.Property(e => e.DisplayFullName).HasColumnName("display_full_name");
            entity.Property(e => e.CurrentStudent).HasColumnName("current_student");
            entity.Property(e => e.CurrentEmployee).HasColumnName("current_employee");
            entity.Property(e => e.Current).HasColumnName("current");
            entity.Property(e => e.FutureStudent).HasColumnName("future_student");
            entity.Property(e => e.FutureEmployee).HasColumnName("future_employee");
            entity.Property(e => e.Future).HasColumnName("future");
        });

        modelBuilder.Entity<VwCahfspersonnel>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_CAHFSPersonnel");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("memberID");
        });

        modelBuilder.Entity<VwCpl>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_CPL");

            entity.Property(e => e.EmpHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_home_dept");
            entity.Property(e => e.First)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Last)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwEmployee>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_employees");

            entity.Property(e => e.DisplayFirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("display_first_name");
            entity.Property(e => e.DisplayFullName)
                .HasMaxLength(91)
                .IsUnicode(false)
                .HasColumnName("display_full_name");
            entity.Property(e => e.DisplayLastName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("display_last_name");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("employee_id");
            entity.Property(e => e.LoginId)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("loginID");
            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("memberID");
            entity.Property(e => e.MothraId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("mothraID");
        });

        modelBuilder.Entity<VwFacultyAtLarge>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_facultyAtLarge");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwFacultySvm>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_Faculty_SVM");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwFederationFacultyAdjunct>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_FederationFacultyAdjunct");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwFederationFacultyCurrent>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_FederationFacultyCurrent");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwFederationFacultyHsclin>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_FederationFacultyHSClin");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwFederationFacultyLecturer>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_FederationFacultyLecturers");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwIdcardApprover>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_IDCard_Approvers");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("memberID");
        });

        modelBuilder.Entity<VwMivUcd>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_MIV_UCD");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwRoss>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_Ross");

            entity.Property(e => e.First)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Last)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwSenateFaculty>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_SenateFaculty");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwSenateFacultyCurrent>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_SenateFacultyCurrent");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwSenateFacultyEmeritu>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_SenateFacultyEmeritus");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwSenateFacultyProf>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_SenateFacultyProf");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwSenateFacultyProfClinBlank>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_SenateFacultyProfClinBlank");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwSenateFacultyProfInResidence>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_SenateFacultyProfInResidence");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwServiceCredit>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ServiceCredits");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwStaffSvm>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_Staff_SVM");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwStaffVet>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_StaffVets");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwStudentClubsMod>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_StudentClubsMods");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("memberId");
            entity.Property(e => e.MembersLoginId)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("members_loginId");
        });

        modelBuilder.Entity<VwStudentsDualDegree>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_STUDENTS_DUAL_DEGREE");

            entity.Property(e => e.IdsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ids_term_code");
            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwStudentsDualDegreeAway>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_STUDENTS_DUAL_DEGREE_AWAY");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwStudentsDvm>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_STUDENTS_DVM");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwStudentsDvmV1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_STUDENTS_DVM_V1");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwStudentsDvmV2>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_STUDENTS_DVM_V2");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwStudentsDvmV3>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_STUDENTS_DVM_V3");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwStudentsDvmV4>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_STUDENTS_DVM_V4");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwStudentsMpvm>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_STUDENTS_MPVM");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwSvmAcademicDeptStaffAndFaculty>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_SVM_AcademicDeptStaffAndFaculty");

            entity.Property(e => e.First)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.IdsTermCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ids_term_code");
            entity.Property(e => e.Last)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwSvmConstituent>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_SVM_Constituents");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmacsCape>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMACS_CAPE");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmacsCardiology>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMACS_Cardiology");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmacsComputerService>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMACS_Computer_Services");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmacsDentistry>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMACS_Dentistry");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmacsDermatology>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMACS_Dermatology");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmacsEqMedicine>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMACS_Eq_Medicine");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmacsEqReproduction>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMACS_Eq_Reproduction");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmacsEqSurgery>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_vmacs_Eq_Surgery");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmacsFaMedicine>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMACS_FA_Medicine");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmacsLaIcu>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMACS_LA_ICU");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmacsNeurology>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMACS_Neurology");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmacsOncology>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMACS_Oncology");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmacsOphthalmology>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMACS_Ophthalmology");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmacsRadiationOncology>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMACS_Radiation_Oncology");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmacsSaEmergency>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMACS_SA_Emergency");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmacsSaIcu>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMACS_SA_ICU");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmacsSaMedicine>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMACS_SA_Medicine");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmacsSaNursing>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMACS_SA_Nursing");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmacsSaOrthopedicSurgery>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMACS_SA_Orthopedic_Surgery");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmacsSaSurgery>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_vmacs_SA_Surgery");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmdo>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMDO");

            entity.Property(e => e.EmpHomeDept)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("emp_home_dept");
            entity.Property(e => e.First)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Last)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmdoAp>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMDO_AP");

            entity.Property(e => e.First)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Last)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.MemberId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmdoCat>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMDO_CATS");

            entity.Property(e => e.First)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Last)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.MemberId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmdoComm>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMDO_Comms");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("memberID");
        });

        modelBuilder.Entity<VwVmdoDevelopment>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMDO_DEVELOPMENT");

            entity.Property(e => e.First)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Last)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.MemberId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmdoSp>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMDO_SP");

            entity.Property(e => e.First)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Last)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.MemberId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmdoSvmIt>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMDO_SVM_IT");

            entity.Property(e => e.First)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Last)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.MemberId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmthClinician>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMTH_Clinicians");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmthConstituent>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMTH_Constituents");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmthRe>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMTH_Res");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmthStaff>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMTH_STAFF");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmthStaffByJob>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMTH_Staff_ByJob");

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
            entity.Property(e => e.First)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Last)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmthStudent>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMTH_students");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("memberID");
        });

        modelBuilder.Entity<VwVmthadmission>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMTHAdmissions");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmthinternsManual>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMTHInternsManual");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVmthtech>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VMTHTechs");

            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<VwVstp>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VSTP");

            entity.Property(e => e.First)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Last)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.MemberId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("MemberID");
        });

        modelBuilder.Entity<GetVmacsUserPermissionsResult>(entity =>
            entity.HasKey(e => e.MemberId));

        modelBuilder.Entity<GetAllRapsViews>(entity =>
            entity.HasKey(e => e.Name));

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

	public virtual void SetModified(object entity)
	{
		Entry(entity).State = EntityState.Modified;
	}
}
