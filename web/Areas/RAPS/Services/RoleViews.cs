using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Services
{
    public class RoleViews
    {
        /// <summary>Default audit actor when no caller-supplied value is given (e.g. legacy callers).</summary>
        public const string DefaultModBy = "__system";

        private readonly RAPSContext _RAPSContext;
        private readonly RAPSAuditService _auditService;
        private static readonly string _AddComment = "Adding to role based on view {0}";
        private static readonly string _DeleteComment = "Removing from role based on view {0}";

        public RoleViews(RAPSContext RAPSContext)
        {
            _RAPSContext = RAPSContext;
            _auditService = new RAPSAuditService(RAPSContext);
        }

        private static string ResolveActor(string? modBy)
            => string.IsNullOrWhiteSpace(modBy) ? DefaultModBy : modBy;

        public async Task<List<string>> GetViewNames()
        {
            List<GetAllRapsViews> allViews = await _RAPSContext.GetAllRapsViews.FromSql($"dbo.usp_getAllRapsViews")
                .ToListAsync();
            return allViews.AsEnumerable().Select(v => v.Name).ToList();
        }

        /// <summary>
        /// Update the membership of all roles defined by a view.
        /// </summary>
        /// <param name="modBy">
        /// Audit actor stamped on every <c>TblRoleMember</c> and <c>TblLog</c>
        /// row written by this run. Pass <c>"__sched"</c> for nightly
        /// recurring runs, the LoginId for manual admin runs, or rely on the
        /// <see cref="DefaultModBy"/> for legacy callers.
        /// </param>
        /// <param name="debugOnly">If true, only write messages, don't change the DB.</param>
        public async Task<List<string>> UpdateRoles(string? modBy = null, bool debugOnly = false)
        {
            var actor = ResolveActor(modBy);
            List<string> messages = new();
            var roles = await _RAPSContext.TblRoles
                    .Where(r => !string.IsNullOrEmpty(r.ViewName))
                    .ToListAsync();
            foreach (var role in roles)
            {
                await UpdateRole(role, messages, debugOnly, actor);
            }
            return messages;
        }

        /// <summary>
        /// Update the membership of a single role
        /// </summary>
        /// <param name="role">The role</param>
        /// <param name="messages">If running as a routine for multiple roles, the messages will be appended to this list.</param>
        /// <param name="debugOnly">If true, only write messages, don't change the DB</param>
        /// <param name="modBy">Audit actor; defaults to <see cref="DefaultModBy"/>.</param>
        public async Task<List<string>> UpdateRole(TblRole role, List<string>? messages = null, bool debugOnly = false, string? modBy = null)
        {
            if (string.IsNullOrEmpty(role.ViewName))
            {
                return messages ?? new List<string> { string.Format("Role {0} has no view", role.Role) };
            }
            messages ??= new();
            messages.Add(string.Format("Role {0} - View {1}", role.Role, role.ViewName));

            List<string?> members = await GetViewMembers(role.ViewName);
            List<TblRoleMember> roleMembers = await GetRoleMembers(role.RoleId);

            List<string> toAdd = new();
            List<TblRoleMember> toDelete = new();

            //Add members that do not have the role, and for members added with an inactive date range, remove the membership with the date range and add the role
            foreach (string member in members.Where(m => !string.IsNullOrEmpty(m))!)
            {
                TblRoleMember? roleMember = roleMembers.FirstOrDefault(rm => rm.MemberId.Trim() == member.Trim());
                if (roleMember == null)
                {
                    messages.Add(string.Format("Adding {0}", member));
                    toAdd.Add(member);
                }
                else if (roleMember.StartDate > DateTime.Now || roleMember.EndDate < DateTime.Now)
                {
                    messages.Add(string.Format("Converting {0} to role membership without dates", member));
                    toDelete.Add(roleMember);
                    toAdd.Add(member);
                }
            }

            //Remove members that were added via this view if they are no longer in the view. Check first that the view is not empty.
            if (members.Count > 0)
            {
                foreach (TblRoleMember roleMember in roleMembers)
                {
                    if (!string.IsNullOrEmpty(roleMember.MemberId.Trim()) && !members.Contains(roleMember.MemberId)
                        && roleMember.ViewName == role.ViewName)
                    {
                        messages.Add(string.Format("Removing {0}", roleMember.MemberId));
                        toDelete.Add(roleMember);
                    }
                }
            }
            else
            {
                messages.Add(string.Format("View {0} has 0 members", role.ViewName));
            }

            var actor = ResolveActor(modBy);
            if (!debugOnly)
            {
                foreach (string toAddMemberId in toAdd)
                {
                    AddRoleMember(role.RoleId, toAddMemberId, role.ViewName, actor);
                }
                foreach (TblRoleMember toDeleteMember in toDelete)
                {
                    DeleteRoleMember(toDeleteMember, role.ViewName, actor);
                }
            }
            return messages;
        }

        private void AddRoleMember(int roleId, string memberId, string viewName, string modBy)
        {
            using var transaction = _RAPSContext.Database.BeginTransaction();
            TblRoleMember tblRoleMember = new()
            {
                RoleId = roleId,
                MemberId = memberId,
                ViewName = viewName,
                ModTime = DateTime.Now,
                ModBy = modBy
            };
            _RAPSContext.TblRoleMembers.Add(tblRoleMember);
            _RAPSContext.SaveChanges();
            _auditService.AuditRoleMemberChange(tblRoleMember, RAPSAuditService.AuditActionType.Create, string.Format(_AddComment, viewName), modBy);
            _RAPSContext.SaveChanges();
            transaction.Commit();
        }

        private void DeleteRoleMember(TblRoleMember deleteMember, string viewName, string modBy)
        {
            _RAPSContext.TblRoleMembers.Remove(deleteMember);
            _auditService.AuditRoleMemberChange(deleteMember, RAPSAuditService.AuditActionType.Delete, string.Format(_DeleteComment, viewName), modBy);
            _RAPSContext.SaveChanges();
        }

        private async Task<List<TblRoleMember>> GetRoleMembers(int roleId)
        {
            return await _RAPSContext.TblRoleMembers
                .Where(rm => rm.RoleId == roleId)
                .ToListAsync();
        }

        private async Task<List<string?>> GetViewMembers(string viewName)
        {
            //There's probably a better way to do this
            return viewName.ToLower() switch
            {
                "vw_cahfspersonnel" => await _RAPSContext.VwCahfspersonnel.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_cpl" => await _RAPSContext.VwCpls.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_employees" => await _RAPSContext.VwEmployees.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_facultyatlarge" => await _RAPSContext.VwFacultyAtLarges.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_faculty_svm" => await _RAPSContext.VwFacultySvms.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_federationfacultyadjunct" => await _RAPSContext.VwFederationFacultyAdjuncts.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_federationfacultycurrent" => await _RAPSContext.VwFederationFacultyCurrents.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_federationfacultyhsclin" => await _RAPSContext.VwFederationFacultyHsclins.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_federationfacultylecturers" => await _RAPSContext.VwFederationFacultyLecturers.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_idcard_approvers" => await _RAPSContext.VwIdcardApprovers.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_miv_ucd" => await _RAPSContext.VwMivUcds.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_ross" => await _RAPSContext.VwRosses.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_senatefaculty" => await _RAPSContext.VwSenateFaculties.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_senatefacultycurrent" => await _RAPSContext.VwSenateFacultyCurrents.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_senatefacultyemeritus" => await _RAPSContext.VwSenateFacultyEmeritus.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_senatefacultyprof" => await _RAPSContext.VwSenateFacultyProfs.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_senatefacultyprofclinblank" => await _RAPSContext.VwSenateFacultyProfClinBlanks.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_senatefacultyprofinresidence" => await _RAPSContext.VwSenateFacultyProfInResidences.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_servicecredits" => await _RAPSContext.VwServiceCredits.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_staff_svm" => await _RAPSContext.VwStaffSvms.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_staffvets" => await _RAPSContext.VwStaffVets.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_studentclubsmods" => await _RAPSContext.VwStudentClubsMods.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_students_dual_degree" => await _RAPSContext.VwStudentsDualDegrees.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_students_dual_degree_away" => await _RAPSContext.VwStudentsDualDegreeAways.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_students_dvm" => await _RAPSContext.VwStudentsDvms.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_students_dvm_v1" => await _RAPSContext.VwStudentsDvmV1s.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_students_dvm_v2" => await _RAPSContext.VwStudentsDvmV2s.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_students_dvm_v3" => await _RAPSContext.VwStudentsDvmV3s.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_students_dvm_v4" => await _RAPSContext.VwStudentsDvmV4s.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_students_mpvm" => await _RAPSContext.VwStudentsMpvms.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_svmacademicdeptstaffandfaculty" => await _RAPSContext.VwSvmAcademicDeptStaffAndFaculties.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_svm_constituents" => await _RAPSContext.VwSvmConstituents.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_cape" => await _RAPSContext.VwVmacsCapes.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_cardiology" => await _RAPSContext.VwVmacsCardiologies.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_computer_services" => await _RAPSContext.VwVmacsComputerServices.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_dentistry" => await _RAPSContext.VwVmacsDentistries.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_dermatology" => await _RAPSContext.VwVmacsDermatologies.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_eq_medicine" => await _RAPSContext.VwVmacsEqMedicines.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_eq_reproduction" => await _RAPSContext.VwVmacsEqReproductions.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_eq_surgery" => await _RAPSContext.VwVmacsEqSurgeries.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_fa_medicine" => await _RAPSContext.VwVmacsFaMedicines.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_la_icu" => await _RAPSContext.VwVmacsLaIcus.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_neurology" => await _RAPSContext.VwVmacsNeurologies.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_oncology" => await _RAPSContext.VwVmacsOncologies.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_ophthalmology" => await _RAPSContext.VwVmacsOphthalmologies.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_radiation_oncology" => await _RAPSContext.VwVmacsRadiationOncologies.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_sa_emergency" => await _RAPSContext.VwVmacsSaEmergencies.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_sa_icu" => await _RAPSContext.VwVmacsSaIcus.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_sa_medicine" => await _RAPSContext.VwVmacsSaMedicines.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_sa_nursing" => await _RAPSContext.VwVmacsSaNursings.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_sa_orthopedic_surgery" => await _RAPSContext.VwVmacsSaOrthopedicSurgeries.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_sa_surgery" => await _RAPSContext.VwVmacsSaSurgeries.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmdo" => await _RAPSContext.VwVmdos.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmdo_ap" => await _RAPSContext.VwVmdoAps.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmdo_cats" => await _RAPSContext.VwVmdoCats.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmdo_comms" => await _RAPSContext.VwVmdoComms.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmdo_development" => await _RAPSContext.VwVmdoDevelopments.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmdo_sp" => await _RAPSContext.VwVmdoSps.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmdo_svm_it" => await _RAPSContext.VwVmdoSvmIts.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmthadmissions" => await _RAPSContext.VwVmthadmissions.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
				"vw_vmth_chiefs" => await _RAPSContext.VwVmthChiefs.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmth_clinicians" => await _RAPSContext.VwVmthClinicians.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmth_constituents" => await _RAPSContext.VwVmthConstituents.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmthinternsmanual" => await _RAPSContext.VwVmthinternsManuals.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmth_res" => await _RAPSContext.VwVmthRes.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmth_staff" => await _RAPSContext.VwVmthStaffs.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmth_staff_byjob" => await _RAPSContext.VwVmthStaffByJobs.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmth_students" => await _RAPSContext.VwVmthStudents.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vmthtechs" => await _RAPSContext.VwVmthtechs.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                "vw_vstp" => await _RAPSContext.VwVstps.AsNoTracking().Select(v => v.MemberId).ToListAsync(),
                _ => new List<string?>(),
            };
        }

}
