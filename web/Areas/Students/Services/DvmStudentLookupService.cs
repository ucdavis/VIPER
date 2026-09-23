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
        var mothraToPersonId = await _aaudContext.AaudUsers
            .Where(u => EF.Parameter(mothraIds).Contains(u.MothraId))
            .Select(u => new { u.MothraId, u.AaudUserId })
            .AsNoTracking()
            .ToDictionaryAsync(u => u.MothraId, u => u.AaudUserId);

        return (dvmStudents, mothraToPersonId);
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
