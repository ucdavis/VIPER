using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.Entities;
using Viper.Classes.SQLContext;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Links cross-listed courses from Banner's cross-list groups (baseinfo_xlist_group).
/// </summary>
public class CrossListService : ICrossListService
{
    public const string CrossListType = "CrossList";
    public const string SectionType = "Section";

    public const string ReasonExistingLink = "Existing link";
    public const string ReasonPriorTerm = "Parent in prior term";
    public const string ReasonSvmSubject = "SVM subject";
    public const string ReasonEnrollment = "Highest enrollment";

    private readonly EffortDbContext _context;
    private readonly CoursesContext _coursesContext;
    private readonly IEffortAuditService _auditService;
    private readonly ILogger<CrossListService> _logger;

    public CrossListService(
        EffortDbContext context,
        CoursesContext coursesContext,
        IEffortAuditService auditService,
        ILogger<CrossListService> logger)
    {
        _context = context;
        _coursesContext = coursesContext;
        _auditService = auditService;
        _logger = logger;
    }

    internal sealed record BannerCrossList(string Crn, string Group, string CourseCode);

    internal sealed record PriorCrossList(int TermCode, string ParentCourse, string ChildCourse);

    public async Task<List<CrossListLink>> PlanAsync(int termCode, IReadOnlyCollection<CrossListCourse> courses,
        IReadOnlySet<int>? existingParentIds = null, CancellationToken ct = default)
    {
        var termCodeStr = termCode.ToString();
        var banner = (await _coursesContext.Baseinfos
                .AsNoTracking()
                .Where(b => b.BaseinfoTermCode == termCodeStr && b.BaseinfoXlistFlag == "Y")
                .Select(b => new { b.BaseinfoCrn, b.BaseinfoXlistGroup, b.BaseinfoSubjCode, b.BaseinfoCrseNumb })
                .ToListAsync(ct))
            .Select(b => new BannerCrossList(b.BaseinfoCrn.Trim(), b.BaseinfoXlistGroup.Trim(),
                $"{b.BaseinfoSubjCode.Trim()} {b.BaseinfoCrseNumb.Trim()}"))
            .ToList();

        if (banner.Count == 0)
        {
            return [];
        }

        // Cross-list links number a few hundred across all terms, so loading every prior one is cheap.
        var prior = (await _context.CourseRelationships
                .AsNoTracking()
                .Where(r => r.RelationshipType == CrossListType && r.ParentCourse.TermCode < termCode)
                .Select(r => new
                {
                    r.ParentCourse.TermCode,
                    ParentSubj = r.ParentCourse.SubjCode,
                    ParentCrse = r.ParentCourse.CrseNumb,
                    ChildSubj = r.ChildCourse.SubjCode,
                    ChildCrse = r.ChildCourse.CrseNumb
                })
                .ToListAsync(ct))
            .Select(r => new PriorCrossList(r.TermCode,
                $"{r.ParentSubj.Trim()} {r.ParentCrse.Trim()}", $"{r.ChildSubj.Trim()} {r.ChildCrse.Trim()}"))
            .ToList();

        return BuildLinks(courses, banner, prior, existingParentIds ?? new HashSet<int>());
    }

    /// <summary>
    /// Pure matching rules: group courses into cross-list families, pick each family's parent,
    /// and link every other course in the family to it.
    /// </summary>
    internal static List<CrossListLink> BuildLinks(
        IReadOnlyCollection<CrossListCourse> courses,
        IReadOnlyCollection<BannerCrossList> banner,
        IReadOnlyCollection<PriorCrossList> prior,
        IReadOnlySet<int> existingParentIds)
    {
        // Banner gives each section pair its own group (APC 100-A01 with NPB 123-A01, A02 with A02),
        // so groups listing the same courses are merged into one family. A group of one course
        // with itself is not a cross-list.
        var familyByCrn = banner
            .Where(b => b.Group.Length > 0)
            .GroupBy(b => b.Group)
            .Select(g => new
            {
                Crns = g.Select(b => b.Crn).Distinct().ToList(),
                Courses = g.Select(b => b.CourseCode).Distinct().OrderBy(c => c, StringComparer.Ordinal).ToList()
            })
            .Where(g => g.Courses.Count > 1)
            .SelectMany(g => g.Crns.Select(crn => (Crn: crn, Family: string.Join("|", g.Courses))))
            .GroupBy(x => x.Crn)
            .ToDictionary(g => g.Key, g => g.First().Family);

        var links = new List<CrossListLink>();
        foreach (var family in courses
                     .Where(c => familyByCrn.ContainsKey(c.Crn))
                     .GroupBy(c => familyByCrn[c.Crn])
                     .OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var byCourse = family.GroupBy(c => c.CourseCode).ToDictionary(g => g.Key, g => g.ToList());
            if (byCourse.Count < 2)
            {
                // The cross-listed partner course is not in the term.
                continue;
            }

            var (parentCode, reason, priorTermCode) = PickParentCourse(byCourse, prior, existingParentIds);

            // Staff use the lowest section as the parent (APC 100-A01 over A02 and A03).
            var parent = byCourse[parentCode]
                .OrderByDescending(c => existingParentIds.Contains(c.Id))
                .ThenBy(c => c.SeqNumb, StringComparer.Ordinal)
                .ThenBy(c => c.Crn, StringComparer.Ordinal)
                .ThenBy(c => c.Id)
                .First();

            links.AddRange(family
                // Other unit variants of the parent's own CRN are the same course, not children.
                .Where(c => c.Crn != parent.Crn)
                .OrderBy(c => c.CourseCode, StringComparer.Ordinal)
                .ThenBy(c => c.SeqNumb, StringComparer.Ordinal)
                .ThenBy(c => c.Id)
                .Select(c => new CrossListLink(parent, c,
                    c.CourseCode == parent.CourseCode ? SectionType : CrossListType, reason, priorTermCode)));
        }

        return links;
    }

    private static (string CourseCode, string Reason, int? PriorTermCode) PickParentCourse(
        Dictionary<string, List<CrossListCourse>> byCourse,
        IReadOnlyCollection<PriorCrossList> prior,
        IReadOnlySet<int> existingParentIds)
    {
        var existing = byCourse
            .Where(kv => kv.Value.Exists(c => existingParentIds.Contains(c.Id)))
            .Select(kv => kv.Key)
            .FirstOrDefault();
        if (existing != null)
        {
            return (existing, ReasonExistingLink, null);
        }

        // Staff keep the same parent from term to term; when it has changed, the change stuck.
        var last = prior
            .Where(p => byCourse.ContainsKey(p.ParentCourse) && byCourse.ContainsKey(p.ChildCourse))
            .MaxBy(p => p.TermCode);
        if (last != null)
        {
            return (last.ParentCourse, ReasonPriorTerm, last.TermCode);
        }

        // Prefer the school's own listing (APC 100 over NPB 123) even when the other has more students.
        var svm = byCourse
            .Where(kv => EffortConstants.AcademicDepartments.Contains(kv.Value[0].SubjCode, StringComparer.OrdinalIgnoreCase))
            .Select(kv => kv.Key)
            .ToList();
        if (svm.Count == 1)
        {
            return (svm[0], ReasonSvmSubject, null);
        }

        var best = (svm.Count > 0 ? svm : byCourse.Keys.ToList())
            .OrderByDescending(code => byCourse[code].Sum(c => c.Enrollment))
            .ThenBy(code => code, StringComparer.Ordinal)
            .First();
        return (best, ReasonEnrollment, null);
    }

    public async Task<CrossListResult> ApplyAsync(int termCode, string? onlyCrn = null, CancellationToken ct = default)
    {
        var courses = (await _context.Courses
                .AsNoTracking()
                .Where(c => c.TermCode == termCode)
                .Select(c => new { c.Id, c.Crn, c.SubjCode, c.CrseNumb, c.SeqNumb, c.Enrollment })
                .ToListAsync(ct))
            .Select(c => new CrossListCourse(c.Id, c.Crn.Trim(), c.SubjCode.Trim(), c.CrseNumb.Trim(), c.SeqNumb.Trim(), c.Enrollment))
            .ToList();

        var existing = await _context.CourseRelationships
            .AsNoTracking()
            .Where(r => r.ParentCourse.TermCode == termCode)
            .Select(r => new { r.ParentCourseId, r.ChildCourseId })
            .ToListAsync(ct);
        var parentIds = existing.Select(r => r.ParentCourseId).ToHashSet();
        var childIds = existing.Select(r => r.ChildCourseId).ToHashSet();

        var links = (await PlanAsync(termCode, courses, parentIds, ct))
            .Where(l => onlyCrn == null || l.Parent.Crn == onlyCrn || l.Child.Crn == onlyCrn)
            // Hierarchy is flat and a child has one parent, so leave anything already linked alone.
            .Where(l => !childIds.Contains(l.Child.Id) && !parentIds.Contains(l.Child.Id) && !childIds.Contains(l.Parent.Id))
            .ToList();

        var conflicts = new List<string>();
        if (links.Count == 0)
        {
            return new CrossListResult(links, conflicts);
        }

        var courseIds = links.SelectMany(l => new[] { l.Parent.Id, l.Child.Id }).Distinct().ToList();
        var records = await _context.Records
            .Include(r => r.Person)
            .Where(r => EF.Parameter(courseIds).Contains(r.CourseId))
            .ToListAsync(ct);
        var recordsByCourse = records.ToLookup(r => r.CourseId);

        // (CourseId, PersonId, EffortTypeId) is unique, so that is what decides whether a record can move.
        var onParent = records
            .GroupBy(r => (r.CourseId, r.PersonId, r.EffortTypeId))
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var link in links)
        {
            _context.CourseRelationships.Add(new CourseRelationship
            {
                ParentCourseId = link.Parent.Id,
                ChildCourseId = link.Child.Id,
                RelationshipType = link.RelationshipType
            });
            _auditService.AddCourseChangeAudit(link.Parent.Id, termCode, EffortAuditActions.CreateCourseRelationship,
                null,
                new
                {
                    ChildCourseId = link.Child.Id,
                    ChildCourse = link.Child.Label,
                    link.RelationshipType,
                    Source = "Banner cross-list",
                    link.Reason
                });

            // Effort is only entered on the parent, so nothing may be left behind on the child.
            foreach (var record in recordsByCourse[link.Child.Id])
            {
                var key = (link.Parent.Id, record.PersonId, record.EffortTypeId);
                if (!onParent.TryGetValue(key, out var parentRecord))
                {
                    _auditService.AddRecordChangeAudit(record.Id, termCode, EffortAuditActions.UpdateEffort,
                        new { Course = link.Child.Label, record.Crn },
                        new { Course = link.Parent.Label, link.Parent.Crn, Reason = "Moved to cross-list parent" });
                    record.CourseId = link.Parent.Id;
                    record.Crn = link.Parent.Crn;
                    onParent[key] = record;
                }
                else if (parentRecord.RoleId == record.RoleId && parentRecord.Hours == record.Hours && parentRecord.Weeks == record.Weeks)
                {
                    _auditService.AddRecordChangeAudit(record.Id, termCode, EffortAuditActions.DeleteEffort,
                        new
                        {
                            Reason = "Duplicate of cross-list parent record",
                            record.PersonId,
                            Course = link.Child.Label,
                            EffortType = record.EffortTypeId,
                            record.RoleId,
                            record.Hours,
                            record.Weeks
                        },
                        null);
                    _context.Records.Remove(record);
                }
                else
                {
                    // Hand-entered values that differ: only staff can say which is right.
                    conflicts.Add($"{record.Person.LastName.Trim()}, {record.Person.FirstName.Trim()}: " +
                        $"{record.EffortTypeId} effort on {link.Child.Label} differs from {link.Parent.Label} and was left on the child course");
                }
            }
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created {Count} cross-list links for term {TermCode} ({Conflicts} effort conflicts)",
            links.Count, termCode, conflicts.Count);

        return new CrossListResult(links, conflicts);
    }
}
