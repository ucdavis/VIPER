using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Areas.Students.Models;
using Viper.Classes.Utilities;

namespace Viper.Areas.Students.Services
{
    public interface IStudentGroupService
    {
        Task<List<StudentPhoto>> GetStudentsByClassLevelAsync(string classLevel, bool includeRossStudents = false);
        Task<List<StudentPhoto>> GetStudentsByGroupAsync(string groupType, string groupId, string? classLevel = null);
        Task<List<StudentPhoto>> GetStudentsByCourseAsync(string termCode, string crn, bool includeRossStudents = false, string? groupType = null, string? groupId = null);
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
        private readonly CoursesContext _coursesContext;
        private readonly IPhotoService _photoService;
        private readonly ILogger<StudentGroupService> _logger;

        public StudentGroupService(AAUDContext aaudContext, SISContext sisContext, CoursesContext coursesContext, IPhotoService photoService, ILogger<StudentGroupService> logger)
        {
            _aaudContext = aaudContext;
            _sisContext = sisContext;
            _coursesContext = coursesContext;
            _photoService = photoService;
            _logger = logger;
        }

        public async Task<List<StudentPhoto>> GetStudentsByClassLevelAsync(string classLevel, bool includeRossStudents = false)
        {
            try
            {
                // Get current term
                var currentTerm = GetCurrentTerm().ToString();
                var currentTermInt = int.Parse(currentTerm);

                // Get list of Ross student IamIds to ALWAYS exclude from regular query
                // This prevents duplicates - Ross students are added separately with IsRossStudent=true
                List<string> rossIamIds = new List<string>();
                var rossGradYear = GradYearClassLevel.GetGradYear(classLevel, currentTermInt);
                if (rossGradYear.HasValue)
                {
                    try
                    {
                        var rossDesignations = await _sisContext.StudentDesignations
                            .Where(sd => sd.DesignationType == "Ross" && sd.ClassYear1 == rossGradYear)
                            .Where(sd => (sd.EndTerm == null || currentTermInt <= sd.EndTerm) &&
                                        (sd.StartTerm == null || sd.StartTerm <= currentTermInt))
                            .ToListAsync();

                        rossIamIds = rossDesignations.Select(rd => rd.IamId).ToList();
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogError(ex, "Invalid operation querying SIS context for Ross students");
                        // Continue with empty rossIamIds list - no Ross students will be excluded/added
                    }
                    catch (Microsoft.Data.SqlClient.SqlException ex)
                    {
                        _logger.LogError(ex, "Database error querying SIS context for Ross students");
                        // Continue with empty rossIamIds list - no Ross students will be excluded/added
                    }
                }

                // Query regular students from AAUD, excluding Ross students to prevent duplicates
                var query = from i in _aaudContext.Ids
                            join p in _aaudContext.People on i.IdsPKey equals p.PersonPKey
                            join s in _aaudContext.Students on p.PersonPKey equals s.StudentsPKey
                            join sg in _aaudContext.Studentgrps on i.IdsPidm equals sg.StudentgrpPidm into sgGroup
                            from sg in sgGroup.DefaultIfEmpty()
                            where s.StudentsClassLevel == classLevel && i.IdsTermCode == currentTerm
                                  && (string.IsNullOrEmpty(i.IdsIamId) || !rossIamIds.Contains(i.IdsIamId!))
                            select new
                            {
                                PersonId = p.PersonPKey,
                                MailId = i.IdsMailid,
                                FirstName = p.PersonDisplayFirstName ?? p.PersonFirstName,
                                LastName = p.PersonLastName,
                                MiddleName = p.PersonMiddleName,
                                IamId = i.IdsIamId,
                                BannerId = i.IdsClientid,
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

                    var photoUrl = string.IsNullOrWhiteSpace(student.MailId)
                        ? string.Empty
                        : await _photoService.GetStudentPhotoUrlAsync(student.MailId);
                    var hasPhoto = !string.Equals(
                        photoUrl,
                        _photoService.GetDefaultPhotoUrl(),
                        StringComparison.OrdinalIgnoreCase);

                    // Combine Eighths and Twentieths groups in format "2B1 / 1AA"
                    var groupAssignment = FormatGroupAssignment(student.EighthsGroup, student.TwentiethsGroup);

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
                        IsRossStudent = false,
                        ClassLevel = student.ClassLevel
                    };

                    photoStudents.Add(photoStudent);
                }

                // Add Ross students if requested
                if (includeRossStudents)
                {
                    _logger.LogDebug("Including Ross students for class level {ClassLevel}", LogSanitizer.SanitizeString(classLevel));

                    if (rossGradYear.HasValue)
                    {
                        var rossStudents = await GetRossStudentsByGradYearAsync(rossGradYear.Value, classLevel);
                        _logger.LogDebug("Adding {Count} Ross students to {TotalStudents} regular students",
                            rossStudents.Count, photoStudents.Count);
                        photoStudents.AddRange(rossStudents);
                        photoStudents = photoStudents.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToList();
                    }
                }

                return photoStudents;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation getting students by class level {ClassLevel}", LogSanitizer.SanitizeString(classLevel));
                return new List<StudentPhoto>();
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogError(ex, "Database error getting students by class level {ClassLevel}", LogSanitizer.SanitizeString(classLevel));
                return new List<StudentPhoto>();
            }
        }

        /// <summary>
        /// Retrieves Ross students for a specific graduation year.
        /// Ross students are transfer students from Ross University who are not enrolled every term,
        /// so this uses the most recent AAUD record available rather than requiring current term enrollment.
        /// </summary>
        /// <param name="gradYear">The graduation year to filter Ross students</param>
        /// <param name="classLevel">The class level (V1-V4) for display purposes</param>
        /// <returns>List of StudentPhoto objects for active Ross students in the specified graduation year</returns>
        private async Task<List<StudentPhoto>> GetRossStudentsByGradYearAsync(int gradYear, string classLevel)
        {
            try
            {
                // Get current term for filtering
                var currentTerm = GetCurrentTerm();
                var currentTermInt = int.Parse(currentTerm.ToString());

                // Query StudentDesignation table in SIS database
                // Filter by ClassYear1 (ClassYear2 is always NULL per database data)
                // Also filter by StartTerm/EndTerm to only include active Ross students
                var rossDesignations = await _sisContext.StudentDesignations
                    .Where(sd => sd.DesignationType == "Ross" && sd.ClassYear1 == gradYear)
                    .Where(sd => (sd.EndTerm == null || currentTermInt <= sd.EndTerm) &&
                                (sd.StartTerm == null || sd.StartTerm <= currentTermInt))
                    .ToListAsync();

                var rossIamIds = rossDesignations.Select(sd => sd.IamId).ToList();

                if (!rossIamIds.Any())
                {
                    _logger.LogDebug("No Ross students found for grad year {GradYear}", gradYear);
                    return new List<StudentPhoto>();
                }

                // For Ross students, we use the most recent AAUD record available rather than
                // requiring an exact match on the current term. This is because:
                // 1. Ross students are transfer students from Ross University who come to UC Davis for clinical work
                // 2. Ross students are not enrolled in every term (only when doing clinical rotations)
                // 3. Regular students always have current-term AAUD records due to continuous enrollment
                // 4. This ensures Ross students appear in the gallery using their most recent contact info
                // Trade-off: Contact info (mailId, etc.) may be from a recent past term if not currently enrolled.
                // We validate that the current term falls within the StudentDesignation date range.

                // Get the most recent AAUD record for each Ross student
                // Only include terms <= current term and validate against StudentDesignation date range
                var currentTermString = currentTermInt.ToString();

                // Get all AAUD records for Ross students (any term <= current term)
                var allAaudRecords = await _aaudContext.Ids
                    .Where(ids => ids.IdsIamId != null && rossIamIds.Contains(ids.IdsIamId))
                    .Where(ids => ids.IdsTermCode.CompareTo(currentTermString) <= 0)
                    .ToListAsync();

                // Filter and get latest record for each student
                var latestAaudRecords = allAaudRecords
                    .GroupBy(ids => ids.IdsIamId)
                    .Select(g => g.OrderByDescending(x => x.IdsTermCode).FirstOrDefault())
                    .Where(ids => ids != null)
                    .ToList();

                _logger.LogDebug("Found {Count} latest AAUD records for Ross students", latestAaudRecords.Count);

                // Then fetch People records for the latest AAUD records and join in-memory
                var latestPersonPKeys = latestAaudRecords.Where(r => r != null).Select(r => r!.IdsPKey).Distinct().ToList();
                var peopleDict = (await _aaudContext.People
                    .Where(person => latestPersonPKeys.Contains(person.PersonPKey))
                    .ToListAsync())
                    .ToDictionary(p => p.PersonPKey, p => p);

                var rossStudents = latestAaudRecords
                    .Where(ids => ids != null && peopleDict.ContainsKey(ids.IdsPKey))
                    .Select(ids =>
                    {
                        var person = peopleDict[ids!.IdsPKey];
                        return new
                        {
                            MailId = ids.IdsMailid,
                            FirstName = person.PersonDisplayFirstName ?? person.PersonFirstName,
                            LastName = person.PersonLastName,
                            MiddleName = person.PersonMiddleName,
                            IamId = ids.IdsIamId,
                            TermCode = ids.IdsTermCode
                        };
                    })
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .ToList();

                _logger.LogDebug("Found {Count} Ross students in AAUD using latest available records", rossStudents.Count);

                var photoStudents = new List<StudentPhoto>();
                foreach (var student in rossStudents)
                {
                    var displayName = FormatStudentDisplayName(student.LastName, student.FirstName, student.MiddleName);

                    var photoUrl = string.IsNullOrWhiteSpace(student.MailId)
                        ? string.Empty
                        : await _photoService.GetStudentPhotoUrlAsync(student.MailId);
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
                        IsRossStudent = true,
                        ClassLevel = classLevel
                    });
                }

                _logger.LogDebug("Returning {Count} Ross students with photos", photoStudents.Count);
                return photoStudents;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation getting Ross students for grad year {GradYear}. Message: {Message}. InnerException: {InnerMessage}",
                    gradYear, ex.Message, ex.InnerException?.Message ?? "None");
                return new List<StudentPhoto>();
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogError(ex, "Database error getting Ross students for grad year {GradYear}. Message: {Message}. InnerException: {InnerMessage}",
                    gradYear, ex.Message, ex.InnerException?.Message ?? "None");
                return new List<StudentPhoto>();
            }
        }

        public async Task<List<StudentPhoto>> GetStudentsByGroupAsync(string groupType, string groupId, string? classLevel = null)
        {
            try
            {
                var currentTerm = GetCurrentTerm().ToString();
                var currentTermInt = int.Parse(currentTerm);

                // Get list of Ross student IamIds to ALWAYS exclude from regular query
                // This prevents duplicates - Ross students would need to be added separately if we supported includeRoss for groups
                List<string> rossIamIds = new List<string>();
                try
                {
                    rossIamIds = await _sisContext.StudentDesignations
                        .Where(sd => sd.DesignationType == "Ross")
                        .Where(sd => (sd.EndTerm == null || currentTermInt <= sd.EndTerm) &&
                                    (sd.StartTerm == null || sd.StartTerm <= currentTermInt))
                        .Select(sd => sd.IamId)
                        .Where(id => id != null)
                        .Distinct()
                        .ToListAsync();
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "Invalid operation querying SIS context for Ross students");
                    // Continue with empty rossIamIds list - no Ross students will be excluded
                }
                catch (Microsoft.Data.SqlClient.SqlException ex)
                {
                    _logger.LogError(ex, "Database error querying SIS context for Ross students");
                    // Continue with empty rossIamIds list - no Ross students will be excluded
                }

                var queryBase = from i in _aaudContext.Ids
                                join p in _aaudContext.People on i.IdsPKey equals p.PersonPKey
                                join s in _aaudContext.Students on p.PersonPKey equals s.StudentsPKey
                                join sg in _aaudContext.Studentgrps on i.IdsPidm equals sg.StudentgrpPidm
                                where i.IdsTermCode == currentTerm
                                      && (string.IsNullOrEmpty(i.IdsIamId) || !rossIamIds.Contains(i.IdsIamId))
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
                    IamId = x.i.IdsIamId,
                    BannerId = x.i.IdsClientid,
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

                    var photoUrl = string.IsNullOrWhiteSpace(student.MailId)
                        ? string.Empty
                        : await _photoService.GetStudentPhotoUrlAsync(student.MailId);
                    var hasPhoto = !string.Equals(
                        photoUrl,
                        _photoService.GetDefaultPhotoUrl(),
                        StringComparison.OrdinalIgnoreCase);

                    // Combine Eighths and Twentieths groups in format "2B1 / 1AA"
                    var groupAssignment = FormatGroupAssignment(student.EighthsGroup, student.TwentiethsGroup);

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
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation getting students by group {GroupType}/{GroupId}", LogSanitizer.SanitizeString(groupType), LogSanitizer.SanitizeString(groupId));
                return new List<StudentPhoto>();
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogError(ex, "Database error getting students by group {GroupType}/{GroupId}", LogSanitizer.SanitizeString(groupType), LogSanitizer.SanitizeString(groupId));
                return new List<StudentPhoto>();
            }
        }

        public async Task<List<StudentPhoto>> GetStudentsByCourseAsync(string termCode, string crn, bool includeRossStudents = false, string? groupType = null, string? groupId = null)
        {
            try
            {
                var currentTermInt = int.Parse(termCode);

                // Get list of Ross IAM IDs so we can always exclude them unless explicitly requested
                List<string> rossIamIds = new();
                try
                {
                    rossIamIds = await _sisContext.StudentDesignations
                        .Where(sd => sd.DesignationType == "Ross")
                        .Where(sd => (sd.EndTerm == null || currentTermInt <= sd.EndTerm) &&
                                    (sd.StartTerm == null || sd.StartTerm <= currentTermInt))
                        .Select(sd => sd.IamId)
                        .Where(id => id != null)
                        .Distinct()
                        .ToListAsync();
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "Invalid operation querying SIS context for Ross students");
                }
                catch (Microsoft.Data.SqlClient.SqlException ex)
                {
                    _logger.LogError(ex, "Database error querying SIS context for Ross students");
                }

                // Query students enrolled in the course
                // Two-step approach to avoid multi-context joins (following legacy implementation pattern)
                // Step 1: Get PIDMs of enrolled students from Courses database
                var enrolledPidms = await _coursesContext.Rosters
                    .Where(r => r.RosterTermCode == termCode
                                && r.RosterCrn == crn
                                && r.RosterEnrollStatus == "RE")
                    .Select(r => r.RosterPidm)
                    .Distinct()
                    .ToListAsync();

                if (!enrolledPidms.Any())
                {
                    _logger.LogInformation("No enrolled students found for course {TermCode}/{Crn}",
                        LogSanitizer.SanitizeId(termCode),
                        LogSanitizer.SanitizeId(crn));
                    return new List<StudentPhoto>();
                }

                // Step 2: Query AAUD database with the enrolled PIDMs
                var query = from i in _aaudContext.Ids
                            join p in _aaudContext.People on i.IdsPKey equals p.PersonPKey
                            join s in _aaudContext.Students on p.PersonPKey equals s.StudentsPKey
                            join sg in _aaudContext.Studentgrps on i.IdsPidm equals sg.StudentgrpPidm into sgGroup
                            from sg in sgGroup.DefaultIfEmpty()
                            where enrolledPidms.Contains(i.IdsPidm)
                                  && i.IdsTermCode == termCode
                                  && (string.IsNullOrEmpty(i.IdsIamId) || !rossIamIds.Contains(i.IdsIamId!))
                            select new
                            {
                                PersonId = p.PersonPKey,
                                MailId = i.IdsMailid,
                                FirstName = p.PersonDisplayFirstName ?? p.PersonFirstName,
                                LastName = p.PersonLastName,
                                MiddleName = p.PersonMiddleName,
                                IamId = i.IdsIamId,
                                BannerId = i.IdsClientid,
                                ClassLevel = s.StudentsClassLevel,
                                EighthsGroup = sg != null ? sg.StudentgrpGrp : null,
                                TwentiethsGroup = sg != null ? sg.Studentgrp20 : null,
                                TeamNumber = sg != null ? sg.StudentgrpTeamno : null,
                                V3SpecialtyGroup = sg != null ? sg.StudentgrpV3grp : null
                            };

                // Apply group filtering if specified
                if (!string.IsNullOrEmpty(groupType) && !string.IsNullOrEmpty(groupId))
                {
                    query = groupType.ToLower() switch
                    {
                        "eighths" => query.Where(s => s.EighthsGroup == groupId),
                        "twentieths" => query.Where(s => s.TwentiethsGroup == groupId),
                        "teams" => query.Where(s => s.TeamNumber == groupId),
                        "v3specialty" => query.Where(s => s.V3SpecialtyGroup == groupId),
                        _ => query
                    };
                }

                var students = await query.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToListAsync();
                var photoStudents = new List<StudentPhoto>();

                foreach (var student in students)
                {
                    var displayName = FormatStudentDisplayName(student.LastName, student.FirstName, student.MiddleName);

                    var photoUrl = string.IsNullOrWhiteSpace(student.MailId)
                        ? string.Empty
                        : await _photoService.GetStudentPhotoUrlAsync(student.MailId);
                    var hasPhoto = !string.Equals(
                        photoUrl,
                        _photoService.GetDefaultPhotoUrl(),
                        StringComparison.OrdinalIgnoreCase);

                    // Combine Eighths and Twentieths groups
                    var groupAssignment = FormatGroupAssignment(student.EighthsGroup, student.TwentiethsGroup);

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
                        IsRossStudent = false,
                        ClassLevel = student.ClassLevel
                    };

                    photoStudents.Add(photoStudent);
                }

                // Add Ross students if requested
                if (includeRossStudents && rossIamIds.Any())
                {
                    _logger.LogDebug("Including Ross students for course {TermCode}/{Crn}", LogSanitizer.SanitizeString(termCode), LogSanitizer.SanitizeString(crn));

                    // Get Ross students who are enrolled in this course
                    var rossStudentsInCourse = await (from r in _coursesContext.Rosters
                                                      join i in _aaudContext.Ids on r.RosterPidm equals i.IdsPidm
                                                      where r.RosterTermCode == termCode
                                                            && r.RosterCrn == crn
                                                            && i.IdsIamId != null
                                                            && rossIamIds.Contains(i.IdsIamId)
                                                      select i.IdsIamId)
                                                     .Distinct()
                                                     .ToListAsync();

                    if (rossStudentsInCourse.Any())
                    {
                        // Fetch all Ross student data in a single bulk query to avoid N+1 problem
                        // Group by IamId and get the most recent record for each student
                        var rossStudentData = await (from i in _aaudContext.Ids
                                                     join p in _aaudContext.People on i.IdsPKey equals p.PersonPKey
                                                     join s in _aaudContext.Students on p.PersonPKey equals s.StudentsPKey
                                                     where i.IdsIamId != null && rossStudentsInCourse.Contains(i.IdsIamId)
                                                     orderby i.IdsIamId, i.IdsTermCode descending
                                                     select new
                                                     {
                                                         i.IdsIamId,
                                                         i.IdsMailid,
                                                         i.IdsTermCode,
                                                         PersonFirstName = p.PersonDisplayFirstName ?? p.PersonFirstName,
                                                         PersonLastName = p.PersonLastName,
                                                         PersonMiddleName = p.PersonMiddleName,
                                                         ClassLevel = s.StudentsClassLevel
                                                     })
                                                    .ToListAsync();

                        // Take only the most recent record for each IamId (already ordered by IdsTermCode descending)
                        var latestRossStudents = rossStudentData
                            .GroupBy(s => s.IdsIamId)
                            .Select(g => g.First())
                            .Where(s => !string.IsNullOrEmpty(s.IdsMailid))
                            .ToList();

                        // Map to StudentPhoto objects (this is in-memory, no more database calls except for photos)
                        foreach (var student in latestRossStudents)
                        {
                            var displayName = FormatStudentDisplayName(student.PersonLastName, student.PersonFirstName, student.PersonMiddleName);
                            var photoUrl = string.IsNullOrWhiteSpace(student.IdsMailid)
                                ? string.Empty
                                : await _photoService.GetStudentPhotoUrlAsync(student.IdsMailid);
                            var hasPhoto = !string.Equals(photoUrl, _photoService.GetDefaultPhotoUrl(), StringComparison.OrdinalIgnoreCase);

                            photoStudents.Add(new StudentPhoto
                            {
                                MailId = student.IdsMailid,
                                FirstName = student.PersonFirstName,
                                LastName = student.PersonLastName,
                                DisplayName = displayName,
                                PhotoUrl = photoUrl,
                                GroupAssignment = null,
                                EighthsGroup = null,
                                TwentiethsGroup = null,
                                HasPhoto = hasPhoto,
                                IsRossStudent = true,
                                ClassLevel = student.ClassLevel
                            });
                        }

                        photoStudents = photoStudents.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToList();
                    }
                }

                return photoStudents;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation getting students by course {TermCode}/{Crn}", LogSanitizer.SanitizeString(termCode), LogSanitizer.SanitizeString(crn));
                return new List<StudentPhoto>();
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogError(ex, "Database error getting students by course {TermCode}/{Crn}", LogSanitizer.SanitizeString(termCode), LogSanitizer.SanitizeString(crn));
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
                    .Select(sg => sg.Studentgrp20!.Trim())
                    .Distinct()
                    .OrderBy(g => g)
                    .ToListAsync();

                return groups;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation getting twentieths groups");
                return new List<string>();
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogError(ex, "Database error getting twentieths groups");
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
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation getting teams for class level {ClassLevel}", LogSanitizer.SanitizeString(classLevel));
                return new List<string>();
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogError(ex, "Database error getting teams for class level {ClassLevel}", LogSanitizer.SanitizeString(classLevel));
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
                                      select sg.StudentgrpV3grp!.Trim())
                                     .Distinct()
                                     .ToListAsync();

                // Merge baseline and database groups, remove duplicates, and sort
                var allGroups = baselineGroups.Union(dbGroups)
                                             .Distinct()
                                             .OrderBy(g => g)
                                             .ToList();

                return allGroups;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation getting V3 specialty groups");
                // Fallback to baseline on error
                return new List<string> { "SA", "LA", "EQ", "LIVE", "ZOO" };
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogError(ex, "Database error getting V3 specialty groups");
                // Fallback to baseline on error
                return new List<string> { "SA", "LA", "EQ", "LIVE", "ZOO" };
            }
        }

        private static int GetCurrentTerm()
        {
            // Logic to determine current semester term
            // This should match the legacy system's currentSemesterTerm logic
            var now = DateTime.Now;
            var year = now.Year;
            var month = now.Month;

            // Fall semester starts in September, otherwise Spring/Summer
            return month >= 9 ? int.Parse($"{year}09") : int.Parse($"{year}01");
        }

        private static string FormatStudentDisplayName(string lastName, string firstName, string? middleName)
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
                GroupId = string.Empty,
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

        /// <summary>
        /// Formats a student's group assignment by combining Eighths and Twentieths groups.
        /// Returns the combined format "EighthsGroup / TwentiethsGroup" if both exist,
        /// otherwise returns whichever group is available, or empty string if neither exists.
        /// </summary>
        /// <param name="eighthsGroup">The student's Eighths group assignment</param>
        /// <param name="twentiethsGroup">The student's Twentieths group assignment</param>
        /// <returns>Formatted group assignment string</returns>
        private static string FormatGroupAssignment(string? eighthsGroup, string? twentiethsGroup)
        {
            if (!string.IsNullOrEmpty(eighthsGroup) && !string.IsNullOrEmpty(twentiethsGroup))
            {
                return $"{eighthsGroup} / {twentiethsGroup}";
            }
            else if (!string.IsNullOrEmpty(eighthsGroup))
            {
                return eighthsGroup;
            }
            else if (!string.IsNullOrEmpty(twentiethsGroup))
            {
                return twentiethsGroup;
            }
            return string.Empty;
        }

    }
}
