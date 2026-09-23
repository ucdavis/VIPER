using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Students.Constants;
using Viper.Areas.Students.Models;
using Viper.Areas.Students.Models.Entities;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using Viper.Models.AAUD;

namespace Viper.Areas.Students.Services;

public class CareerSelectionService : ICareerSelectionService
{
    private readonly RAPSContext _rapsContext;
    private readonly AAUDContext _aaudContext;
    private readonly IUserHelper _userHelper;
    private readonly VIPERContext _viperContext;
    private readonly ILogger<CareerSelectionService> _logger;
    private readonly IStudentAppAccessService _appAccessService;
    private readonly IDvmStudentLookupService _dvmStudentLookup;

    public CareerSelectionService(
        RAPSContext rapsContext,
        AAUDContext aaudContext,
        IUserHelper userHelper,
        VIPERContext viperContext,
        ILogger<CareerSelectionService> logger,
        IStudentAppAccessService appAccessService,
        IDvmStudentLookupService dvmStudentLookup)
    {
        _rapsContext = rapsContext;
        _aaudContext = aaudContext;
        _userHelper = userHelper;
        _viperContext = viperContext;
        _logger = logger;
        _appAccessService = appAccessService;
        _dvmStudentLookup = dvmStudentLookup;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The join to AaudUser supplies the ids a career selection is saved and shown with.
    /// </remarks>
    public async Task<List<MentorOptionDto>> SearchMentorsAsync(string? search, CancellationToken ct = default)
    {
        var normalizedSearch = PersonSearchHelper.Normalize(search);
        if (normalizedSearch == null)
        {
            return [];
        }

        var namePredicate = PersonSearchHelper
            .NameMatches<VwCurrentAffiliate>(a => a.LastName, a => a.FirstName, normalizedSearch)
            .Or(a => a.IdsLoginid != null && a.IdsLoginid.Contains(normalizedSearch))
            .Or(a => a.IdsMailid != null && a.IdsMailid.Contains(normalizedSearch));

        // The view carries a row per affiliation, so the same person can match more than once.
        // Reducing to the identity before the join keeps duplicates out of the picker.
        var affiliates = _aaudContext.VwCurrentAffiliates
            .AsNoTracking()
            .Where(a => a.IdsMothraid != null)
            .Where(namePredicate)
            .Select(a => a.IdsMothraid)
            .Distinct();

        return await affiliates
            .Join(_aaudContext.AaudUsers.Where(u => u.IamId != null),
                mothraId => mothraId,
                u => u.MothraId,
                (_, u) => new MentorOptionDto
                {
                    PersonId = u.AaudUserId,
                    IamId = u.IamId!,
                    FullName = u.DisplayLastName + ", " + u.DisplayFirstName,
                    LoginId = u.LoginId,
                    MailId = u.MailId
                })
            // Ordered after the join so the cap keeps the alphabetically first matches rather
            // than whichever rows the join happened to produce.
            .OrderBy(m => m.FullName)
            .Take(PersonSearchHelper.MaxResults)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public CareerSelectionScope ResolveScope(AaudUser currentUser)
    {
        if (_userHelper.HasPermission(_rapsContext, currentUser, CareerSelectionPermissions.Admin)
            || _userHelper.HasPermission(_rapsContext, currentUser, CareerSelectionPermissions.ReadOnly))
        {
            return CareerSelectionScope.All;
        }

        if (_userHelper.HasPermission(_rapsContext, currentUser, CareerSelectionPermissions.Faculty))
        {
            return CareerSelectionScope.Mentored;
        }

        if (_userHelper.HasPermission(_rapsContext, currentUser, CareerSelectionPermissions.Student)
            || _userHelper.HasPermission(_rapsContext, currentUser, CareerSelectionPermissions.ViewOwn))
        {
            return CareerSelectionScope.Own;
        }

        return CareerSelectionScope.None;
    }

    /// <inheritdoc/>
    public async Task<bool> IsMentorOfAsync(string mentorMothraId, int studentPersonId)
    {
        if (string.IsNullOrWhiteSpace(mentorMothraId))
        {
            return false;
        }

        var pidm = await _dvmStudentLookup.GetCurrentDvmPidmAsync(studentPersonId);
        if (pidm == null)
        {
            return false;
        }

        // Unwrap before the query: the closure then captures a plain int, so the null check above
        // does not have to survive into the expression tree.
        var studentPidm = pidm.Value;
        return await _viperContext.CareerSelections
            .AsNoTracking()
            .AnyAsync(c => c.Pidm == studentPidm && c.FacultyMothraId == mentorMothraId);
    }

    /// <inheritdoc/>
    /// <param name="access">
    /// Which students to return. Only <see cref="StudentListAccess.AllStudents"/> is unrestricted,
    /// and it has to be asked for by name, so a caller that cannot establish its own scope gets
    /// an empty roster rather than everyone's.
    /// </param>
    public async Task<List<StudentCareerListItemDto>> GetStudentCareerListAsync(StudentListAccess access)
    {
        var (dvmStudents, mothraToPersonId, careerSelectionByPidm) = await LoadDvmStudentsWithCareerDataAsync(access);
        var mentorsByMothraId = await LoadMentorsByMothraIdAsync(careerSelectionByPidm.Values);
        var result = new List<StudentCareerListItemDto>();
        foreach (var student in dvmStudents)
        {
            var item = CreateRow<StudentCareerListItemDto>(student, mothraToPersonId);

            if (TryGetCareerSelection(student, careerSelectionByPidm, out var careerSelection))
            {
                item.DirectionCompleted = IsSelectionComplete(
                    careerSelection.CareerOption, careerSelection.CareerOther);
                item.PrimaryFocusCompleted = IsSelectionComplete(
                    careerSelection.FirstSpeciesOption, careerSelection.FirstSpeciesOther);
                item.SecondaryFocusCompleted = IsSelectionComplete(
                    careerSelection.SecondSpeciesOption, careerSelection.SecondSpeciesOther);
                // Post-graduation plans have no free-text field of their own; the form directs
                // students to describe an "Other" choice in their short term plans instead.
                item.PostGradCompleted = IsSelectionComplete(
                    careerSelection.PostGradOption, careerSelection.ShortTermStatement);
                item.ShortTermPlansCompleted = !String.IsNullOrWhiteSpace(careerSelection.ShortTermStatement);
                item.LongTermPlansCompleted = !String.IsNullOrWhiteSpace(careerSelection.LongTermStatement);

                if (careerSelection.FacultyMothraId != null
                    && mentorsByMothraId.TryGetValue(careerSelection.FacultyMothraId, out var mentor))
                {
                    item.MentorName = mentor.FullName;
                }

                item.LastUpdated = careerSelection.DateModified ?? careerSelection.DateAdded;
            }

            result.Add(item);
        }

        return result;
    }

    public async Task<StudentCareerDetailDto?> GetStudentCareerDetailAsync(int personId, bool canEdit, bool canViewStudentList)
    {
        var student = await _dvmStudentLookup.GetDvmStudentAsync(personId);
        if (student == null)
        {
            return null;
        }

        var dto = new StudentCareerDetailDto
        {
            PersonId = student.PersonId,
            FullName = student.FullName,
            ClassLevel = student.ClassLevel,
            CanEdit = canEdit,
            CanViewStudentList = canViewStudentList
        };

        var pidm = student.Pidm;
        if (pidm == null)
        {
            // Student exists but has no PIDM mapping — return empty career selection shell
            return dto;
        }

        var studentPidm = pidm.Value;
        var career = await _viperContext.CareerSelections
            .Include(c => c.CareerOption)
            .Include(c => c.FirstSpeciesOption)
            .Include(c => c.SecondSpeciesOption)
            .Include(c => c.PostGradOption)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Pidm == studentPidm);

        if (career != null)
        {
            dto.StudentInfo.Direction = ToDropdownOption(career.CareerOption);
            dto.StudentInfo.DirectionOther = career.CareerOther;
            dto.StudentInfo.PrimaryFocus = ToDropdownOption(career.FirstSpeciesOption);
            dto.StudentInfo.PrimaryFocusOther = career.FirstSpeciesOther;
            dto.StudentInfo.SecondaryFocus = ToDropdownOption(career.SecondSpeciesOption);
            dto.StudentInfo.SecondaryFocusOther = career.SecondSpeciesOther;
            dto.StudentInfo.PostGrad = ToDropdownOption(career.PostGradOption);
            dto.StudentInfo.ShortTermPlans = career.ShortTermStatement;
            dto.StudentInfo.LongTermPlans = career.LongTermStatement;
            dto.LastUpdated = career.DateModified ?? career.DateAdded;

            // The mentor is stored as a MothraId; the client works in PersonIds.
            if (!string.IsNullOrWhiteSpace(career.FacultyMothraId))
            {
                var mentor = await _aaudContext.AaudUsers
                    .Where(u => u.MothraId == career.FacultyMothraId)
                    // "Last, First" rather than the stored DisplayFullName, so the mentor reads
                    // the same here, in the picker, and in the list and report.
                    .Select(u => new { u.AaudUserId, u.IamId, FullName = u.DisplayLastName + ", " + u.DisplayFirstName })
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
                if (mentor != null)
                {
                    dto.StudentInfo.MentorId = mentor.AaudUserId;
                    dto.StudentInfo.MentorName = mentor.FullName;
                    dto.StudentInfo.MentorIamId = mentor.IamId;
                }
            }
        }

        return dto;
    }

    /// <inheritdoc/>
    public async Task<List<string>> UpdateStudentCareerSelectionAsync(int personId, StudentCareerInfoDto request, bool isAdmin)
    {
        if (!await _dvmStudentLookup.IsCurrentDvmStudentAsync(personId))
        {
            throw new InvalidOperationException($"PersonId {personId} is not a current DVM student");
        }

        var pidm = await _dvmStudentLookup.GetCurrentDvmPidmAsync(personId);
        if (pidm == null)
        {
            throw new InvalidOperationException($"No PIDM found for PersonId {personId}");
        }

        var studentPidm = pidm.Value;

        // A cleared dropdown can arrive as a placeholder option rather than an absent one.
        // Drop the whole option.
        request.Direction = NormalizeSelection(request.Direction);
        request.PrimaryFocus = NormalizeSelection(request.PrimaryFocus);
        request.SecondaryFocus = NormalizeSelection(request.SecondaryFocus);
        request.PostGrad = NormalizeSelection(request.PostGrad);
        if (request.MentorId <= 0)
        {
            request.MentorId = null;
        }

        var errors = await ValidateCareerSelectionAsync(request, isAdmin);
        if (errors.Count > 0)
        {
            return errors;
        }

        // The mentor is admin-managed and read-only to the student, so a student's own save
        // must leave whatever is already stored untouched.
        var mentorMothraId = isAdmin ? await ResolveMentorMothraIdAsync(request.MentorId) : null;

        void ApplyTo(CareerSelection target)
        {
            CareerSelectionMapper.ApplyStudentInfoToEntity(request, target);
            if (isAdmin)
            {
                target.FacultyMothraId = mentorMothraId;
            }
            target.DateModified = DateTime.Now;
        }

        var careerSelection = await _viperContext.CareerSelections
            .FirstOrDefaultAsync(c => c.Pidm == studentPidm);

        var isNew = careerSelection == null;
        if (careerSelection == null)
        {
            careerSelection = new CareerSelection
            {
                Pidm = studentPidm,
                DateAdded = DateTime.Now
            };
            _viperContext.CareerSelections.Add(careerSelection);
        }

        ApplyTo(careerSelection);

        try
        {
            await _viperContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (isNew && ex.IsUniqueKeyViolation())
        {
            // Another save created this student's row between the read above and this insert,
            // and the unique index on Pidm refused the second one. Fold the answers into the row
            // that won rather than failing a save the student has no way to retry differently.
            // Any other rejection (a deleted option, an over-long value) propagates to the caller.
            _viperContext.Entry(careerSelection).State = EntityState.Detached;

            var stored = await _viperContext.CareerSelections
                .FirstOrDefaultAsync(c => c.Pidm == studentPidm);
            if (stored == null)
            {
                // Rejected for some other reason, so it is not ours to absorb.
                throw;
            }

            ApplyTo(stored);
            await _viperContext.SaveChangesAsync();
        }

        return errors;
    }

    /// <summary>
    /// Reduces a submitted dropdown to nothing selected, or a real option id. Every choice a
    /// student can make is a stored row now, "Other" included, so anything without an id is a
    /// placeholder left behind by a cleared field.
    /// </summary>
    private static CareerDropdownOption? NormalizeSelection(CareerDropdownOption? option)
        => option?.Value > 0 ? option : null;

    /// <summary>
    /// A choice counts as answered unless it is the catch-all, which also needs its free text.
    /// Mirrors isSelectionComplete in CareerSelectionForm.vue, which drives the form's missing
    /// fields warning.
    /// </summary>
    private static bool IsSelectionComplete(ICareerSelectionOption? option, string? otherText)
        => option != null && (!option.IsOther || !String.IsNullOrWhiteSpace(otherText));

    /// <summary>
    /// Checks the submitted option ids against their lookup tables so a bad id is reported as
    /// a validation error rather than reaching the database as a foreign key failure.
    /// </summary>
    private async Task<List<string>> ValidateCareerSelectionAsync(StudentCareerInfoDto request, bool isAdmin)
    {
        var errors = new List<string>();

        await ValidateSelectionAsync(request.Direction, CareerOptionType.Career,
            "The selected career direction is not a valid option.", errors);
        await ValidateSelectionAsync(request.PrimaryFocus, CareerOptionType.Species,
            "The selected primary species focus is not a valid option.", errors);
        await ValidateSelectionAsync(request.SecondaryFocus, CareerOptionType.Species,
            "The selected secondary species focus is not a valid option.", errors);
        await ValidateSelectionAsync(request.PostGrad, CareerOptionType.PostGrad,
            "The selected post-graduation plan is not a valid option.", errors);
        var mentorId = request.MentorId;
        if (isAdmin && mentorId != null
            && !await IsCurrentAffiliateAsync(mentorId.Value))
        {
            errors.Add("The selected mentor is not a current SVM affiliate.");
        }

        return errors;
    }

    /// <summary>
    /// Checks one submitted dropdown against its option table, reporting an unknown id as an error.
    /// The lookup doubles as the source of truth for IsOther: the submitted flag decides whether
    /// the free text is kept, so it is re-read from the stored row rather than taken on the
    /// client's word.
    /// </summary>
    private async Task ValidateSelectionAsync(CareerDropdownOption? selection, CareerOptionType type,
        string invalidMessage, List<string> errors)
    {
        if (selection == null)
        {
            return;
        }

        var selectedId = selection.Value;
        if (selectedId == null)
        {
            return;
        }

        var isOther = await LookupIsOtherAsync(type, selectedId.Value);
        if (isOther == null)
        {
            errors.Add(invalidMessage);
        }
        else
        {
            selection.IsOther = isOther.Value;
        }
    }

    /// <summary>
    /// Reads IsOther for one option, or null if no such option exists. Keyed on each table's own
    /// id column, since EF cannot translate <see cref="ICareerSelectionOption.Id"/> into a query.
    /// </summary>
    private Task<bool?> LookupIsOtherAsync(CareerOptionType type, int id) => type switch
    {
        CareerOptionType.Career => _viperContext.CareerOptions
            .Where(o => o.CareerOptionId == id)
            .Select(o => (bool?)o.IsOther)
            .FirstOrDefaultAsync(),
        CareerOptionType.Species => _viperContext.SpeciesOptions
            .Where(o => o.SpeciesOptionId == id)
            .Select(o => (bool?)o.IsOther)
            .FirstOrDefaultAsync(),
        CareerOptionType.PostGrad => _viperContext.PostGradOptions
            .Where(o => o.PostGradOptionId == id)
            .Select(o => (bool?)o.IsOther)
            .FirstOrDefaultAsync(),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown career option type."),
    };

    /// <summary>
    /// A stored option as the form's dropdown value, or null when nothing is selected.
    /// </summary>
    private static CareerDropdownOption? ToDropdownOption(ICareerSelectionOption? option)
        => option == null ? null : new CareerDropdownOption { Label = option.Label, Value = option.Id, IsOther = option.IsOther };

    /// <summary>
    /// Resolves a mentor PersonId to the MothraId the table stores. A null id clears the mentor.
    /// </summary>
    private async Task<string?> ResolveMentorMothraIdAsync(int? mentorId)
    {
        if (mentorId == null)
        {
            return null;
        }

        return await _aaudContext.AaudUsers
            .Where(u => u.AaudUserId == mentorId)
            .Select(u => u.MothraId)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    /// <param name="access">
    /// Which students to report on, on the same terms as
    /// <see cref="GetStudentCareerListAsync"/>.
    /// </param>
    public async Task<List<StudentCareerReportDto>> GetStudentCareerReportAsync(StudentListAccess access)
    {
        var (dvmStudents, mothraToPersonId, careerSelectionByPidm) = await LoadDvmStudentsWithCareerDataAsync(access);
        var mentorsByMothraId = await LoadMentorsByMothraIdAsync(careerSelectionByPidm.Values);

        var result = new List<StudentCareerReportDto>();
        foreach (var student in dvmStudents)
        {
            var dto = CreateRow<StudentCareerReportDto>(student, mothraToPersonId);

            if (TryGetCareerSelection(student, careerSelectionByPidm, out var career))
            {
                if (career.CareerOption != null)
                {
                    dto.Direction = career.CareerOption.IsOther ? career.CareerOther : career.CareerOption.Career;
                }
                if (career.FirstSpeciesOption != null)
                {
                    dto.PrimaryFocus = career.FirstSpeciesOption.IsOther ? career.FirstSpeciesOther : career.FirstSpeciesOption.Species;
                }
                if (career.SecondSpeciesOption != null)
                {
                    dto.SecondaryFocus = career.SecondSpeciesOption.IsOther ? career.SecondSpeciesOther : career.SecondSpeciesOption.Species;
                }
                if (career.PostGradOption != null)
                {
                    // No Other field; display "Other" instead.
                    dto.PostGrad = career.PostGradOption.PostGrad;
                }
                dto.ShortTermPlans = career.ShortTermStatement;
                dto.LongTermPlans = career.LongTermStatement;

                if (career.FacultyMothraId != null
                    && mentorsByMothraId.TryGetValue(career.FacultyMothraId, out var mentor))
                {
                    dto.MentorName = mentor.FullName;
                }

                dto.LastUpdated = career.DateModified ?? career.DateAdded;
            }

            result.Add(dto);
        }

        return result;
    }

    public Task<bool> ToggleAppAccessAsync() =>
        _appAccessService.ToggleAppAccessAsync(CareerSelectionPermissions.Student);

    /// <inheritdoc/>
    public bool CanEdit(int personId, AaudUser currentUser)
    {
        // Admin can always edit
        if (_userHelper.HasPermission(_rapsContext, currentUser, CareerSelectionPermissions.Admin))
        {
            return true;
        }

        // ReadOnly users cannot edit (view only)
        if (_userHelper.HasPermission(_rapsContext, currentUser, CareerSelectionPermissions.ReadOnly))
        {
            return false;
        }

        // Students can edit their own record if they have the Student permission, signaling
        // that the app is open for student self-service. AaudUserId is PersonId.
        return personId == currentUser.AaudUserId && _userHelper.HasPermission(_rapsContext, currentUser, CareerSelectionPermissions.Student);
    }

    public Task<bool> IsAppOpenAsync() =>
        _appAccessService.IsAppOpenAsync(CareerSelectionPermissions.Student);

    #region Private Helpers

    /// <summary>
    /// Loads DVM students from the AAUD view together with their career data,
    /// used by both the list and report endpoints.
    /// </summary>
    private async Task<(List<VwDvmStudentsMaxTerm> DvmStudents, Dictionary<string, int> MothraToPersonId, Dictionary<int, CareerSelection> careerSelectionByPidm)>
        LoadDvmStudentsWithCareerDataAsync(StudentListAccess access)
    {
        if (access.IsDenied)
        {
            // Every endpoint refuses a denied scope before calling in, so this
            // only fires if a future one forgets. Returning nothing keeps that mistake harmless.
            return ([], [], []);
        }

        var (dvmStudents, mothraToPersonId) = await _dvmStudentLookup.LoadDvmStudentsAsync();

        // The view has no inherent order, so without this the roster, report and exports could
        // change order between loads. Case-insensitive ordinal matches the grid's own name sort,
        // and MothraId keeps two students with the same name in a fixed order.
        dvmStudents = dvmStudents
            .OrderBy(s => s.PersonLastName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.PersonFirstName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.IdsMothraId, StringComparer.Ordinal)
            .ToList();

        // The view contains PIDM as a string and the table keys on an int, so parse once here
        // rather than at each lookup below; a student whose PIDM will not parse simply has no
        // career selection to match.
        var pidms = dvmStudents
            .Select(s => DvmStudentLookupService.ParsePidm(s.IdsPidm))
            .Where(p => p.HasValue)
            .Select(p => p!.Value)
            .ToList();

        var careerSelectionQuery = _viperContext.CareerSelections
            .Include(c => c.CareerOption)
            .Include(c => c.FirstSpeciesOption)
            .Include(c => c.SecondSpeciesOption)
            .Include(c => c.PostGradOption)
            .Where(c => EF.Parameter(pidms).Contains(c.Pidm));

        var isMentorView = access.TryGetMentor(out var mentorMothraId);
        if (isMentorView)
        {
            careerSelectionQuery = careerSelectionQuery.Where(c => c.FacultyMothraId == mentorMothraId);
        }

        var careerSelections = await careerSelectionQuery
            .AsNoTracking()
            .ToListAsync();

        var careerSelectionByPidm = careerSelections.ToDictionary(c => c.Pidm);

        if (isMentorView)
        {
            // The mentor is recorded on the career selection, so narrowing the selections above
            // has already identified the mentees; drop every other student from the roster rather
            // than listing them with empty career data.
            dvmStudents = dvmStudents
                .Where(s => DvmStudentLookupService.TryParsePidm(s.IdsPidm, out var p)
                    && careerSelectionByPidm.ContainsKey(p))
                .ToList();
        }

        return (dvmStudents, mothraToPersonId, careerSelectionByPidm);
    }

    /// <summary>
    /// Resolves the mentors referenced by a set of career selections in one query, keyed by the
    /// MothraId the career selection stores. Names are read through rather than copied, so a
    /// mentor who changes their name reads correctly everywhere without a data fix.
    /// </summary>
    private async Task<Dictionary<string, MentorIdentity>> LoadMentorsByMothraIdAsync(
        IEnumerable<CareerSelection> careerSelections)
    {
        var mentorMothraIds = careerSelections
            .Select(c => c.FacultyMothraId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct()
            .ToList();

        if (mentorMothraIds.Count == 0)
        {
            return [];
        }

        return await _aaudContext.AaudUsers
            .Where(u => EF.Parameter(mentorMothraIds).Contains(u.MothraId))
            .Select(u => new MentorIdentity(
                u.MothraId,
                u.DisplayLastName + ", " + u.DisplayFirstName))
            .AsNoTracking()
            .ToDictionaryAsync(m => m.MothraId);
    }

    private sealed record MentorIdentity(string MothraId, string FullName);

    /// <summary>
    /// Starts a roster row with the student's identity columns. A student with no AaudUser
    /// mapping is still listed, with PersonId 0 and no detail route.
    /// </summary>
    private T CreateRow<T>(VwDvmStudentsMaxTerm student, Dictionary<string, int> mothraToPersonId)
        where T : StudentCareerRowDto, new()
    {
        if (!mothraToPersonId.TryGetValue(student.IdsMothraId, out var personId))
        {
            _logger.LogWarning(
                "DVM student MothraId {MothraId} has no AaudUser mapping — included with PersonId 0",
                LogSanitizer.SanitizeId(student.IdsMothraId));
        }

        return new T
        {
            PersonId = personId,
            RowKey = personId > 0 ? personId.ToString() : student.IdsMothraId,
            HasDetailRoute = personId > 0,
            FullName = $"{student.PersonLastName}, {student.PersonFirstName}",
            ClassLevel = student.StudentsClassLevel ?? string.Empty,
            Email = DvmStudentLookupService.FormatEmail(student.IdsMailid)
        };
    }

    private static bool TryGetCareerSelection(VwDvmStudentsMaxTerm student,
        Dictionary<int, CareerSelection> careerSelectionByPidm,
        [NotNullWhen(true)] out CareerSelection? careerSelection)
    {
        careerSelection = null;
        return DvmStudentLookupService.TryParsePidm(student.IdsPidm, out var pidm)
            && careerSelectionByPidm.TryGetValue(pidm, out careerSelection);
    }

    /// <summary>
    /// Only current SVM affiliates may be recorded as a mentor. The picker offers nobody else, so
    /// a PersonId outside that set reached the API from something other than the form.
    /// If a current mentor becomes a former affiliate, they are no longer selectable as a mentor.
    /// Student edits are unaffected, and admin edits will need to replace the old mentor.
    /// </summary>
    private async Task<bool> IsCurrentAffiliateAsync(int personId)
    {
        return await _aaudContext.AaudUsers
            .Where(u => u.AaudUserId == personId)
            .Join(_aaudContext.VwCurrentAffiliates,
                u => u.MothraId,
                a => a.IdsMothraid,
                (u, _) => u.AaudUserId)
            .AnyAsync();
    }

    #endregion
}
