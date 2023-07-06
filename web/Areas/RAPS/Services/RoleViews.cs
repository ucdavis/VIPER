using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;
using System.Linq.Dynamic.Core;
using System.Data;

namespace Viper.Areas.RAPS.Services
{
    public class RoleViews
    {
        private readonly RAPSContext _RAPSContext;
        private readonly RAPSAuditService _auditService;
        private static readonly string _ModByName = "__system";
        private static readonly string _AddComment = "Adding to role based on view {0}";
        private static readonly string _DeleteComment = "Removing from role based on view {0}";

        public RoleViews(RAPSContext RAPSContext)
        {
            _RAPSContext = RAPSContext;
            _auditService = new RAPSAuditService(RAPSContext);
        }

        public async Task<List<GetAllRapsViews>> GetViewNames()
        {
            return await _RAPSContext.GetAllRapsViews.FromSql($"dbo.usp_getAllRapsViews")
                .ToListAsync();
        }

        /// <summary>
        /// Update the membership of all roles defined by a view
        /// </summary>
        /// <param name="debugOnly"></param>
        /// <returns></returns>
        public async Task<List<string>> UpdateRoles(bool debugOnly=false)
        {
            List<string> messages = new();
            var roles = await _RAPSContext.TblRoles
                    .Where(r => !string.IsNullOrEmpty(r.ViewName))
                    .ToListAsync();
            foreach(var role in roles )
            {
                await UpdateRole(role, messages, debugOnly);
            }
            return messages;
        }

        /// <summary>
        /// Update the membership of a single role
        /// </summary>
        /// <param name="role">The role</param>
        /// <param name="messages">If running as a routine for multiple roles, the messages will be appended to this list.</param>
        /// <param name="debugOnly">If true, only write messages, don't change the DB</param>
        public async Task<List<string>> UpdateRole(TblRole role, List<string>? messages = null, bool debugOnly=false)
        {
            if(string.IsNullOrEmpty(role.ViewName))
            {
                return messages ?? new List<string>() { string.Format("Role {0} has no view", role.Role)};
            }
            messages ??= new();
            messages.Add(string.Format("Role {0} - View {1}", role.Role, role.ViewName));

            List<string?> members = await GetViewMembers(role.ViewName);
            List<TblRoleMember> roleMembers = await GetRoleMembers(role.RoleId, role.ViewName);

            List<string> toAdd = new();
            List<TblRoleMember> toDelete = new();

            //Add members that do not have the role, and for members added with an inactive date range, remove the membership with the date range and add the role
            foreach (string? member in members)
            {
                if(!string.IsNullOrEmpty(member))
                {
                    TblRoleMember? roleMember = roleMembers.FirstOrDefault(rm => rm.MemberId.Trim() == member.Trim());
                    if (roleMember == null)
                    {
                        messages.Add(string.Format("Adding {0}", member));
                        toAdd.Add(member);
                    }
                    else if(roleMember.StartDate > DateTime.Now || roleMember.EndDate < DateTime.Now)
                    {
                        messages.Add(string.Format("Converting {0} to role membership without dates", member));
                        toDelete.Add(roleMember);
                        toAdd.Add(member);
                    }
                }
            }

            //Remove members that were added via this view if they are no longer in the view. Check first that the view is not empty.
            if(members.Count > 0) {
                foreach (TblRoleMember roleMember in roleMembers)
                {
                    if(!string.IsNullOrEmpty(roleMember.MemberId.Trim()) && !members.Contains(roleMember.MemberId)
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

            if(!debugOnly)
            {
                foreach(string toAddMemberId in toAdd)
                {
                    AddRoleMember(role.RoleId, toAddMemberId, role.ViewName);
                }
                foreach(TblRoleMember toDeleteMember in toDelete)
                {
                    DeleteRoleMember(toDeleteMember, role.ViewName);
                }
            }
            return messages;
        }

        private void AddRoleMember(int roleId, string memberId, string viewName)
        {
            using var transaction = _RAPSContext.Database.BeginTransaction();
            TblRoleMember tblRoleMember = new()
            {
                RoleId = roleId,
                MemberId = memberId,
                ViewName = viewName,
                ModTime = DateTime.Now,
                ModBy = _ModByName
            };
            _RAPSContext.TblRoleMembers.Add(tblRoleMember);
            _RAPSContext.SaveChanges();
            _auditService.AuditRoleMemberChange(tblRoleMember, RAPSAuditService.AuditActionType.Create, string.Format(_AddComment, viewName));
            _RAPSContext.SaveChanges();
            transaction.Commit();
        }

        private void DeleteRoleMember(TblRoleMember deleteMember, string viewName)
        {
            _RAPSContext.TblRoleMembers.Remove(deleteMember);
            _auditService.AuditRoleMemberChange(deleteMember, RAPSAuditService.AuditActionType.Delete, string.Format(_DeleteComment, viewName));
            _RAPSContext.SaveChanges();
        }

        private async Task<List<TblRoleMember>> GetRoleMembers(int roleId, string viewName)
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
                "vw_cahfspersonnel" => await _RAPSContext.VwCahfspersonnel.Select(v => v.MemberId).ToListAsync(),
                "vw_cpl" => await _RAPSContext.VwCpls.Select(v => v.MemberId).ToListAsync(),
                "vw_employees" => await _RAPSContext.VwEmployees.Select(v => v.MemberId).ToListAsync(),
                "vw_facultyatlarge" => await _RAPSContext.VwFacultyAtLarges.Select(v => v.MemberId).ToListAsync(),
                "vw_faculty_svm" => await _RAPSContext.VwFacultySvms.Select(v => v.MemberId).ToListAsync(),
                "vw_federationfacultyadjunct" => await _RAPSContext.VwFederationFacultyAdjuncts.Select(v => v.MemberId).ToListAsync(),
                "vw_federationfacultycurrent" => await _RAPSContext.VwFederationFacultyCurrents.Select(v => v.MemberId).ToListAsync(),
                "vw_federationfacultyhsclin" => await _RAPSContext.VwFederationFacultyHsclins.Select(v => v.MemberId).ToListAsync(),
                "vw_federationfacultylecturers" => await _RAPSContext.VwFederationFacultyLecturers.Select(v => v.MemberId).ToListAsync(),
                "vw_idcard_approvers" => await _RAPSContext.VwIdcardApprovers.Select(v => v.MemberId).ToListAsync(),
                "vw_miv_ucd" => await _RAPSContext.VwMivUcds.Select(v => v.MemberId).ToListAsync(),
                "vw_ross" => await _RAPSContext.VwRosses.Select(v => v.MemberId).ToListAsync(),
                "vw_senatefaculty" => await _RAPSContext.VwSenateFaculties.Select(v => v.MemberId).ToListAsync(),
                "vw_senatefacultycurrent" => await _RAPSContext.VwSenateFacultyCurrents.Select(v => v.MemberId).ToListAsync(),
                "vw_senatefacultyemeritus" => await _RAPSContext.VwSenateFacultyEmeritus.Select(v => v.MemberId).ToListAsync(),
                "vw_senatefacultyprof" => await _RAPSContext.VwSenateFacultyProfs.Select(v => v.MemberId).ToListAsync(),
                "vw_senatefacultyprofclinblank" => await _RAPSContext.VwSenateFacultyProfClinBlanks.Select(v => v.MemberId).ToListAsync(),
                "vw_senatefacultyprofinresidence" => await _RAPSContext.VwSenateFacultyProfInResidences.Select(v => v.MemberId).ToListAsync(),
                "vw_servicecredits" => await _RAPSContext.VwServiceCredits.Select(v => v.MemberId).ToListAsync(),
                "vw_staff_svm" => await _RAPSContext.VwStaffSvms.Select(v => v.MemberId).ToListAsync(),
                "vw_staffvets" => await _RAPSContext.VwStaffVets.Select(v => v.MemberId).ToListAsync(),
                "vw_studentclubsmods" => await _RAPSContext.VwStudentClubsMods.Select(v => v.MemberId).ToListAsync(),
                "vw_students_dual_degree" => await _RAPSContext.VwStudentsDualDegrees.Select(v => v.MemberId).ToListAsync(),
                "vw_students_dual_degree_away" => await _RAPSContext.VwStudentsDualDegreeAways.Select(v => v.MemberId).ToListAsync(),
                "vw_students_dvm" => await _RAPSContext.VwStudentsDvms.Select(v => v.MemberId).ToListAsync(),
                "vw_students_dvm_v1" => await _RAPSContext.VwStudentsDvmV1s.Select(v => v.MemberId).ToListAsync(),
                "vw_students_dvm_v2" => await _RAPSContext.VwStudentsDvmV2s.Select(v => v.MemberId).ToListAsync(),
                "vw_students_dvm_v3" => await _RAPSContext.VwStudentsDvmV3s.Select(v => v.MemberId).ToListAsync(),
                "vw_students_dvm_v4" => await _RAPSContext.VwStudentsDvmV4s.Select(v => v.MemberId).ToListAsync(),
                "vw_students_mpvm" => await _RAPSContext.VwStudentsMpvms.Select(v => v.MemberId).ToListAsync(),
                "vw_svmacademicdeptstaffandfaculty" => await _RAPSContext.VwSvmAcademicDeptStaffAndFaculties.Select(v => v.MemberId).ToListAsync(),
                "vw_svm_constituents" => await _RAPSContext.VwSvmConstituents.Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_cape" => await _RAPSContext.VwVmacsCapes.Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_cardiology" => await _RAPSContext.VwVmacsCardiologies.Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_computer_services" => await _RAPSContext.VwVmacsComputerServices.Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_dentistry" => await _RAPSContext.VwVmacsDentistries.Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_dermatology" => await _RAPSContext.VwVmacsDermatologies.Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_eq_medicine" => await _RAPSContext.VwVmacsEqMedicines.Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_eq_reproduction" => await _RAPSContext.VwVmacsEqReproductions.Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_eq_surgery" => await _RAPSContext.VwVmacsEqSurgeries.Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_fa_medicine" => await _RAPSContext.VwVmacsFaMedicines.Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_la_icu" => await _RAPSContext.VwVmacsLaIcus.Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_neurology" => await _RAPSContext.VwVmacsNeurologies.Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_oncology" => await _RAPSContext.VwVmacsOncologies.Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_ophthalmology" => await _RAPSContext.VwVmacsOphthalmologies.Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_radiation_oncology" => await _RAPSContext.VwVmacsRadiationOncologies.Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_sa_emergency" => await _RAPSContext.VwVmacsSaEmergencies.Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_sa_icu" => await _RAPSContext.VwVmacsSaIcus.Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_sa_medicine" => await _RAPSContext.VwVmacsSaMedicines.Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_sa_nursing" => await _RAPSContext.VwVmacsSaNursings.Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_sa_orthopedic_surgery" => await _RAPSContext.VwVmacsSaOrthopedicSurgeries.Select(v => v.MemberId).ToListAsync(),
                "vw_vmacs_sa_surgery" => await _RAPSContext.VwVmacsSaSurgeries.Select(v => v.MemberId).ToListAsync(),
                "vw_vmdo" => await _RAPSContext.VwVmdos.Select(v => v.MemberId).ToListAsync(),
                "vw_vmdo_ap" => await _RAPSContext.VwVmdoAps.Select(v => v.MemberId).ToListAsync(),
                "vw_vmdo_cats" => await _RAPSContext.VwVmdoCats.Select(v => v.MemberId).ToListAsync(),
                "vw_vmdo_comms" => await _RAPSContext.VwVmdoComms.Select(v => v.MemberId).ToListAsync(),
                "vw_vmdo_development" => await _RAPSContext.VwVmdoDevelopments.Select(v => v.MemberId).ToListAsync(),
                "vw_vmdo_sp" => await _RAPSContext.VwVmdoSps.Select(v => v.MemberId).ToListAsync(),
                "vw_vmdo_svm_it" => await _RAPSContext.VwVmdoSvmIts.Select(v => v.MemberId).ToListAsync(),
                "vw_vmthadmissions" => await _RAPSContext.VwVmthadmissions.Select(v => v.MemberId).ToListAsync(),
                "vw_vmth_clinicians" => await _RAPSContext.VwVmthClinicians.Select(v => v.MemberId).ToListAsync(),
                "vw_vmth_constituents" => await _RAPSContext.VwVmthConstituents.Select(v => v.MemberId).ToListAsync(),
                "vw_vmthinternsmanual" => await _RAPSContext.VwVmthinternsManuals.Select(v => v.MemberId).ToListAsync(),
                "vw_vmth_res" => await _RAPSContext.VwVmthRes.Select(v => v.MemberId).ToListAsync(),
                "vw_vmth_staff" => await _RAPSContext.VwVmthStaffs.Select(v => v.MemberId).ToListAsync(),
                "vw_vmth_staff_byjob" => await _RAPSContext.VwVmthStaffByJobs.Select(v => v.MemberId).ToListAsync(),
                "vw_vmth_students" => await _RAPSContext.VwVmthStudents.Select(v => v.MemberId).ToListAsync(),
                "vw_vmthtechs" => await _RAPSContext.VwVmthtechs.Select(v => v.MemberId).ToListAsync(),
                "vw_vstp" => await _RAPSContext.VwVstps.Select(v => v.MemberId).ToListAsync(),
                _ => new List<string?>(),
            };
        }
    }
}
