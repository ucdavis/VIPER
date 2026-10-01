using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.Areas.Students.Services;

/// <summary>
/// Looks up current DVM students through the AAUD students view, the one source for who counts
/// as a current student across the student self-service apps.
/// </summary>
public class DvmStudentLookupService : IDvmStudentLookupService
{
    private readonly AAUDContext _aaudContext;

    public DvmStudentLookupService(AAUDContext aaudContext)
    {
        _aaudContext = aaudContext;
    }

    /// <summary>
    /// Loads every current DVM student, with a map from MothraId to PersonId. A student with no
    /// AaudUser row is absent from the map.
    /// </summary>
    public async Task<(List<VwDvmStudentsMaxTerm> DvmStudents, Dictionary<string, int> MothraToPersonId)> LoadDvmStudentsAsync()
    {
        var dvmStudents = await _aaudContext.VwDvmStudentsMaxTerms
            .AsNoTracking()
            .ToListAsync();

        // Built from the rows already loaded rather than a second pass over the view. Distinct only
        // keeps the parameter list short if a student appears twice in the view.
        var mothraIds = dvmStudents
            .Select(s => s.IdsMothraId)
            .Distinct()
            .ToList();
        var users = await LoadAaudUsersByMothraIdAsync(mothraIds);
        var mothraToPersonId = users.ToDictionary(u => u.Key, u => u.Value.PersonId);

        return (dvmStudents, mothraToPersonId);
    }

    /// <summary>
    /// One AaudUser per MothraId, for the MothraIds given. A MothraId with no AaudUser row is
    /// absent from the map.
    /// </summary>
    public async Task<Dictionary<string, AaudPersonIdentity>> LoadAaudUsersByMothraIdAsync(List<string> mothraIds)
    {
        if (mothraIds.Count == 0)
        {
            return [];
        }

        var users = await _aaudContext.AaudUsers
            .Where(u => EF.Parameter(mothraIds).Contains(u.MothraId))
            // "Last, First" rather than the stored DisplayFullName, so a person reads the same in
            // the roster, the report and the mentor picker.
            .Select(u => new { u.MothraId, u.AaudUserId, u.Current, FullName = u.DisplayLastName + ", " + u.DisplayFirstName })
            .AsNoTracking()
            .ToListAsync();

        // The MothraId index on aaudUser is not unique, so one person can have more than one row.
        // Prefer the current row, then the lowest id, so every load picks the same one.
        return users
            .GroupBy(u => u.MothraId)
            .Select(g => g.OrderByDescending(u => u.Current).ThenBy(u => u.AaudUserId).First())
            .ToDictionary(u => u.MothraId, u => new AaudPersonIdentity(u.AaudUserId, u.FullName));
    }

    /// <summary>
    /// The name, class level and PIDM of one current DVM student, or null if the person is not one.
    /// </summary>
    public async Task<DvmStudentIdentity?> GetDvmStudentAsync(int personId)
    {
        var student = await _aaudContext.AaudUsers
            .Where(u => u.AaudUserId == personId)
            .Join(_aaudContext.VwDvmStudentsMaxTerms,
                u => u.MothraId,
                s => s.IdsMothraId,
                (u, s) => new { PersonId = u.AaudUserId, s.PersonLastName, s.PersonFirstName, s.StudentsClassLevel, s.IdsPidm })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (student == null)
        {
            return null;
        }

        return new DvmStudentIdentity(
            student.PersonId,
            $"{student.PersonLastName}, {student.PersonFirstName}",
            student.StudentsClassLevel ?? string.Empty,
            ParsePidm(student.IdsPidm));
    }

    public async Task<bool> IsCurrentDvmStudentAsync(int personId)
    {
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
    public async Task<int?> GetCurrentDvmPidmAsync(int personId)
    {
        var pidmStr = await _aaudContext.AaudUsers
            .Where(u => u.AaudUserId == personId)
            .Join(_aaudContext.VwDvmStudentsMaxTerms,
                u => u.MothraId,
                s => s.IdsMothraId,
                (_, s) => s.IdsPidm)
            .FirstOrDefaultAsync();

        return ParsePidm(pidmStr);
    }

    /// <summary>
    /// The view holds a bare mail ID for most students; an address that already has a domain is
    /// kept as it is.
    /// </summary>
    public static string FormatEmail(string? mailId)
    {
        if (string.IsNullOrEmpty(mailId))
        {
            return string.Empty;
        }

        return mailId.Contains('@') ? mailId : $"{mailId}@ucdavis.edu";
    }

    /// <summary>
    /// The AAUD views carry PIDM as a string; every table that keys on one stores an int. This is
    /// the single conversion between the two, so no caller can key on a form of its own.
    /// </summary>
    public static int? ParsePidm(string? pidm) => int.TryParse(pidm, out var value) ? value : null;

    /// <summary>
    /// <see cref="ParsePidm"/> in out-parameter form, for callers that test and use the PIDM in
    /// one boolean expression and so have nowhere to put a null check.
    /// </summary>
    public static bool TryParsePidm(string? pidm, out int value) => int.TryParse(pidm, out value);
}

public sealed record DvmStudentIdentity(int PersonId, string FullName, string ClassLevel, int? Pidm);

public sealed record AaudPersonIdentity(int PersonId, string FullName);
