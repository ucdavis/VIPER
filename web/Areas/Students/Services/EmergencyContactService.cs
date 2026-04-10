using Microsoft.EntityFrameworkCore;
using Viper.Areas.RAPS.Services;
using Viper.Areas.Students.Constants;
using Viper.Areas.Students.Models;
using Viper.Areas.Students.Models.Entities;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using Viper.Models.AAUD;

namespace Viper.Areas.Students.Services;

public class EmergencyContactService : IEmergencyContactService
{
    private readonly SISContext _sisContext;
    private readonly RAPSContext _rapsContext;
    private readonly AAUDContext _aaudContext;
    private readonly IUserHelper _userHelper;
    private readonly ILogger<EmergencyContactService> _logger;
    private readonly RAPSCacheService _rapsCacheService;
    private readonly RAPSAuditService _rapsAuditService;

    public EmergencyContactService(
        SISContext sisContext,
        RAPSContext rapsContext,
        AAUDContext aaudContext,
        IUserHelper userHelper,
        ILogger<EmergencyContactService> logger)
    {
        _sisContext = sisContext;
        _rapsContext = rapsContext;
        _aaudContext = aaudContext;
        _userHelper = userHelper;
        _logger = logger;
        _rapsCacheService = new RAPSCacheService(rapsContext, aaudContext, userHelper);
        _rapsAuditService = new RAPSAuditService(rapsContext, userHelper);
    }

    public async Task<List<StudentContactListItemDto>> GetStudentContactListAsync()
    {
        var (dvmStudents, mothraToPersonId, contactsByPidm) = await LoadDvmStudentsWithContactsAsync();

        var result = new List<StudentContactListItemDto>();
        foreach (var student in dvmStudents)
        {
            if (!mothraToPersonId.TryGetValue(student.IdsMothraId, out var personId))
            {
                _logger.LogWarning(
                    "DVM student MothraId {MothraId} has no AaudUser mapping — included with PersonId 0",
                    LogSanitizer.SanitizeId(student.IdsMothraId));
            }

            var item = new StudentContactListItemDto
            {
                PersonId = personId,
                RowKey = personId > 0 ? personId.ToString() : student.IdsMothraId,
                HasDetailRoute = personId > 0,
                // View already applies display name logic (display_last_name AS person_last_name)
                FullName = $"{student.PersonLastName}, {student.PersonFirstName}",
                ClassLevel = student.StudentsClassLevel ?? string.Empty,
                Email = FormatEmail(student.IdsMailid)
            };

            if (int.TryParse(student.IdsPidm, out var pidm)
                && contactsByPidm.TryGetValue(pidm, out var contact))
            {
                item.CellPhone = contact.CellPhone ?? contact.HomePhone;
                item.StudentInfoMissing = GetStudentInfoMissingFields(contact);
                item.StudentInfoComplete = StudentInfoFieldCount - item.StudentInfoMissing.Count;

                item.LocalContactMissing = GetContactMissingFields(
                    contact.EmergencyContacts.FirstOrDefault(e => e.Type == "local"));
                item.LocalContactComplete = ContactFieldCount - item.LocalContactMissing.Count;

                item.EmergencyContactMissing = GetContactMissingFields(
                    contact.EmergencyContacts.FirstOrDefault(e => e.Type == "emergency"));
                item.EmergencyContactComplete = ContactFieldCount - item.EmergencyContactMissing.Count;

                item.PermanentContactMissing = GetContactMissingFields(
                    contact.EmergencyContacts.FirstOrDefault(e => e.Type == "permanent"));
                item.PermanentContactComplete = ContactFieldCount - item.PermanentContactMissing.Count;

                item.LastUpdated = contact.LastUpdated;
            }

            result.Add(item);
        }

        return result;
    }

    public async Task<StudentContactDetailDto?> GetStudentContactDetailAsync(int personId, bool canEdit)
    {
        // Use AAUD view (consistent with list/report) instead of VIPER StudentList
        var studentInfo = await _aaudContext.AaudUsers
            .Where(u => u.AaudUserId == personId)
            .Join(_aaudContext.VwDvmStudentsMaxTerms,
                u => u.MothraId,
                s => s.IdsMothraId,
                (u, s) => new { PersonId = u.AaudUserId, s.PersonLastName, s.PersonFirstName, s.StudentsClassLevel })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (studentInfo == null)
        {
            return null;
        }

        var pidm = await GetCurrentDvmPidmAsync(personId);
        if (pidm == null)
        {
            // Student exists but has no PIDM mapping — return empty contact shell
            return new StudentContactDetailDto
            {
                PersonId = studentInfo.PersonId,
                FullName = $"{studentInfo.PersonLastName}, {studentInfo.PersonFirstName}",
                ClassLevel = studentInfo.StudentsClassLevel ?? string.Empty,
                CanEdit = canEdit
            };
        }

        var contact = await _sisContext.StudentContacts
            .Include(c => c.EmergencyContacts)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Pidm == pidm.Value);

        var dto = new StudentContactDetailDto
        {
            PersonId = studentInfo.PersonId,
            FullName = $"{studentInfo.PersonLastName}, {studentInfo.PersonFirstName}",
            ClassLevel = studentInfo.StudentsClassLevel ?? string.Empty,
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

        var pidm = await GetCurrentDvmPidmAsync(personId);
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
        var (dvmStudents, mothraToPersonId, contactsByPidm) = await LoadDvmStudentsWithContactsAsync();

        var result = new List<StudentContactReportDto>();
        foreach (var student in dvmStudents)
        {
            if (!mothraToPersonId.TryGetValue(student.IdsMothraId, out var personId))
            {
                _logger.LogWarning(
                    "DVM student MothraId {MothraId} has no AaudUser mapping — included with PersonId 0",
                    LogSanitizer.SanitizeId(student.IdsMothraId));
            }

            var dto = new StudentContactReportDto
            {
                PersonId = personId,
                RowKey = personId > 0 ? personId.ToString() : student.IdsMothraId,
                FullName = $"{student.PersonLastName}, {student.PersonFirstName}",
                ClassLevel = student.StudentsClassLevel ?? string.Empty
            };

            if (int.TryParse(student.IdsPidm, out var pidm)
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

        // Get individual member permissions — legacy grants have null dates (created via
        // usp_createPermissionForMemberNoDates), but filter by date window defensively
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
        var currentLoginId = _userHelper.GetCurrentUser()?.LoginId;

        // Find existing role-permission row (keep it permanently, just flip Access)
        var rolePermission = await _rapsContext.TblRolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId
                && rp.PermissionId == permissionId);

        if (rolePermission == null)
        {
            // Ensure role has the permission — create with Access = 1 (open)
            rolePermission = new Viper.Models.RAPS.TblRolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                Access = 1,
                ModTime = DateTime.Now,
                ModBy = currentLoginId
            };
            _rapsContext.TblRolePermissions.Add(rolePermission);
            _rapsAuditService.AuditRolePermissionChange(rolePermission, RAPSAuditService.AuditActionType.Create);
            await _rapsContext.SaveChangesAsync();
            ClearCacheForRoleMembers(roleId);
            return true;
        }

        // Toggle Access between 1 (open) and 0 (closed)
        var nowOpen = rolePermission.Access != 1;
        rolePermission.Access = (byte)(nowOpen ? 1 : 0);
        rolePermission.ModTime = DateTime.Now;
        rolePermission.ModBy = currentLoginId;
        _rapsAuditService.AuditRolePermissionChange(rolePermission, RAPSAuditService.AuditActionType.Update);
        await _rapsContext.SaveChangesAsync();
        ClearCacheForRoleMembers(roleId);
        return nowOpen;
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

        var currentLoginId = _userHelper.GetCurrentUser()?.LoginId;
        if (existing.Count > 0)
        {
            // Remove all matching individual grants (handles duplicates)
            foreach (var mp in existing)
            {
                _rapsAuditService.AuditPermissionMemberChange(mp, RAPSAuditService.AuditActionType.Delete);
            }
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
                ModTime = DateTime.Now,
                ModBy = currentLoginId
            };
            _rapsContext.TblMemberPermissions.Add(memberPermission);
            _rapsAuditService.AuditPermissionMemberChange(memberPermission, RAPSAuditService.AuditActionType.Create);
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
    /// Returns labels of missing student info fields.
    /// Checks: address group (address+city+zip), phone (home OR cell).
    /// Total = 2.
    /// </summary>
    public static List<string> GetStudentInfoMissingFields(StudentContact contact)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(contact.Address)
            || string.IsNullOrWhiteSpace(contact.City)
            || string.IsNullOrWhiteSpace(contact.Zip))
        {
            missing.Add("Address");
        }
        if (string.IsNullOrWhiteSpace(contact.HomePhone)
            && string.IsNullOrWhiteSpace(contact.CellPhone))
        {
            missing.Add("Phone");
        }
        return missing;
    }

    /// <summary>
    /// Returns labels of missing contact fields.
    /// Checks: name, relationship, phone (work OR home OR cell), email.
    /// Total = 4.
    /// </summary>
    public static List<string> GetContactMissingFields(StudentEmergencyContact? contact)
    {
        if (contact == null)
        {
            return ["Name", "Relationship", "Phone", "Email"];
        }

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(contact.Name)) missing.Add("Name");
        if (string.IsNullOrWhiteSpace(contact.Relationship)) missing.Add("Relationship");
        if (string.IsNullOrWhiteSpace(contact.WorkPhone)
            && string.IsNullOrWhiteSpace(contact.HomePhone)
            && string.IsNullOrWhiteSpace(contact.CellPhone))
        {
            missing.Add("Phone");
        }
        if (string.IsNullOrWhiteSpace(contact.Email)) missing.Add("Email");
        return missing;
    }

    public const int StudentInfoFieldCount = 2;
    public const int ContactFieldCount = 4;

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

    /// <summary>
    /// Loads DVM students from the AAUD view together with their contact data,
    /// used by both the list and report endpoints.
    /// </summary>
    private async Task<(List<VwDvmStudentsMaxTerm> DvmStudents, Dictionary<string, int> MothraToPersonId, Dictionary<int, StudentContact> ContactsByPidm)>
        LoadDvmStudentsWithContactsAsync()
    {
        var dvmStudents = await _aaudContext.VwDvmStudentsMaxTerms
            .AsNoTracking()
            .ToListAsync();

        var mothraIds = dvmStudents.Select(s => s.IdsMothraId).ToList();
        var mothraToPersonId = await _aaudContext.AaudUsers
            .Where(u => EF.Parameter(mothraIds).Contains(u.MothraId))
            .Select(u => new { u.MothraId, u.AaudUserId })
            .AsNoTracking()
            .ToDictionaryAsync(u => u.MothraId, u => u.AaudUserId);

        var pidms = dvmStudents
            .Select(s => int.TryParse(s.IdsPidm, out var p) ? p : 0)
            .Where(p => p > 0)
            .ToList();

        var contacts = await _sisContext.StudentContacts
            .Include(c => c.EmergencyContacts)
            .Where(c => EF.Parameter(pidms).Contains(c.Pidm))
            .AsNoTracking()
            .ToListAsync();

        var contactsByPidm = contacts.ToDictionary(c => c.Pidm);

        return (dvmStudents, mothraToPersonId, contactsByPidm);
    }

    private async Task<bool> IsCurrentDvmStudentAsync(int personId)
    {
        // Use AAUD view (consistent with list/report) instead of VIPER StudentList
        return await _aaudContext.AaudUsers
            .Where(u => u.AaudUserId == personId)
            .Join(_aaudContext.VwDvmStudentsMaxTerms,
                u => u.MothraId,
                s => s.IdsMothraId,
                (u, s) => u.AaudUserId)
            .AnyAsync();
    }

    /// <summary>
    /// Resolves PIDM for a person through the DVM students view, consistent with
    /// the list/report paths that read IdsPidm from VwDvmStudentsMaxTerms.
    /// </summary>
    private async Task<int?> GetCurrentDvmPidmAsync(int personId)
    {
        var pidmStr = await _aaudContext.AaudUsers
            .Where(u => u.AaudUserId == personId)
            .Join(_aaudContext.VwDvmStudentsMaxTerms,
                u => u.MothraId,
                s => s.IdsMothraId,
                (_, s) => s.IdsPidm)
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

    private static string FormatEmail(string? mailId)
    {
        if (string.IsNullOrEmpty(mailId))
        {
            return string.Empty;
        }

        return mailId.Contains('@') ? mailId : $"{mailId}@ucdavis.edu";
    }

    #endregion
}
