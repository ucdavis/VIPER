using System.Text.Json;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.EmailTemplates.Models;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Classes.SQLContext;
using Viper.EmailTemplates.Services;
using Viper.Classes.Utilities;
using Viper.Services;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for effort verification operations.
/// Handles self-service verification by instructors and admin email notifications.
/// </summary>
public class VerificationService : IVerificationService
{
    private readonly EffortDbContext _context;
    private readonly VIPERContext _viperContext;
    private readonly IEffortAuditService _auditService;
    private readonly IEffortPermissionService _permissionService;
    private readonly ITermService _termService;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private readonly ILogger<VerificationService> _logger;
    private readonly EffortSettings _settings;
    private readonly IEmailTemplateRenderer _emailTemplateRenderer;

    public VerificationService(
        EffortDbContext context,
        VIPERContext viperContext,
        IEffortAuditService auditService,
        IEffortPermissionService permissionService,
        ITermService termService,
        IEmailService emailService,
        IMapper mapper,
        ILogger<VerificationService> logger,
        IOptions<EffortSettings> settings,
        IEmailTemplateRenderer emailTemplateRenderer)
    {
        _context = context;
        _viperContext = viperContext;
        _auditService = auditService;
        _permissionService = permissionService;
        _termService = termService;
        _emailService = emailService;
        _mapper = mapper;
        _logger = logger;
        _settings = settings.Value;
        _emailTemplateRenderer = emailTemplateRenderer;
    }

    public async Task<MyEffortDto?> GetMyEffortAsync(int termCode, CancellationToken ct = default)
    {
        var currentPersonId = _permissionService.GetCurrentPersonId();
        if (currentPersonId == 0)
        {
            return null;
        }

        // Get instructor with effort records
        var instructor = await _context.Persons
            .AsNoTracking()
            .Include(p => p.Term)
            .FirstOrDefaultAsync(p => p.PersonId == currentPersonId && p.TermCode == termCode, ct);

        if (instructor == null)
        {
            return null;
        }

        // Get effort records with course and role info
        var records = await _context.Records
            .AsNoTracking()
            .Include(r => r.Course)
            .Include(r => r.RoleNavigation)
            .Where(r => r.PersonId == currentPersonId && r.TermCode == termCode)
            .OrderBy(r => r.Course.SubjCode)
            .ThenBy(r => r.Course.CrseNumb)
            .ThenBy(r => r.Course.SeqNumb)
            .ToListAsync(ct);

        var recordDtos = _mapper.Map<List<InstructorEffortRecordDto>>(records);

        // Get child courses for all parent courses
        var courseIds = records.Select(r => r.CourseId).Distinct().ToList();
        var childCourses = await GetChildCoursesAsync(courseIds, ct);

        // Attach child courses to their parent record
        foreach (var record in recordDtos.Where(r => childCourses.ContainsKey(r.CourseId)))
        {
            record.ChildCourses = childCourses[record.CourseId];
        }

        // Calculate zero-effort records
        var useWeeksForClinical = termCode >= EffortConstants.ClinicalAsWeeksStartTermCode;
        var zeroEffortRecordIds = records
            .Where(r => IsZeroEffort(r, useWeeksForClinical))
            .Select(r => r.Id)
            .ToList();

        // Check edit permission - user must have EditEffort permission or full access
        // Self-service users with only VerifyEffort permission cannot edit, only view and verify
        var term = await _termService.GetTermAsync(termCode, ct);
        var canEdit = (term?.IsOpen == true) && await _permissionService.CanEditPersonEffortAsync(currentPersonId, termCode, ct);

        // Check verify permission - must have VerifyEffort permission to submit verification
        // Instructors can verify even with no records (to confirm "no effort" for the term)
        var hasVerifyPermission = await _permissionService.HasSelfServiceAccessAsync(ct);
        var hasNoZeroEffort = zeroEffortRecordIds.Count == 0;

        return new MyEffortDto
        {
            Instructor = _mapper.Map<PersonDto>(instructor),
            EffortRecords = recordDtos,
            CrossListedCourses = childCourses.Values.SelectMany(c => c).ToList(),
            HasZeroEffort = !hasNoZeroEffort,
            ZeroEffortRecordIds = zeroEffortRecordIds,
            CanVerify = hasNoZeroEffort && hasVerifyPermission,
            CanEdit = canEdit,
            HasVerifyPermission = hasVerifyPermission,
            TermName = _termService.GetTermName(termCode),
            LastModifiedDate = records.Count == 0 ? null : records.Max(r => r.ModifiedDate),
            ClinicalAsWeeksStartTermCode = EffortConstants.ClinicalAsWeeksStartTermCode
        };
    }

    public async Task<VerificationResult> VerifyEffortAsync(int termCode, CancellationToken ct = default)
    {
        var currentPersonId = _permissionService.GetCurrentPersonId();
        if (currentPersonId == 0)
        {
            return new VerificationResult
            {
                Success = false,
                ErrorCode = VerificationErrorCodes.PersonNotFound,
                ErrorMessage = "You must be logged in to verify effort."
            };
        }

        // Get instructor
        var instructor = await _context.Persons
            .FirstOrDefaultAsync(p => p.PersonId == currentPersonId && p.TermCode == termCode, ct);

        if (instructor == null)
        {
            return new VerificationResult
            {
                Success = false,
                ErrorCode = VerificationErrorCodes.PersonNotFound,
                ErrorMessage = "You do not have effort records for this term."
            };
        }

        // Check if already verified
        if (instructor.EffortVerified.HasValue)
        {
            return new VerificationResult
            {
                Success = false,
                ErrorCode = VerificationErrorCodes.AlreadyVerified,
                ErrorMessage = "Your effort has already been verified.",
                VerifiedDate = instructor.EffortVerified.Value
            };
        }

        // Get record count for audit
        var recordCount = await _context.Records
            .AsNoTracking()
            .CountAsync(r => r.PersonId == currentPersonId && r.TermCode == termCode, ct);

        // Check for zero-effort records (instructors with no records can still verify "no effort")
        var canVerify = await CanVerifyAsync(currentPersonId, termCode, ct);
        if (!canVerify.CanVerify)
        {
            return new VerificationResult
            {
                Success = false,
                ErrorCode = VerificationErrorCodes.ZeroEffort,
                ErrorMessage = "You have courses with zero effort that must be resolved before verifying.",
                ZeroEffortCourses = canVerify.ZeroEffortCourses
            };
        }

        // Set verification timestamp
        var verifiedDate = DateTime.Now;
        var oldValues = new { EffortVerified = (DateTime?)null };

        instructor.EffortVerified = verifiedDate;

        // Get course list for audit (empty list if no records)
        var courses = recordCount > 0
            ? await _context.Records
                .AsNoTracking()
                .Include(r => r.Course)
                .Where(r => r.PersonId == currentPersonId && r.TermCode == termCode)
                .Select(r => $"{r.Course.SubjCode.Trim()} {r.Course.CrseNumb.Trim()}-{r.Course.SeqNumb.Trim()}")
                .Distinct()
                .ToListAsync(ct)
            : new List<string>();

        var newValues = new
        {
            EffortVerified = verifiedDate,
            EffortRecordCount = recordCount,
            Courses = courses,
            VerifiedNoEffort = recordCount == 0
        };

        await _context.SaveChangesAsync(ct);

        // Log audit
        await _auditService.LogPersonChangeAsync(
            currentPersonId,
            termCode,
            EffortAuditActions.VerifiedEffort,
            oldValues,
            newValues,
            ct);

        _logger.LogInformation("Person {PersonId} verified effort for term {TermCode}", currentPersonId, termCode);

        return new VerificationResult
        {
            Success = true,
            VerifiedDate = verifiedDate
        };
    }

    public async Task<CanVerifyResult> CanVerifyAsync(int personId, int termCode, CancellationToken ct = default)
    {
        var records = await _context.Records
            .AsNoTracking()
            .Include(r => r.Course)
            .Where(r => r.PersonId == personId && r.TermCode == termCode)
            .ToListAsync(ct);

        var useWeeksForClinical = termCode >= EffortConstants.ClinicalAsWeeksStartTermCode;
        var zeroEffortRecords = records
            .Where(r => IsZeroEffort(r, useWeeksForClinical))
            .ToList();

        var zeroEffortRecordIds = zeroEffortRecords.Select(r => r.Id).ToList();
        var zeroEffortCourses = zeroEffortRecords
            .Select(r => $"{r.Course.SubjCode.Trim()} {r.Course.CrseNumb.Trim()}-{r.Course.SeqNumb.Trim()}")
            .Distinct()
            .ToList();

        return new CanVerifyResult
        {
            CanVerify = zeroEffortRecordIds.Count == 0,
            ZeroEffortCount = zeroEffortRecordIds.Count,
            ZeroEffortCourses = zeroEffortCourses,
            ZeroEffortRecordIds = zeroEffortRecordIds
        };
    }

    /// <summary>
    /// Sends a verification reminder email to an instructor.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: This method does NOT perform authorization checks.
    /// The caller is responsible for verifying that the current user has permission
    /// to send emails to the specified instructor (e.g., via CanViewDepartmentAsync).
    /// See VerificationController.SendVerificationEmail for the authorization pattern.
    ///
    /// The email is sent from the current user's email address (matching legacy behavior),
    /// allowing instructors to reply directly to the sender.
    /// </remarks>
    public async Task<EmailSendResult> SendVerificationEmailAsync(int personId, int termCode, CancellationToken ct = default)
    {
        // Get sender email (current user) - matches legacy behavior where emails come from the logged-in user
        var senderEmail = _permissionService.GetCurrentUserEmail();
        if (string.IsNullOrWhiteSpace(senderEmail))
        {
            return new EmailSendResult { Success = false, Error = "Unable to determine sender email address" };
        }

        // Get instructor
        var instructor = await _context.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonId == personId && p.TermCode == termCode, ct);

        if (instructor == null)
        {
            return new EmailSendResult { Success = false, Error = "Instructor not found" };
        }

        // Get email address from VIPER Person table
        var person = await _viperContext.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonId == personId, ct);

        if (person == null || string.IsNullOrWhiteSpace(person.MailId))
        {
            var auditData = new
            {
                RecipientPersonId = personId,
                RecipientName = $"{instructor.LastName}, {instructor.FirstName}",
                SendResult = "Failed: No email address"
            };

            await _auditService.LogPersonChangeAsync(
                personId, termCode, EffortAuditActions.VerifyEmail, null, auditData, ct);

            return new EmailSendResult { Success = false, Error = "No email address found" };
        }

        // Validate email address format
        var mailId = person.MailId.Trim();
        var recipientEmail = $"{mailId}@ucdavis.edu";
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(recipientEmail))
        {
            var invalidEmailAuditData = new
            {
                RecipientPersonId = personId,
                RecipientName = $"{instructor.LastName}, {instructor.FirstName}",
                AttemptedEmail = recipientEmail,
                SendResult = "Failed: Invalid email address format"
            };

            await _auditService.LogPersonChangeAsync(
                personId, termCode, EffortAuditActions.VerifyEmail, null, invalidEmailAuditData, ct);

            return new EmailSendResult { Success = false, Error = "Invalid email address" };
        }

        // Validate configuration up-front so InvalidOperationException in try block is only from template errors
        var verificationUrl = BuildVerificationUrl(termCode);

        try
        {
            // Get effort records for email body
            var records = await _context.Records
                .AsNoTracking()
                .Include(r => r.Course)
                .Include(r => r.RoleNavigation)
                .Where(r => r.PersonId == personId && r.TermCode == termCode)
                .OrderBy(r => r.Course.SubjCode)
                .ThenBy(r => r.Course.CrseNumb)
                .ThenBy(r => r.Course.SeqNumb)
                .ToListAsync(ct);

            // Get child courses (only if there are records)
            var courseIds = records.Select(r => r.CourseId).Distinct().ToList();
            var childCourses = records.Count > 0
                ? await GetChildCourseInfoAsync(courseIds, ct)
                : new Dictionary<int, List<ChildCourseInfo>>();

            // Get term dates from VIPER
            var viperTerm = await _viperContext.Terms
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TermCode == termCode, ct);

            // Build email body using Razor template
            var termDescription = _termService.GetTermName(termCode);
            var viewModel = BuildVerificationEmailViewModel(
                records, childCourses, termDescription, verificationUrl, termCode,
                viperTerm?.StartDate, viperTerm?.EndDate);
            var emailBody = await _emailTemplateRenderer.RenderAsync(
                "/Areas/Effort/EmailTemplates/Views/VerificationReminder.cshtml", viewModel);

            // Send email
            await _emailService.SendEmailAsync(
                recipientEmail,
                _settings.VerificationEmailSubject,
                emailBody,
                isHtml: true,
                from: senderEmail);

            // Log success audit
            var successAuditData = new
            {
                RecipientEmail = recipientEmail,
                RecipientName = $"{instructor.LastName}, {instructor.FirstName}",
                RecipientPersonId = personId,
                SenderEmail = senderEmail,
                EmailSubject = _settings.VerificationEmailSubject,
                SendResult = "Success"
            };

            await _auditService.LogPersonChangeAsync(
                personId, termCode, EffortAuditActions.VerifyEmail, null, successAuditData, ct);

            _logger.LogInformation(
                "Sent verification email for person {PersonId} term {TermCode}",
                personId, termCode);

            return new EmailSendResult { Success = true };
        }
        catch (EmailSendException ex)
        {
            _logger.LogError(ex, "Email send error for person {PersonId} term {TermCode}", personId, termCode);

            var errorAuditData = new
            {
                RecipientEmail = recipientEmail,
                RecipientName = $"{instructor.LastName}, {instructor.FirstName}",
                RecipientPersonId = personId,
                EmailSubject = _settings.VerificationEmailSubject,
                SendResult = "Failed: Email send error"
            };

            await _auditService.LogPersonChangeAsync(
                personId, termCode, EffortAuditActions.VerifyEmail, null, errorAuditData, ct);

            return new EmailSendResult { Success = false, Error = "Failed to send email. Please try again." };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Template rendering error for verification email for person {PersonId} term {TermCode}", personId, termCode);

            var errorAuditData = new
            {
                RecipientEmail = recipientEmail,
                RecipientName = $"{instructor.LastName}, {instructor.FirstName}",
                RecipientPersonId = personId,
                EmailSubject = _settings.VerificationEmailSubject,
                SendResult = "Failed: Template error"
            };

            await _auditService.LogPersonChangeAsync(
                personId, termCode, EffortAuditActions.VerifyEmail, null, errorAuditData, ct);

            return new EmailSendResult { Success = false, Error = "Failed to generate email content. Please contact support." };
        }
    }

    public async Task<BulkEmailResult> SendBulkVerificationEmailsAsync(string departmentCode, int termCode, CancellationToken ct = default)
    {
        // Verify permission to this department
        if (!await _permissionService.CanViewDepartmentAsync(departmentCode, ct))
        {
            return new BulkEmailResult
            {
                TotalInstructors = 0,
                EmailsSent = 0,
                EmailsFailed = 0,
                Failures = new List<EmailFailure>
                {
                    new() { Reason = "Access denied to department" }
                }
            };
        }

        // Get unverified instructors in department (including those without records)
        var instructors = await _context.Persons
            .AsNoTracking()
            .Where(p => p.TermCode == termCode
                && p.EffortDept == departmentCode
                && p.EffortVerified == null)
            .ToListAsync(ct);

        var result = new BulkEmailResult
        {
            TotalInstructors = instructors.Count,
            EmailsSent = 0,
            EmailsFailed = 0,
            Failures = new List<EmailFailure>()
        };

        foreach (var instructor in instructors)
        {
            var emailResult = await SendVerificationEmailAsync(instructor.PersonId, termCode, ct);

            if (emailResult.Success)
            {
                result.EmailsSent++;
            }
            else
            {
                result.EmailsFailed++;
                result.Failures.Add(new EmailFailure
                {
                    PersonId = instructor.PersonId,
                    InstructorName = $"{instructor.LastName}, {instructor.FirstName}",
                    Reason = emailResult.Error ?? "Unknown error"
                });
            }
        }

        _logger.LogInformation(
            "Bulk email for dept {Dept} term {TermCode}: {Sent} sent, {Failed} failed",
            LogSanitizer.SanitizeId(departmentCode), termCode, result.EmailsSent, result.EmailsFailed);

        return result;
    }

    public async Task<List<EmailHistoryDto>> GetEmailHistoryAsync(int personId, int termCode, CancellationToken ct = default)
    {
        // Query audit entries for VerifyEmail action
        var audits = await _context.Audits
            .AsNoTracking()
            .Where(a => a.TableName == EffortAuditTables.Persons
                && a.RecordId == personId
                && a.TermCode == termCode
                && a.Action == EffortAuditActions.VerifyEmail)
            .OrderByDescending(a => a.ChangedDate)
            .ToListAsync(ct);

        // Get sender names
        var senderIds = audits.Select(a => a.ChangedBy).Distinct().ToList();
        var senderNames = await _viperContext.People
            .AsNoTracking()
            .Where(p => senderIds.Contains(p.PersonId))
            .ToDictionaryAsync(p => p.PersonId, p => p.FullName, ct);

        var result = new List<EmailHistoryDto>();

        foreach (var audit in audits)
        {
            var dto = new EmailHistoryDto
            {
                SentDate = audit.ChangedDate,
                SentBy = audit.ChangedBy.ToString(),
                SentByName = senderNames.TryGetValue(audit.ChangedBy, out var name) ? name : "Unknown"
            };

            // Parse Changes JSON for recipient info
            if (!string.IsNullOrEmpty(audit.Changes))
            {
                try
                {
                    using var doc = JsonDocument.Parse(audit.Changes);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("RecipientEmail", out var emailProp))
                    {
                        dto.RecipientEmail = emailProp.GetString() ?? "";
                    }
                    if (root.TryGetProperty("RecipientName", out var nameProp))
                    {
                        dto.RecipientName = nameProp.GetString() ?? "";
                    }
                }
                catch (JsonException)
                {
                    // Ignore parsing errors for legacy data
                }
            }

            result.Add(dto);
        }

        return result;
    }

    private async Task<Dictionary<int, List<ChildCourseDto>>> GetChildCoursesAsync(
        List<int> parentCourseIds, CancellationToken ct)
    {
        var relationships = await GetCourseRelationshipsAsync(parentCourseIds, ct);

        return relationships
            .GroupBy(r => r.ParentCourseId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new ChildCourseDto
                {
                    Id = r.ChildCourse.Id,
                    SubjCode = r.ChildCourse.SubjCode,
                    CrseNumb = r.ChildCourse.CrseNumb,
                    SeqNumb = r.ChildCourse.SeqNumb,
                    Units = r.ChildCourse.Units,
                    Enrollment = r.ChildCourse.Enrollment,
                    RelationshipType = r.RelationshipType
                }).ToList());
    }

    private async Task<Dictionary<int, List<ChildCourseInfo>>> GetChildCourseInfoAsync(
        List<int> parentCourseIds, CancellationToken ct)
    {
        var relationships = await GetCourseRelationshipsAsync(parentCourseIds, ct);

        return relationships
            .GroupBy(r => r.ParentCourseId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new ChildCourseInfo(
                    r.ChildCourse.SubjCode,
                    r.ChildCourse.CrseNumb,
                    r.ChildCourse.SeqNumb,
                    r.RelationshipType
                )).ToList());
    }

    private async Task<List<CourseRelationship>> GetCourseRelationshipsAsync(
        List<int> parentCourseIds, CancellationToken ct)
    {
        if (parentCourseIds.Count == 0)
        {
            return [];
        }

        return await _context.CourseRelationships
            .AsNoTracking()
            .Include(r => r.ChildCourse)
            .Where(r => parentCourseIds.Contains(r.ParentCourseId))
            .ToListAsync(ct);
    }

    private string BuildVerificationUrl(int termCode)
    {
        // Require configured base URL to avoid Host header injection
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            throw new InvalidOperationException("EffortSettings:BaseUrl must be configured for verification emails.");
        }

        var baseUrlNormalized = _settings.BaseUrl.TrimEnd('/') + "/";
        if (!Uri.TryCreate(baseUrlNormalized, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException($"EffortSettings:BaseUrl value '{_settings.BaseUrl}' is not a valid absolute URL.");
        }

        return new Uri(baseUri, $"Effort/{termCode}/my-effort").ToString();
    }

    /// <summary>
    /// Determines if an effort record has zero effort value.
    /// Clinical (CLI) effort uses weeks starting from ClinicalAsWeeksStartTermCode.
    /// </summary>
    private static bool IsZeroEffort(EffortRecord record, bool useWeeksForClinical)
    {
        if (useWeeksForClinical && record.EffortTypeId == EffortConstants.ClinicalEffortType)
        {
            return (record.Weeks ?? 0) == 0;
        }

        return (record.Hours ?? 0) == 0;
    }

    /// <summary>
    /// Builds the view model for the verification email Razor template.
    /// </summary>
    private VerificationReminderViewModel BuildVerificationEmailViewModel(
        IList<EffortRecord> records,
        IDictionary<int, List<ChildCourseInfo>> childCourses,
        string termDescription,
        string verificationUrl,
        int termCode,
        DateTime? termStartDate,
        DateTime? termEndDate)
    {
        var useWeeksForClinical = termCode >= EffortConstants.ClinicalAsWeeksStartTermCode;
        var replyByDate = DateTime.Now.AddDays(_settings.VerificationReplyDays).ToString("MM/dd/yyyy");

        // Group records by course
        var courseGroups = new List<EffortCourseGroup>();
        var hasZeroEffort = false;
        int? currentCourseId = null;
        EffortCourseGroup? currentGroup = null;

        foreach (var record in records.OrderBy(r => r.Course.SubjCode)
            .ThenBy(r => r.Course.CrseNumb)
            .ThenBy(r => r.Course.SeqNumb))
        {
            var (effortValue, effortUnit) = GetEffortValueAndType(record, useWeeksForClinical);

            if (effortValue == 0)
            {
                hasZeroEffort = true;
            }

            // New course group
            if (currentCourseId != record.CourseId)
            {
                currentGroup = new EffortCourseGroup
                {
                    CourseCode = $"{record.Course.SubjCode.Trim()} {record.Course.CrseNumb.Trim()}-{record.Course.SeqNumb.Trim()}",
                    Units = record.Course.Units,
                    Enrollment = record.Course.Enrollment,
                    Role = record.RoleNavigation.Description,
                    EffortItems = new List<EffortLineItem>()
                };
                courseGroups.Add(currentGroup);
                currentCourseId = record.CourseId;
            }

            // Add effort item to current group
            currentGroup?.EffortItems.Add(new EffortLineItem
            {
                EffortType = record.EffortTypeId,
                Value = effortValue,
                Unit = effortUnit
            });
        }

        // Build child courses list
        var childCoursesList = new List<ChildCourseDisplay>();
        var courseIds = records.Select(r => r.CourseId).Distinct();

        foreach (var courseId in courseIds.Where(id => childCourses.ContainsKey(id)))
        {
            foreach (var child in childCourses[courseId])
            {
                childCoursesList.Add(new ChildCourseDisplay
                {
                    CourseCode = $"{child.SubjCode.Trim()} {child.CrseNumb.Trim()}-{child.SeqNumb.Trim()}",
                    RelationshipType = child.RelationshipType
                });
            }
        }

        return new VerificationReminderViewModel
        {
            BaseUrl = _settings.BaseUrl ?? "",
            TermDescription = termDescription,
            TermStartDate = termStartDate,
            TermEndDate = termEndDate,
            ReplyByDate = replyByDate,
            VerificationUrl = verificationUrl,
            HasZeroEffort = hasZeroEffort,
            HasNoRecords = records.Count == 0,
            Courses = courseGroups,
            ChildCourses = childCoursesList
        };
    }

    /// <summary>
    /// Calculates the effort value and unit label for a record.
    /// Clinical effort uses weeks starting from ClinicalAsWeeksStartTermCode.
    /// </summary>
    private static (int Value, string UnitLabel) GetEffortValueAndType(EffortRecord record, bool useWeeksForClinical)
    {
        int effortValue;
        string effortValueType;

        if (useWeeksForClinical && record.EffortTypeId == EffortConstants.ClinicalEffortType)
        {
            effortValue = record.Weeks ?? 0;
            effortValueType = effortValue == 1 ? "Week" : "Weeks";
        }
        else
        {
            effortValue = record.Hours ?? 0;
            effortValueType = effortValue == 1 ? "Hour" : "Hours";
        }

        return (effortValue, effortValueType);
    }

    /// <summary>
    /// Simplified child course info for email building.
    /// </summary>
    private sealed record ChildCourseInfo(
        string SubjCode,
        string CrseNumb,
        string SeqNumb,
        string RelationshipType);
}
