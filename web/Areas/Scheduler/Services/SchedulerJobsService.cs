using Hangfire;
using Hangfire.Storage;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Scheduler.Models.DTOs.Responses;
using Viper.Areas.Scheduler.Models.Entities;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using HangfireRecurringJobDto = Hangfire.Storage.RecurringJobDto;

namespace Viper.Areas.Scheduler.Services;

/// <summary>
/// List/pause/resume + reconciliation against Hangfire's recurring-job storage.
/// Pause and resume are ordered, idempotent, and reconcilable; the marker
/// table is the declared source of truth for "is this job paused?".
/// </summary>
public class SchedulerJobsService : ISchedulerJobsService
{
    private readonly VIPERContext _context;
    private readonly JobStorage _hangfireStorage;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly ILogger<SchedulerJobsService> _logger;

    public SchedulerJobsService(
        VIPERContext context,
        JobStorage hangfireStorage,
        IRecurringJobManager recurringJobManager,
        ILogger<SchedulerJobsService> logger)
    {
        _context = context;
        _hangfireStorage = hangfireStorage;
        _recurringJobManager = recurringJobManager;
        _logger = logger;
    }

    public async Task<List<SchedulerJobDto>> ListJobsAsync(CancellationToken ct = default)
    {
        var markers = await _context.SchedulerJobStates.AsNoTracking().ToListAsync(ct);
        var markerById = markers.ToDictionary(m => m.RecurringJobId, StringComparer.Ordinal);

        var hangfireJobs = GetHangfireRecurringJobs();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var results = new List<SchedulerJobDto>();

        foreach (var hf in hangfireJobs)
        {
            seenIds.Add(hf.Id);
            markerById.TryGetValue(hf.Id, out var marker);
            results.Add(BuildDtoFromHangfire(hf, marker));
        }

        results.AddRange(markers
            .Where(m => !seenIds.Contains(m.RecurringJobId))
            .Select(BuildDtoFromMarker));

        return [.. results.OrderBy(r => r.Id, StringComparer.Ordinal)];
    }

    public async Task<SchedulerJobDto?> GetJobAsync(string id, CancellationToken ct = default)
    {
        var marker = await _context.SchedulerJobStates
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.RecurringJobId == id, ct);

        var hf = GetHangfireRecurringJobs().FirstOrDefault(j => j.Id == id);

        if (hf != null)
        {
            return BuildDtoFromHangfire(hf, marker);
        }
        if (marker != null)
        {
            return BuildDtoFromMarker(marker);
        }
        return null;
    }

    public async Task<PauseResumeResultDto> PauseJobAsync(
        string id,
        string modBy,
        byte[]? expectedRowVersion,
        CancellationToken ct = default)
    {
        EnsureNotSystemJob(id);

        var hangfireJob = GetHangfireRecurringJobs().FirstOrDefault(j => j.Id == id);
        var existingMarker = await _context.SchedulerJobStates
            .FirstOrDefaultAsync(s => s.RecurringJobId == id, ct);

        // Idempotent path: marker already there, registration already gone.
        if (hangfireJob == null && existingMarker != null)
        {
            return new PauseResumeResultDto
            {
                Id = id,
                IsPaused = true,
                DeregistrationPending = false,
                RowVersion = Convert.ToBase64String(existingMarker.RowVersion),
            };
        }

        if (hangfireJob == null)
        {
            throw new SchedulerJobNotFoundException(id);
        }

        if (expectedRowVersion != null
            && existingMarker != null
            && !existingMarker.RowVersion.SequenceEqual(expectedRowVersion))
        {
            throw new SchedulerConcurrencyException(id);
        }

        // Hangfire stores Job as nullable on RecurringJobDto; a paused row that
        // can't resolve its job type will hit this path before pause writes a
        // marker. Surface as a 500 with context rather than NPE.
        if (hangfireJob.Job is null)
        {
            throw new InvalidOperationException(
                $"Cannot pause job '{id}': Hangfire cannot resolve its job type. The type may have been renamed or removed.");
        }

        var payload = SerializeJobPayload(hangfireJob.Job);

        if (existingMarker == null)
        {
            existingMarker = new SchedulerJobState
            {
                RecurringJobId = id,
                Cron = hangfireJob.Cron ?? string.Empty,
                Queue = hangfireJob.Queue ?? "default",
                TimeZoneId = hangfireJob.TimeZoneId ?? "UTC",
                JobTypeName = hangfireJob.Job?.Type?.AssemblyQualifiedName ?? string.Empty,
                SerializedArgs = payload,
                PausedAt = DateTime.Now,
                PausedBy = modBy,
            };
            _context.SchedulerJobStates.Add(existingMarker);
        }
        else
        {
            existingMarker.Cron = hangfireJob.Cron ?? string.Empty;
            existingMarker.Queue = hangfireJob.Queue ?? "default";
            existingMarker.TimeZoneId = hangfireJob.TimeZoneId ?? "UTC";
            existingMarker.JobTypeName = hangfireJob.Job?.Type?.AssemblyQualifiedName ?? string.Empty;
            existingMarker.SerializedArgs = payload;
            existingMarker.PausedAt = DateTime.Now;
            existingMarker.PausedBy = modBy;
        }

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new SchedulerConcurrencyException(id);
        }

        var deregistrationPending = false;
        try
        {
            _recurringJobManager.RemoveIfExists(id);
        }
        // Marker is authoritative when Hangfire's storage layer fails the
        // deregistration; the API returns 202 and the reconciler heals.
        // Hangfire wraps storage failures as BackgroundJobClientException;
        // SqlException covers raw provider errors that escape its translator,
        // and TimeoutException covers connection/lock timeouts.
        catch (Exception ex) when (ex is BackgroundJobClientException
            or Microsoft.Data.SqlClient.SqlException
            or TimeoutException)
        {
            deregistrationPending = true;
            _logger.LogError(
                ex,
                "Pause: marker persisted but Hangfire deregistration failed for {JobId}; reconciler will heal",
                LogSanitizer.SanitizeId(id));
        }

        return new PauseResumeResultDto
        {
            Id = id,
            IsPaused = true,
            DeregistrationPending = deregistrationPending,
            RowVersion = Convert.ToBase64String(existingMarker.RowVersion),
        };
    }

    public async Task<PauseResumeResultDto> ResumeJobAsync(
        string id,
        byte[]? expectedRowVersion,
        CancellationToken ct = default)
    {
        EnsureNotSystemJob(id);

        var marker = await _context.SchedulerJobStates
            .FirstOrDefaultAsync(s => s.RecurringJobId == id, ct);

        // Idempotent: no marker but registration already exists.
        if (marker == null)
        {
            var hangfireJob = GetHangfireRecurringJobs().FirstOrDefault(j => j.Id == id);
            if (hangfireJob != null)
            {
                return new PauseResumeResultDto
                {
                    Id = id,
                    IsPaused = false,
                    DeregistrationPending = false,
                };
            }
            throw new SchedulerJobNotFoundException(id);
        }

        if (expectedRowVersion is null || !marker.RowVersion.SequenceEqual(expectedRowVersion))
        {
            throw new SchedulerConcurrencyException(id);
        }

        var job = DeserializeJobPayload(marker.SerializedArgs);

        // Restore the original queue when resuming so a job paused from a
        // non-default queue resumes onto the same one. QueueName is the only
        // route on IRecurringJobManager.AddOrUpdate(Job, ...); the explicit
        // `queue` parameter mentioned in Hangfire's obsolete notice is on the
        // expression-based overload only and will land in 2.0.
#pragma warning disable CS0618 // RecurringJobOptions.QueueName obsolete in 2.0
        _recurringJobManager.AddOrUpdate(
            id,
            job,
            marker.Cron,
            new RecurringJobOptions
            {
                TimeZone = ResolveTimeZone(marker.TimeZoneId),
                QueueName = marker.Queue,
            });
#pragma warning restore CS0618

        _context.SchedulerJobStates.Remove(marker);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        // Reconciler treats "registered + marker present" as drift and will
        // delete the orphan marker on its next pass, so a stale marker after
        // a successful resume still converges.
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(
                ex,
                "Resume: registration succeeded for {JobId} but marker delete saw a concurrency conflict; reconciler will clean up",
                LogSanitizer.SanitizeId(id));
        }
        // For non-concurrency persistence failures the registration would
        // outlive the failure response and the reconciler would later treat
        // marker+registration as split-brain and remove it — un-resuming the
        // job. Compensate by removing the registration we just added so the
        // visible state matches the 5xx the caller is about to see. Wrap the
        // failure so the controller surfaces a typed exception instead of
        // log-and-rethrow on the bare DbUpdateException.
        catch (Exception ex) when (ex is DbUpdateException
            or Microsoft.Data.SqlClient.SqlException
            or TimeoutException)
        {
            try
            {
                _recurringJobManager.RemoveIfExists(id);
            }
            catch (Exception removeEx) when (removeEx is BackgroundJobClientException
                or Microsoft.Data.SqlClient.SqlException
                or TimeoutException)
            {
                _logger.LogError(
                    removeEx,
                    "Resume rollback failed for {JobId} after marker delete failed; reconciler will heal",
                    LogSanitizer.SanitizeId(id));
            }
            throw new InvalidOperationException(
                $"Resume failed for '{id}': marker delete error, registration rolled back.",
                ex);
        }

        return new PauseResumeResultDto
        {
            Id = id,
            IsPaused = false,
            DeregistrationPending = false,
        };
    }

    public async Task<ReconcilerOutcomeDto> ReconcileAsync(CancellationToken ct = default)
    {
        var markers = await _context.SchedulerJobStates.ToListAsync(ct);
        var hangfireJobs = GetHangfireRecurringJobs();
        var hangfireById = hangfireJobs.ToDictionary(j => j.Id, StringComparer.Ordinal);
        var outcome = new ReconcilerOutcomeDto
        {
            MarkersExamined = markers.Count,
            RegistrationsExamined = hangfireJobs.Count,
        };

        var splitBrainIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var marker in markers)
        {
            if (marker.RecurringJobId.StartsWith(ISchedulerJobsService.SystemJobPrefix, StringComparison.Ordinal))
            {
                _context.SchedulerJobStates.Remove(marker);
                outcome.SystemMarkersDeleted++;
                _logger.LogWarning(
                    "Reconciler: deleted system-namespaced marker {JobId} (markers must never reference __scheduler:* ids)",
                    LogSanitizer.SanitizeId(marker.RecurringJobId));
                continue;
            }

            if (hangfireById.ContainsKey(marker.RecurringJobId))
            {
                splitBrainIds.Add(marker.RecurringJobId);
                _recurringJobManager.RemoveIfExists(marker.RecurringJobId);
                outcome.SplitBrainHealed++;
                _logger.LogWarning(
                    "Reconciler: removed Hangfire registration for {JobId} (marker is intent)",
                    LogSanitizer.SanitizeId(marker.RecurringJobId));
                continue;
            }

            outcome.CorrectlyPaused++;
        }

        if (outcome.SystemMarkersDeleted > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        outcome.CorrectlyActive = hangfireJobs.Count(j => !splitBrainIds.Contains(j.Id));

        return outcome;
    }

    private static void EnsureNotSystemJob(string id)
    {
        if (id.StartsWith(ISchedulerJobsService.SystemJobPrefix, StringComparison.Ordinal))
        {
            throw new SchedulerSystemJobProtectedException(id);
        }
    }

    // Custom serialization rather than Hangfire.Common.InvocationData because
    // that type's accessibility is restricted in this assembly's compilation
    // context (a known Hangfire 1.8 wrinkle). System.Text.Json plus assembly-
    // qualified names round-trip the same shape with no library dependency.
    private sealed record JobPayload(string TypeName, string MethodName, string[] ParameterTypeNames, string[] SerializedArgs);

    private static string SerializeJobPayload(Hangfire.Common.Job job)
    {
        var parameters = job.Method.GetParameters();
        var payload = new JobPayload(
            job.Type.AssemblyQualifiedName ?? job.Type.FullName ?? string.Empty,
            job.Method.Name,
            [.. parameters.Select(p => p.ParameterType.AssemblyQualifiedName ?? p.ParameterType.FullName ?? string.Empty)],
            [.. job.Args.Select(a => System.Text.Json.JsonSerializer.Serialize(a))]);
        return System.Text.Json.JsonSerializer.Serialize(payload);
    }

    private static Hangfire.Common.Job DeserializeJobPayload(string serialized)
    {
        var payload = System.Text.Json.JsonSerializer.Deserialize<JobPayload>(serialized)
            ?? throw new InvalidOperationException("Stored job payload was empty.");
        var type = Type.GetType(payload.TypeName)
            ?? throw new InvalidOperationException($"Could not resolve job type '{payload.TypeName}'.");
        var paramTypes = payload.ParameterTypeNames
            .Select(n => Type.GetType(n) ?? throw new InvalidOperationException($"Could not resolve parameter type '{n}'."))
            .ToArray();
        var method = type.GetMethod(payload.MethodName, paramTypes)
            ?? throw new InvalidOperationException($"Could not resolve method '{payload.MethodName}' on '{payload.TypeName}'.");
        var args = payload.SerializedArgs
            .Zip(paramTypes, (json, t) => System.Text.Json.JsonSerializer.Deserialize(json, t))
            .ToArray();
        return new Hangfire.Common.Job(type, method, args!);
    }

    /// <summary>
    /// Reads Hangfire's current recurring-job set. Virtual so tests can
    /// override without standing up the storage extension method internals.
    /// </summary>
    protected virtual List<HangfireRecurringJobDto> GetHangfireRecurringJobs()
    {
        // Connection is per-call: matches Hangfire's recommended access
        // pattern and avoids lifetime issues with the storage layer.
        using var connection = _hangfireStorage.GetConnection();
        return [.. connection.GetRecurringJobs()];
    }

    private static SchedulerJobDto BuildDtoFromHangfire(
        HangfireRecurringJobDto hf,
        SchedulerJobState? marker)
    {
        return new SchedulerJobDto
        {
            Id = hf.Id,
            Cron = hf.Cron ?? string.Empty,
            TimeZoneId = hf.TimeZoneId ?? string.Empty,
            Queue = hf.Queue ?? "default",
            JobTypeName = hf.Job?.Type?.FullName ?? string.Empty,
            NextExecution = hf.NextExecution,
            LastExecution = hf.LastExecution,
            LastJobState = hf.LastJobState,
            IsPaused = marker != null,
            PausedAt = marker?.PausedAt,
            PausedBy = marker?.PausedBy,
            IsSystem = hf.Id.StartsWith(ISchedulerJobsService.SystemJobPrefix, StringComparison.Ordinal),
            RowVersion = marker != null ? Convert.ToBase64String(marker.RowVersion) : null,
        };
    }

    private static SchedulerJobDto BuildDtoFromMarker(SchedulerJobState marker)
    {
        return new SchedulerJobDto
        {
            Id = marker.RecurringJobId,
            Cron = marker.Cron,
            TimeZoneId = marker.TimeZoneId,
            Queue = marker.Queue,
            JobTypeName = marker.JobTypeName,
            NextExecution = null,
            LastExecution = null,
            LastJobState = null,
            IsPaused = true,
            PausedAt = marker.PausedAt,
            PausedBy = marker.PausedBy,
            IsSystem = marker.RecurringJobId.StartsWith(ISchedulerJobsService.SystemJobPrefix, StringComparison.Ordinal),
            RowVersion = Convert.ToBase64String(marker.RowVersion),
        };
    }

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
