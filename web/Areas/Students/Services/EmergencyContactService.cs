using Microsoft.EntityFrameworkCore;
using Viper.Areas.RAPS.Services;
using Viper.Areas.Students.Constants;
using Viper.Areas.Students.Models;
using Viper.Areas.Students.Models.Entities;
using Viper.Classes.SQLContext;

namespace Viper.Areas.Students.Services;

public class EmergencyContactService : IEmergencyContactService
{
    private readonly SISContext _sisContext;
    private readonly RAPSContext _rapsContext;
    private readonly VIPERContext _viperContext;
    private readonly AAUDContext _aaudContext;
    private readonly IUserHelper _userHelper;
    private readonly RAPSCacheService _rapsCacheService;

    public EmergencyContactService(
        SISContext sisContext,
        RAPSContext rapsContext,
        VIPERContext viperContext,
        AAUDContext aaudContext,
        IUserHelper userHelper)
    {
        _sisContext = sisContext;
        _rapsContext = rapsContext;
        _viperContext = viperContext;
        _aaudContext = aaudContext;
        _userHelper = userHelper;
        _rapsCacheService = new RAPSCacheService(rapsContext, aaudContext, userHelper);
    }

    public async Task<List<StudentContactListItemDto>> GetStudentContactListAsync()
    {
        var studentList = new StudentList(_viperContext);
        var students = await studentList.GetStudents();

        // Get all PIDMs for these students to batch-load contact data
        var personIds = students.Select(s => s.PersonId).ToList();
        var pidmMap = await GetPersonIdToPidmMapAsync(personIds);
        var pidms = pidmMap.Values.ToList();

        // Load all contact data in one query
        var contacts = await _sisContext.StudentContacts
            .Include(c => c.EmergencyContacts)
            .Where(c => EF.Parameter(pidms).Contains(c.Pidm))
            .AsNoTracking()
            .ToListAsync();

        var contactsByPidm = contacts.ToDictionary(c => c.Pidm);

        var result = new List<StudentContactListItemDto>();
        foreach (var student in students)
        {
            var item = new StudentContactListItemDto
            {
                PersonId = student.PersonId,
                FullName = student.FullName,
                ClassLevel = student.ClassLevel ?? string.Empty
            };

            if (pidmMap.TryGetValue(student.PersonId, out var pidm)
                && contactsByPidm.TryGetValue(pidm, out var contact))
            {
                item.StudentInfoComplete = CalculateStudentInfoCompleteness(contact);
                item.LocalContactComplete = CalculateContactCompleteness(
                    contact.EmergencyContacts.FirstOrDefault(e => e.Type == "local"));
                item.EmergencyContactComplete = CalculateContactCompleteness(
                    contact.EmergencyContacts.FirstOrDefault(e => e.Type == "emergency"));
                item.PermanentContactComplete = CalculateContactCompleteness(
                    contact.EmergencyContacts.FirstOrDefault(e => e.Type == "permanent"));
                item.LastUpdated = contact.LastUpdated;
            }

            result.Add(item);
        }

        return result;
    }

    public async Task<StudentContactDetailDto?> GetStudentContactDetailAsync(int personId, bool canEdit)
    {
        var studentList = new StudentList(_viperContext);
        var student = await studentList.GetStudent(personId);
        if (student == null)
        {
            return null;
        }

        var pidm = await GetPidmForPersonIdAsync(personId);
        if (pidm == null)
        {
            // Student exists but has no PIDM mapping — return empty contact shell
            return new StudentContactDetailDto
            {
                PersonId = student.PersonId,
                FullName = student.FullName,
                ClassLevel = student.ClassLevel ?? string.Empty,
                CanEdit = canEdit
            };
        }

        var contact = await _sisContext.StudentContacts
            .Include(c => c.EmergencyContacts)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Pidm == pidm.Value);

        var dto = new StudentContactDetailDto
        {
            PersonId = student.PersonId,
            FullName = student.FullName,
            ClassLevel = student.ClassLevel ?? string.Empty,
            CanEdit = canEdit
        };

        if (contact != null)
        {
            dto.StudentInfo = EmergencyContactMapper.ToStudentInfoDto(contact);
            dto.ContactPermanent = contact.ContactPermanent;
            dto.LastUpdated = contact.LastUpdated;
            dto.UpdatedBy = contact.UpdatedBy;

            dto.LocalContact = EmergencyContactMapper.ToContactInfoDto(
                contact.EmergencyContacts.FirstOrDefault(e => e.Type == "local"));
            dto.EmergencyContact = EmergencyContactMapper.ToContactInfoDto(
                contact.EmergencyContacts.FirstOrDefault(e => e.Type == "emergency"));
            dto.PermanentContact = EmergencyContactMapper.ToContactInfoDto(
                contact.EmergencyContacts.FirstOrDefault(e => e.Type == "permanent"));
        }

        return dto;
    }

    public async Task UpdateStudentContactAsync(int personId, UpdateStudentContactRequest request, string updatedBy)
    {
        if (!await IsCurrentDvmStudentAsync(personId))
        {
            throw new InvalidOperationException($"PersonId {personId} is not a current DVM student");
        }

        var pidm = await GetPidmForPersonIdAsync(personId);
        if (pidm == null)
        {
            throw new InvalidOperationException($"No PIDM found for PersonId {personId}");
        }

        // Validate all phone fields server-side before persisting
        var invalidFields = new List<string>();
        ValidatePhone(request.StudentInfo.HomePhone, "Student Home Phone", invalidFields);
        ValidatePhone(request.StudentInfo.CellPhone, "Student Cell Phone", invalidFields);
        ValidatePhone(request.LocalContact.WorkPhone, "Local Contact Work Phone", invalidFields);
        ValidatePhone(request.LocalContact.HomePhone, "Local Contact Home Phone", invalidFields);
        ValidatePhone(request.LocalContact.CellPhone, "Local Contact Cell Phone", invalidFields);
        ValidatePhone(request.EmergencyContact.WorkPhone, "Emergency Contact Work Phone", invalidFields);
        ValidatePhone(request.EmergencyContact.HomePhone, "Emergency Contact Home Phone", invalidFields);
        ValidatePhone(request.EmergencyContact.CellPhone, "Emergency Contact Cell Phone", invalidFields);
        ValidatePhone(request.PermanentContact.WorkPhone, "Permanent Contact Work Phone", invalidFields);
        ValidatePhone(request.PermanentContact.HomePhone, "Permanent Contact Home Phone", invalidFields);
        ValidatePhone(request.PermanentContact.CellPhone, "Permanent Contact Cell Phone", invalidFields);
        if (invalidFields.Count > 0)
        {
            throw new ArgumentException(
                $"Invalid phone number(s): {string.Join(", ", invalidFields)}");
        }

        await using var transaction = await _sisContext.Database.BeginTransactionAsync();

        var contact = await _sisContext.StudentContacts
            .Include(c => c.EmergencyContacts)
            .FirstOrDefaultAsync(c => c.Pidm == pidm.Value);

        if (contact == null)
        {
            contact = new StudentContact { Pidm = pidm.Value };
            _sisContext.StudentContacts.Add(contact);
        }

        // Update student info
        EmergencyContactMapper.ApplyStudentInfoToEntity(request.StudentInfo, contact);
        contact.ContactPermanent = request.ContactPermanent;
        contact.LastUpdated = DateTime.Now;
        contact.UpdatedBy = updatedBy;

        await _sisContext.SaveChangesAsync();

        // Upsert each contact type
        UpsertEmergencyContact(contact, "local", request.LocalContact);
        UpsertEmergencyContact(contact, "emergency", request.EmergencyContact);
        UpsertEmergencyContact(contact, "permanent", request.PermanentContact);

        await _sisContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<List<StudentContactReportDto>> GetStudentContactReportAsync()
    {
        var studentList = new StudentList(_viperContext);
        var students = await studentList.GetStudents();

        var personIds = students.Select(s => s.PersonId).ToList();
        var pidmMap = await GetPersonIdToPidmMapAsync(personIds);
        var pidms = pidmMap.Values.ToList();

        var contacts = await _sisContext.StudentContacts
            .Include(c => c.EmergencyContacts)
            .Where(c => EF.Parameter(pidms).Contains(c.Pidm))
            .AsNoTracking()
            .ToListAsync();

        var contactsByPidm = contacts.ToDictionary(c => c.Pidm);

        var result = new List<StudentContactReportDto>();
        foreach (var student in students)
        {
            var dto = new StudentContactReportDto
            {
                PersonId = student.PersonId,
                FullName = student.FullName,
                ClassLevel = student.ClassLevel ?? string.Empty
            };

            if (pidmMap.TryGetValue(student.PersonId, out var pidm)
                && contactsByPidm.TryGetValue(pidm, out var contact))
            {
                var studentInfo = EmergencyContactMapper.ToStudentInfoDto(contact);
                dto.Address = studentInfo.Address;
                dto.City = studentInfo.City;
                dto.Zip = studentInfo.Zip;
                dto.HomePhone = studentInfo.HomePhone;
                dto.CellPhone = studentInfo.CellPhone;
                dto.ContactPermanent = contact.ContactPermanent;

                dto.LocalContact = EmergencyContactMapper.ToContactInfoDto(
                    contact.EmergencyContacts.FirstOrDefault(e => e.Type == "local"));
                dto.EmergencyContact = EmergencyContactMapper.ToContactInfoDto(
                    contact.EmergencyContacts.FirstOrDefault(e => e.Type == "emergency"));
                dto.PermanentContact = EmergencyContactMapper.ToContactInfoDto(
                    contact.EmergencyContacts.FirstOrDefault(e => e.Type == "permanent"));
            }

            result.Add(dto);
        }

        return result;
    }

    public async Task<AppAccessStatusDto> GetAccessStatusAsync()
    {
        var appOpen = await IsAppOpenAsync();
        var permissionId = await GetStudentPermissionIdAsync();

        // Get individual member permissions for the emergency contact student permission
        var mothraIds = await _rapsContext.TblMemberPermissions
            .Where(mp => mp.PermissionId == permissionId
                && mp.Access == 1
                && (mp.StartDate == null || mp.StartDate <= DateTime.Now)
                && (mp.EndDate == null || mp.EndDate >= DateTime.Now))
            .Select(mp => mp.MemberId)
            .ToListAsync();

        // Map MothraIds to person info in a single query
        var grants = await _aaudContext.AaudUsers
            .Where(u => mothraIds.Contains(u.MothraId))
            .Select(u => new IndividualAccessDto
            {
                PersonId = u.AaudUserId,
                FullName = u.DisplayFullName
            })
            .OrderBy(g => g.FullName)
            .ToListAsync();

        return new AppAccessStatusDto
        {
            AppOpen = appOpen,
            IndividualGrants = grants
        };
    }

    public async Task<bool> ToggleAppAccessAsync()
    {
        var permissionId = await GetStudentPermissionIdAsync();
        var roleId = await GetStudentRoleIdAsync();
        var isOpen = await IsAppOpenAsync();
        if (isOpen)
        {
            // Close: remove all matching role-permission grants (handles duplicates)
            var rolePermissions = await _rapsContext.TblRolePermissions
                .Where(rp => rp.RoleId == roleId
                    && rp.PermissionId == permissionId)
                .ToListAsync();
            if (rolePermissions.Count > 0)
            {
                _rapsContext.TblRolePermissions.RemoveRange(rolePermissions);
                await _rapsContext.SaveChangesAsync();
            }

            ClearCacheForRoleMembers(roleId);
            return false;
        }
        else
        {
            // Open: add the role-permission grant only if not already present
            var alreadyExists = await _rapsContext.TblRolePermissions
                .AnyAsync(rp => rp.RoleId == roleId
                    && rp.PermissionId == permissionId
                    && rp.Access == 1);
            if (!alreadyExists)
            {
                var rolePermission = new Viper.Models.RAPS.TblRolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId,
                    Access = 1,
                    ModTime = DateTime.Now
                };
                _rapsContext.TblRolePermissions.Add(rolePermission);
                await _rapsContext.SaveChangesAsync();
            }

            ClearCacheForRoleMembers(roleId);
            return true;
        }
    }

    public async Task<bool> ToggleIndividualAccessAsync(int personId)
    {
        if (!await IsCurrentDvmStudentAsync(personId))
        {
            throw new InvalidOperationException($"PersonId {personId} is not a current DVM student");
        }

        var user = await _aaudContext.AaudUsers
            .FirstOrDefaultAsync(u => u.AaudUserId == personId);
        if (user == null)
        {
            throw new InvalidOperationException($"No user found for PersonId {personId}");
        }

        var permissionId = await GetStudentPermissionIdAsync();
        var existing = await _rapsContext.TblMemberPermissions
            .Where(mp => mp.MemberId == user.MothraId
                && mp.PermissionId == permissionId
                && mp.Access == 1
                && (mp.StartDate == null || mp.StartDate <= DateTime.Now)
                && (mp.EndDate == null || mp.EndDate >= DateTime.Now))
            .ToListAsync();

        if (existing.Count > 0)
        {
            // Remove all matching individual grants (handles duplicates)
            _rapsContext.TblMemberPermissions.RemoveRange(existing);
            await _rapsContext.SaveChangesAsync();
            _rapsCacheService.ClearCachedRolesAndPermissionsForUser(user.MothraId);
            return false;
        }
        else
        {
            // Add individual grant
            var memberPermission = new Viper.Models.RAPS.TblMemberPermission
            {
                MemberId = user.MothraId,
                PermissionId = permissionId,
                Access = 1,
                AddDate = DateTime.Now,
                ModTime = DateTime.Now
            };
            _rapsContext.TblMemberPermissions.Add(memberPermission);
            await _rapsContext.SaveChangesAsync();
            _rapsCacheService.ClearCachedRolesAndPermissionsForUser(user.MothraId);
            return true;
        }
    }

    public async Task<bool> CanEditAsync(int personId, string? currentLoginId)
    {
        if (string.IsNullOrEmpty(currentLoginId))
        {
            return false;
        }

        var currentUser = await _aaudContext.AaudUsers
            .FirstOrDefaultAsync(u => u.LoginId == currentLoginId);
        if (currentUser == null)
        {
            return false;
        }

        // Admin can always edit
        if (_userHelper.HasPermission(_rapsContext, currentUser, EmergencyContactPermissions.Admin))
        {
            return true;
        }

        // SIS users cannot edit (view only)
        if (_userHelper.HasPermission(_rapsContext, currentUser, EmergencyContactPermissions.SISAllStudents))
        {
            return false;
        }

        // Students can edit their own record if app is open or individually granted
        if (currentUser.AaudUserId == personId)
        {
            if (await IsAppOpenAsync())
            {
                return true;
            }

            // Check individual grant
            var permissionId = await GetStudentPermissionIdAsync();
            var hasIndividualGrant = await _rapsContext.TblMemberPermissions
                .AnyAsync(mp => mp.MemberId == currentUser.MothraId
                    && mp.PermissionId == permissionId
                    && mp.Access == 1
                    && (mp.StartDate == null || mp.StartDate <= DateTime.Now)
                    && (mp.EndDate == null || mp.EndDate >= DateTime.Now));
            return hasIndividualGrant;
        }

        return false;
    }

    public async Task<bool> IsAppOpenAsync()
    {
        var permissionId = await GetStudentPermissionIdAsync();
        var roleId = await GetStudentRoleIdAsync();
        return await _rapsContext.TblRolePermissions
            .AnyAsync(rp => rp.RoleId == roleId
                && rp.PermissionId == permissionId
                && rp.Access == 1);
    }

    #region Completeness Calculation

    /// <summary>
    /// Student info completeness: 3 checks — (address+city+zip as 1), home phone, cell phone.
    /// </summary>
    public static int CalculateStudentInfoCompleteness(StudentContact contact)
    {
        int count = 0;
        if (!string.IsNullOrWhiteSpace(contact.Address)
            && !string.IsNullOrWhiteSpace(contact.City)
            && !string.IsNullOrWhiteSpace(contact.Zip))
        {
            count++;
        }
        if (!string.IsNullOrWhiteSpace(contact.HomePhone))
        {
            count++;
        }
        if (!string.IsNullOrWhiteSpace(contact.CellPhone))
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Contact completeness: 6 checks — name, relationship, work phone, home phone, cell phone, email.
    /// </summary>
    public static int CalculateContactCompleteness(StudentEmergencyContact? contact)
    {
        if (contact == null)
        {
            return 0;
        }

        int count = 0;
        if (!string.IsNullOrWhiteSpace(contact.Name)) count++;
        if (!string.IsNullOrWhiteSpace(contact.Relationship)) count++;
        if (!string.IsNullOrWhiteSpace(contact.WorkPhone)) count++;
        if (!string.IsNullOrWhiteSpace(contact.HomePhone)) count++;
        if (!string.IsNullOrWhiteSpace(contact.CellPhone)) count++;
        if (!string.IsNullOrWhiteSpace(contact.Email)) count++;
        return count;
    }

    #endregion

    #region RAPS Lookups

    private async Task<int> GetStudentPermissionIdAsync()
    {
        var permission = await _rapsContext.TblPermissions
            .FirstOrDefaultAsync(p => p.Permission == EmergencyContactPermissions.Student);
        return permission?.PermissionId
            ?? throw new InvalidOperationException(
                $"RAPS permission '{EmergencyContactPermissions.Student}' not found");
    }

    private async Task<int> GetStudentRoleIdAsync()
    {
        var role = await _rapsContext.TblRoles
            .FirstOrDefaultAsync(r => r.Role == EmergencyContactPermissions.StudentRoleName);
        return role?.RoleId
            ?? throw new InvalidOperationException(
                $"RAPS role '{EmergencyContactPermissions.StudentRoleName}' not found");
    }

    #endregion

    #region Private Helpers

    private async Task<bool> IsCurrentDvmStudentAsync(int personId)
    {
        var studentList = new StudentList(_viperContext);
        var student = await studentList.GetStudent(personId);
        return student != null;
    }

    private async Task<Dictionary<int, int>> GetPersonIdToPidmMapAsync(List<int> personIds)
    {
        // AaudUser.AaudUserId == Person.PersonId, and AaudUser.Pidm is the Banner PIDM (string)
        var users = await _aaudContext.AaudUsers
            .Where(u => EF.Parameter(personIds).Contains(u.AaudUserId) && u.Pidm != null)
            .Select(u => new { u.AaudUserId, u.Pidm })
            .ToListAsync();

        var map = new Dictionary<int, int>();
        foreach (var u in users)
        {
            if (int.TryParse(u.Pidm, out var pidm))
            {
                map[u.AaudUserId] = pidm;
            }
        }
        return map;
    }

    private async Task<int?> GetPidmForPersonIdAsync(int personId)
    {
        var pidmStr = await _aaudContext.AaudUsers
            .Where(u => u.AaudUserId == personId)
            .Select(u => u.Pidm)
            .FirstOrDefaultAsync();

        if (pidmStr != null && int.TryParse(pidmStr, out var pidm))
        {
            return pidm;
        }
        return null;
    }

    /// <summary>
    /// Clears the cached roles and permissions for all members of a given role,
    /// so that permission changes take effect immediately.
    /// </summary>
    private void ClearCacheForRoleMembers(int roleId)
    {
        var memberIds = _rapsContext.TblRoleMembers
            .Where(rm => rm.RoleId == roleId
                && (rm.StartDate == null || rm.StartDate <= DateTime.Now)
                && (rm.EndDate == null || rm.EndDate >= DateTime.Now))
            .Select(rm => rm.MemberId)
            .ToList();

        foreach (var memberId in memberIds)
        {
            _rapsCacheService.ClearCachedRolesAndPermissionsForUser(memberId);
        }
    }

    private static void ValidatePhone(string? value, string fieldName, List<string> invalidFields)
    {
        if (!PhoneHelper.IsValidPhone(value))
        {
            invalidFields.Add(fieldName);
        }
    }

    private void UpsertEmergencyContact(StudentContact contact, string type, ContactInfoDto dto)
    {
        var existing = contact.EmergencyContacts.FirstOrDefault(e => e.Type == type);
        if (existing != null)
        {
            EmergencyContactMapper.ApplyContactInfoToEntity(dto, existing);
        }
        else
        {
            var newContact = new StudentEmergencyContact { StdContactId = contact.StdContactId, Type = type };
            EmergencyContactMapper.ApplyContactInfoToEntity(dto, newContact);
            contact.EmergencyContacts.Add(newContact);
        }
    }

    #endregion
}
