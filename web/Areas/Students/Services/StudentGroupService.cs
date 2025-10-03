using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Areas.Students.Models;

namespace Viper.Areas.Students.Services
{
    public interface IStudentGroupService
    {
        Task<List<StudentPhoto>> GetStudentsByClassLevelAsync(string classLevel, bool includeRossStudents =
false);
        Task<List<StudentPhoto>> GetStudentsByGroupAsync(string groupType, string groupId, string? classLevel = null);
        Task<List<string>> GetEighthsGroupsAsync();
        Task<List<string>> GetTwentiethsGroupsAsync();
        Task<List<string>> GetTeamsAsync(string classLevel);
        Task<List<string>> GetV3SpecialtyGroupsAsync();
        Task<GroupingInfo> GetGroupingInfoAsync(string groupType);
    }

    public class StudentGroupService : IStudentGroupService
    {
        private readonly AAUDContext _aaudContext;
        private readonly SISContext _sisContext;
        private readonly IPhotoService _photoService;
        private readonly ILogger<StudentGroupService> _logger;

        public StudentGroupService(AAUDContext aaudContext, SISContext sisContext, IPhotoService photoService,
 ILogger<StudentGroupService> logger)
        {
            _aaudContext = aaudContext;
            _sisContext = sisContext;
            _photoService = photoService;
            _logger = logger;
        }

        public async Task<List<StudentPhoto>> GetStudentsByClassLevelAsync(string classLevel, bool
includeRossStudents = false)
        {
            try
            {
                // Get current term
                var currentTerm = GetCurrentTerm().ToString();
                var currentTermInt = int.Parse(currentTerm);

                // Get list of Ross student IamIds to exclude from regular query
                List<string> rossIamIds = new List<string>();
                if (includeRossStudents)
                {
                    var gradYear = GradYearClassLevel.GetGradYear(classLevel, currentTermInt);
                    if (gradYear.HasValue)
                    {
                        var rossDesignations = await _sisContext.StudentDesignations
                            .Where(sd => sd.DesignationType == "Ross" && sd.ClassYear1 == gradYear)
                            .Where(sd => (sd.EndTerm == null || currentTermInt <= sd.EndTerm) &&
                                        (sd.StartTerm == null || sd.StartTerm <= currentTermInt))
                            .ToListAsync();
                        rossIamIds = rossDesignations.Select(rd => rd.IamId).ToList();
                    }
                }

                // Query regular students from AAUD, excluding Ross students to prevent duplicates
                var query = from i in _aaudContext.Ids
                            join p in _aaudContext.People on i.IdsPKey equals p.PersonPKey
                            join s in _aaudContext.Students on p.PersonPKey equals s.StudentsPKey
                            join sg in _aaudContext.Studentgrps on i.IdsPidm equals sg.StudentgrpPidm into sgGroup
                            from sg in sgGroup.DefaultIfEmpty()
                            where s.StudentsClassLevel == classLevel && i.IdsTermCode == currentTerm
                                  && !rossIamIds.Contains(i.IdsIamId)
                            select new
                            {
                                PersonId = p.PersonPKey,
                                MailId = i.IdsMailid,
                                FirstName = p.PersonDisplayFirstName ?? p.PersonFirstName,
                                LastName = p.PersonLastName,
                                MiddleName = p.PersonMiddleName,
                                ClassLevel = s.StudentsClassLevel,
                                EighthsGroup = sg != null ? sg.StudentgrpGrp : null,
                                TwentiethsGroup = sg != null ? sg.Studentgrp20 : null,
                                TeamNumber = sg != null ? sg.StudentgrpTeamno : null,
                                V3SpecialtyGroup = sg != null ? sg.StudentgrpV3grp : null
                            };

                var students = await query.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToListAsync();

                var photoStudents = new List<StudentPhoto>();
                foreach (var student in students)
                {
                    var displayName = FormatStudentDisplayName(student.LastName, student.FirstName, student.MiddleName);

                    var photoUrl = await _photoService.GetStudentPhotoUrlAsync(student.MailId);
                    var hasPhoto = !string.Equals(
                        photoUrl,
                        _photoService.GetDefaultPhotoUrl(),
                        StringComparison.OrdinalIgnoreCase);

                    // Combine Eighths and Twentieths groups in format "2B1 / 1AA"
                    var groupAssignment = "";
                    if (!string.IsNullOrEmpty(student.EighthsGroup) && !string.IsNullOrEmpty(student.TwentiethsGroup))
                    {
                        groupAssignment = $"{student.EighthsGroup} / {student.TwentiethsGroup}";
                    }
                    else if (!string.IsNullOrEmpty(student.EighthsGroup))
                    {
                        groupAssignment = student.EighthsGroup;
                    }
                    else if (!string.IsNullOrEmpty(student.TwentiethsGroup))
                    {
                        groupAssignment = student.TwentiethsGroup;
                    }

                    var photoStudent = new StudentPhoto
                    {
                        MailId = student.MailId,
                        FirstName = student.FirstName,
                        LastName = student.LastName,
                        DisplayName = displayName,
                        PhotoUrl = photoUrl,
                        GroupAssignment = groupAssignment,
                        EighthsGroup = student.EighthsGroup?.Trim(),
                        TwentiethsGroup = student.TwentiethsGroup?.Trim(),
                        TeamNumber = student.ClassLevel == "V3" ? student.TeamNumber?.Trim() : null,
                        V3SpecialtyGroup = student.ClassLevel == "V3" ? student.V3SpecialtyGroup?.Trim() : null,
                        HasPhoto = hasPhoto,
                        IsRossStudent = false
                    };

                    photoStudents.Add(photoStudent);
                }

                // Add Ross students if requested
                if (includeRossStudents)
                {
                    _logger.LogInformation("Including Ross students for class level {ClassLevel}", classLevel);
                    var termCode = int.Parse(currentTerm);
                    var gradYear = GradYearClassLevel.GetGradYear(classLevel, termCode);
                    _logger.LogInformation("Calculated grad year: {GradYear} for class level {ClassLevel} and term {TermCode}",
                        gradYear, classLevel, termCode);

                    if (gradYear.HasValue)
                    {
                        var rossStudents = await GetRossStudentsByGradYearAsync(gradYear.Value);
                        _logger.LogInformation("Adding {Count} Ross students to {TotalStudents} regular students",
                            rossStudents.Count, photoStudents.Count);
                        photoStudents.AddRange(rossStudents);
                        photoStudents = photoStudents.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToList();
                    }
                }

                return photoStudents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students by class level {ClassLevel}", classLevel);
                return new List<StudentPhoto>();
            }
        }

        private async Task<List<StudentPhoto>> GetRossStudentsByGradYearAsync(int gradYear)
        {
            try
            {
                _logger.LogInformation("Searching for Ross students with grad year {GradYear}", gradYear);

                // Get current term
                var currentTerm = GetCurrentTerm().ToString();
                _logger.LogInformation("Current term: {CurrentTerm}", currentTerm);

                // Query StudentDesignation table in SIS database
                var rossDesignations = await _sisContext.StudentDesignations
                    .Where(sd => sd.DesignationType == "Ross" && sd.ClassYear1 == gradYear)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} Ross student designations", rossDesignations.Count);
                foreach (var rd in rossDesignations)
                {
                    _logger.LogDebug("  Ross Student: IamId={IamId}, Name={FirstName} {LastName}, StartTerm={StartTerm}, EndTerm={EndTerm}",
                        rd.IamId, rd.FirstName, rd.LastName, rd.StartTerm, rd.EndTerm);
                }

                var rossIamIds = rossDesignations.Select(sd => sd.IamId).ToList();

                if (!rossIamIds.Any())
                {
                    _logger.LogInformation("No Ross students found for grad year {GradYear}", gradYear);
                    return new List<StudentPhoto>();
                }

                // For Ross students, we use the most recent AAUD record available rather than
                // requiring an exact match on the current term. This is because:
                // 1. Ross students are often added to StudentDesignation before AAUD is updated
                // 2. Regular students always have current-term AAUD records due to enrollment processes
                // 3. This ensures Ross students appear in the gallery immediately upon designation
                // Trade-off: Contact info (mailId, etc.) may be from a recent past term until AAUD updates.
                // We validate that the current term falls within the StudentDesignation date range.

                // Check what's in AAUD Ids table for these IamIds (debugging)
                var aaudIdsForRoss = await _aaudContext.Ids
                    .Where(ids => ids.IdsIamId != null && rossIamIds.Contains(ids.IdsIamId))
                    .Select(ids => new { ids.IdsIamId, ids.IdsTermCode, ids.IdsMailid })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} AAUD Ids records for Ross IamIds (any term)", aaudIdsForRoss.Count);
                foreach (var aaud in aaudIdsForRoss)
                {
                    _logger.LogDebug("  AAUD Id: IamId={IamId}, TermCode={TermCode}, MailId={MailId}",
                        aaud.IdsIamId, aaud.IdsTermCode, aaud.IdsMailid);
                }

                // Get the most recent AAUD record for each Ross student
                // Only include terms <= current term and validate against StudentDesignation date range
                var currentTermInt = int.Parse(currentTerm);

                // Get all AAUD records for Ross students (any term <= current term)
                var allAaudRecords = await _aaudContext.Ids
                    .Where(ids => ids.IdsIamId != null && rossIamIds.Contains(ids.IdsIamId))
                    .Where(ids => ids.IdsTermCode.CompareTo(currentTerm) <= 0)
                    .ToListAsync();

                // Filter and get latest record for each student, validating date ranges
                var latestAaudRecords = allAaudRecords
                    .GroupBy(ids => ids.IdsIamId)
                    .Select(g =>
                    {
                        var designation = rossDesignations.First(rd => rd.IamId == g.Key);
                        // Only include if current term is within the designation date range
                        if ((designation.EndTerm == null || currentTermInt <= designation.EndTerm) &&
                            (designation.StartTerm == null || designation.StartTerm <= currentTermInt))
                        {
                            return g.OrderByDescending(x => x.IdsTermCode).FirstOrDefault();
                        }
                        return null;
                    })
                    .Where(ids => ids != null)
                    .ToList();

                _logger.LogInformation("Found {Count} latest AAUD records for Ross students", latestAaudRecords.Count());

                // Then query AAUD for student details using the latest records
                var latestRecordKeys = latestAaudRecords.Where(r => r != null).Select(r => r.IdsPKey).ToList();
                var rossStudents = await _aaudContext.Ids
                    .Where(ids => latestRecordKeys.Contains(ids.IdsPKey))
                    .Join(_aaudContext.People,
                          ids => ids.IdsPKey,
                          person => person.PersonPKey,
                          (ids, person) => new
                          {
                              MailId = ids.IdsMailid,
                              FirstName = person.PersonDisplayFirstName ?? person.PersonFirstName,
                              LastName = person.PersonLastName,
                              MiddleName = person.PersonMiddleName,
                              IamId = ids.IdsIamId,
                              TermCode = ids.IdsTermCode
                          })
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} Ross students in AAUD using latest available records", rossStudents.Count);
                _logger.LogDebug("Ross students: {Students}", string.Join(", ", rossStudents.Select(s => $"{s.MailId} ({s.IamId}, term {s.TermCode})")));

                var photoStudents = new List<StudentPhoto>();
                foreach (var student in rossStudents)
                {
                    var displayName = FormatStudentDisplayName(student.LastName, student.FirstName, student.MiddleName);

                    var photoUrl = await _photoService.GetStudentPhotoUrlAsync(student.MailId);
                    var hasPhoto = !string.Equals(
                        photoUrl,
                        _photoService.GetDefaultPhotoUrl(),
                        StringComparison.OrdinalIgnoreCase);

                    photoStudents.Add(new StudentPhoto
                    {
                        MailId = student.MailId,
                        FirstName = student.FirstName,
                        LastName = student.LastName,
                        DisplayName = displayName,
                        PhotoUrl = photoUrl,
                        GroupAssignment = null,
                        EighthsGroup = null,
                        TwentiethsGroup = null,
                        HasPhoto = hasPhoto,
                        IsRossStudent = true
                    });
                }

                _logger.LogInformation("Returning {Count} Ross students with photos", photoStudents.Count);
                return photoStudents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Ross students for grad year {GradYear}. Message: {Message}. InnerException: {InnerMessage}",
                    gradYear, ex.Message, ex.InnerException?.Message ?? "None");
                return new List<StudentPhoto>();
            }
        }

        public async Task<List<StudentPhoto>> GetStudentsByGroupAsync(string groupType, string groupId, string? classLevel = null)
        {
            try
            {
                var currentTerm = GetCurrentTerm().ToString();

                var queryBase = from i in _aaudContext.Ids
                                join p in _aaudContext.People on i.IdsPKey equals p.PersonPKey
                                join s in _aaudContext.Students on p.PersonPKey equals s.StudentsPKey
                                join sg in _aaudContext.Studentgrps on i.IdsPidm equals sg.StudentgrpPidm
                                where i.IdsTermCode == currentTerm
                                select new { i, p, s, sg };

                // Filter by class level first if provided
                if (!string.IsNullOrEmpty(classLevel))
                {
                    queryBase = queryBase.Where(x => x.s.StudentsClassLevel == classLevel);
                }

                // Then filter by specific group
                if (groupType?.ToLower() == "eighths")
                {
                    queryBase = queryBase.Where(x => x.sg.StudentgrpGrp == groupId);
                }
                else if (groupType?.ToLower() == "twentieths")
                {
                    queryBase = queryBase.Where(x => x.sg.Studentgrp20 == groupId);
                }
                else if (groupType?.ToLower() == "teams")
                {
                    queryBase = queryBase.Where(x => x.sg.StudentgrpTeamno == groupId);
                }
                else if (groupType?.ToLower() == "v3specialty")
                {
                    queryBase = queryBase.Where(x => x.sg.StudentgrpV3grp == groupId);
                }

                var query = queryBase.Select(x => new
                {
                    PersonId = x.p.PersonPKey,
                    MailId = x.i.IdsMailid,
                    FirstName = x.p.PersonDisplayFirstName ?? x.p.PersonFirstName,
                    LastName = x.p.PersonLastName,
                    MiddleName = x.p.PersonMiddleName,
                    ClassLevel = x.s.StudentsClassLevel,
                    EighthsGroup = x.sg.StudentgrpGrp,
                    TwentiethsGroup = x.sg.Studentgrp20,
                    TeamNumber = x.sg.StudentgrpTeamno,
                    V3SpecialtyGroup = x.sg.StudentgrpV3grp
                });

                var students = await query.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToListAsync();

                var photoStudents = new List<StudentPhoto>();
                foreach (var student in students)
                {
                    var displayName = FormatStudentDisplayName(student.LastName, student.FirstName, student.MiddleName);

                    var photoUrl = await _photoService.GetStudentPhotoUrlAsync(student.MailId);
                    var hasPhoto = !string.Equals(
                        photoUrl,
                        _photoService.GetDefaultPhotoUrl(),
                        StringComparison.OrdinalIgnoreCase);

                    // Combine Eighths and Twentieths groups in format "2B1 / 1AA"
                    var groupAssignment = "";
                    if (!string.IsNullOrEmpty(student.EighthsGroup) && !string.IsNullOrEmpty(student.TwentiethsGroup))
                    {
                        groupAssignment = $"{student.EighthsGroup} / {student.TwentiethsGroup}";
                    }
                    else if (!string.IsNullOrEmpty(student.EighthsGroup))
                    {
                        groupAssignment = student.EighthsGroup;
                    }
                    else if (!string.IsNullOrEmpty(student.TwentiethsGroup))
                    {
                        groupAssignment = student.TwentiethsGroup;
                    }

                    photoStudents.Add(new StudentPhoto
                    {
                        MailId = student.MailId,
                        FirstName = student.FirstName,
                        LastName = student.LastName,
                        DisplayName = displayName,
                        PhotoUrl = photoUrl,
                        GroupAssignment = groupAssignment,
                        EighthsGroup = student.EighthsGroup?.Trim(),
                        TwentiethsGroup = student.TwentiethsGroup?.Trim(),
                        TeamNumber = student.ClassLevel == "V3" ? student.TeamNumber?.Trim() : null,
                        V3SpecialtyGroup = student.ClassLevel == "V3" ? student.V3SpecialtyGroup?.Trim() : null,
                        HasPhoto = hasPhoto,
                        IsRossStudent = false
                    });
                }

                return photoStudents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students by group {GroupType}/{GroupId}", groupType, groupId);
                return new List<StudentPhoto>();
            }
        }

        public async Task<List<string>> GetEighthsGroupsAsync()
        {
            return await Task.FromResult(new List<string>
              {
                  "1A1", "1A2", "1B1", "1B2",
                  "2A1", "2A2", "2B1", "2B2"
              });
        }

        public async Task<List<string>> GetTwentiethsGroupsAsync()
        {
            try
            {
                var groups = await _aaudContext.Studentgrps
                    .Where(sg => !string.IsNullOrEmpty(sg.Studentgrp20))
                    .Select(sg => sg.Studentgrp20.Trim())
                    .Distinct()
                    .OrderBy(g => g)
                    .ToListAsync();

                return groups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting twentieths groups");
                return new List<string>();
            }
        }

        public async Task<List<string>> GetTeamsAsync(string classLevel)
        {
            try
            {
                var currentTerm = GetCurrentTerm().ToString();

                var groups = await (from sg in _aaudContext.Studentgrps
                                    join i in _aaudContext.Ids on sg.StudentgrpPidm equals i.IdsPidm
                                    join s in _aaudContext.Students on i.IdsPKey equals s.StudentsPKey
                                    where s.StudentsClassLevel == classLevel &&
                                          i.IdsTermCode == currentTerm &&
                                          !string.IsNullOrEmpty(sg.StudentgrpTeamno)
                                    select sg.StudentgrpTeamno)
                                   .Distinct()
                                   .ToListAsync();

                // Natural sort: sort numerically if the values are numbers, otherwise alphabetically
                return groups.OrderBy(t =>
                {
                    if (int.TryParse(t, out int num))
                    {
                        return num;
                    }
                    return int.MaxValue; // Non-numeric values go to the end
                }).ThenBy(t => t) // Secondary sort alphabetically for non-numeric values
                .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teams for class level {ClassLevel}", classLevel);
                return new List<string>();
            }
        }

        public async Task<List<string>> GetV3SpecialtyGroupsAsync()
        {
            try
            {
                // Baseline groups from legacy system - always show these
                var baselineGroups = new List<string> { "SA", "LA", "EQ", "LIVE", "ZOO" };

                var currentTerm = GetCurrentTerm().ToString();

                // Query database for any groups currently assigned to students
                var dbGroups = await (from sg in _aaudContext.Studentgrps
                                      join i in _aaudContext.Ids on sg.StudentgrpPidm equals i.IdsPidm
                                      join s in _aaudContext.Students on i.IdsPKey equals s.StudentsPKey
                                      where s.StudentsClassLevel == "V3" &&
                                            i.IdsTermCode == currentTerm &&
                                            !string.IsNullOrEmpty(sg.StudentgrpV3grp)
                                      select sg.StudentgrpV3grp.Trim())
                                     .Distinct()
                                     .ToListAsync();

                // Merge baseline and database groups, remove duplicates, and sort
                var allGroups = baselineGroups.Union(dbGroups)
                                             .Distinct()
                                             .OrderBy(g => g)
                                             .ToList();

                return allGroups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting V3 specialty groups");
                // Fallback to baseline on error
                return new List<string> { "SA", "LA", "EQ", "LIVE", "ZOO" };
            }
        }

        private int GetCurrentTerm()
        {
            // Logic to determine current semester term
            // This should match the legacy system's currentSemesterTerm logic
            var now = DateTime.Now;
            var year = now.Year;
            var month = now.Month;

            // Fall semester starts in September
            if (month >= 9)
            {
                return int.Parse($"{year}09");
            }
            // Spring/Summer semester
            else if (month >= 1 && month <= 8)
            {
                return int.Parse($"{year}01");
            }

            return int.Parse($"{year}09");
        }

        private string FormatStudentDisplayName(string lastName, string firstName, string? middleName)
        {
            var displayName = $"{lastName}, {firstName}";
            if (!string.IsNullOrEmpty(middleName))
            {
                displayName += $" {middleName[0]}";
            }
            return displayName;
        }

        public async Task<GroupingInfo> GetGroupingInfoAsync(string groupType)
        {
            var groupInfo = new GroupingInfo
            {
                GroupType = groupType,
                AvailableGroups = new List<string>()
            };

            switch (groupType?.ToLower())
            {
                case "eighths":
                    groupInfo.AvailableGroups = await GetEighthsGroupsAsync();
                    break;
                case "twentieths":
                    groupInfo.AvailableGroups = await GetTwentiethsGroupsAsync();
                    break;
                case "teams":
                    groupInfo.AvailableGroups = await GetTeamsAsync("V3");
                    break;
                case "v3specialty":
                    groupInfo.AvailableGroups = await GetV3SpecialtyGroupsAsync();
                    break;
            }

            return groupInfo;
        }
    }
}
