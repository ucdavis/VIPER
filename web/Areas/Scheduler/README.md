# Scheduler

Cron-driven background jobs for VIPER. Built on Hangfire 1.8 with SQL Server
storage; jobs are written against a thin `IScheduledJob` abstraction so they
do not depend on Hangfire types directly.

This document is the operational source of truth for the scheduler:
how to add a job, how it is configured, and how to triage incidents.

---

## Onboarding a job

Every recurring job is a class that implements `IScheduledJob` and carries a
`[ScheduledJob]` attribute. Discovery happens at startup; there is no
manifest file to update.

### 1. Declare the job

Place the file under your area's `Jobs/` folder. Example, the RAPS
role-membership refresh:

```csharp
// web/Areas/RAPS/Jobs/RapsRoleRefreshScheduledJob.cs
[ScheduledJob(id: "raps:role-refresh", cron: "0 0 * * *", TimeZoneId = "Pacific Standard Time")]
public sealed class RapsRoleRefreshScheduledJob : IScheduledJob
{
    private readonly RAPSContext _rapsContext;
    private readonly ILogger<RapsRoleRefreshScheduledJob> _logger;

    public RapsRoleRefreshScheduledJob(
        RAPSContext rapsContext,
        ILogger<RapsRoleRefreshScheduledJob> logger)
    {
        _rapsContext = rapsContext;
        _logger = logger;
    }

    public async Task RunAsync(ScheduledJobContext context, CancellationToken ct)
    {
        var roleViews = new RoleViews(_rapsContext);
        await roleViews.UpdateRoles(modBy: context.ModBy, debugOnly: false);
    }
}
```

### 2. Naming rules

| Field | Rule |
|---|---|
| `id` | `area:job-name` for user jobs (e.g. `raps:role-refresh`). Must NOT start with `__scheduler:`. |
| `id` (system jobs) | If `IsSystem = true`, the id MUST start with `__scheduler:`. The discovery pass refuses any combination that violates this invariant. |
| `cron` | Five-field Hangfire cron (`m h dom mon dow`). |
| `TimeZoneId` | Defaults to `Pacific Standard Time`. UC Davis runs Windows; IANA aliases like `America/Los_Angeles` also work. |

### 3. Stamping audit rows

Background jobs run with no HTTP context, so `UserHelper.GetCurrentUser()`
is **not available**. Every job receives a `ScheduledJobContext` carrying:

- `TriggerSource` &mdash; `Scheduled` for cron-driven runs, `Manual` for
  admin-triggered runs.
- `ModBy` &mdash; the audit actor for this run. Pass it through to your
  service layer; do not derive it inside the job.

For the scheduler-triggered path, `ModBy` is `"__sched"` (7 chars; the
legacy `tblRoleMembers.ModBy` column is `varchar(8)`, so the stamp is
shortened to fit while staying distinct from the existing `"__system"`
convention). Existing audit queries can filter on
`WHERE ModBy = '__sched'` to isolate scheduler-driven changes from
human-driven changes.

### 4. DI

Job dependencies are resolved from a fresh DI scope per execution. Any
`Scoped` service (DbContexts, scoped services from Scrutor) works without
extra wiring &mdash; the discovery pass registers your job type as
`Scoped` for you.

### 5. What runs where

| Surface | Mechanism |
|---|---|
| Initial registration | At app startup, after Hangfire is mounted, every `[ScheduledJob]`-declared type is `AddOrUpdate`'d. Idempotent. |
| Subsequent registrations | A fresh deploy with new jobs picks them up on next startup. |
| Lost-registration heal | If a job is missing from Hangfire's storage and has no pause marker, the hourly `__scheduler:reconcile` recurring job re-registers it. |

---

## Configuration

All settings live in `appsettings.{Environment}.json` (or AWS SSM
parameters in deployed environments).

| Key | Purpose | Default |
|---|---|---|
| `Hangfire:Enabled` | Master switch. When `false`, no scheduler wiring runs and the dashboard is unreachable. | `true` |
| `ConnectionStrings:VIPER` | The database that hosts Hangfire's tables and our `[HangFire].[SchedulerJobState]` marker table. Required when `Hangfire:Enabled=true`. Hangfire and the marker table share VIPER; splitting them is not supported. | n/a |
| `IPAddressAllowlistConfiguration:InternalAllowlist` | Source-IP gate for `/health/detail` and the HealthChecks UI. Add SVM infra ranges + your office subnet. | localhost only |

The dashboard does **not** read this config; it is always mounted at
`/scheduler/dashboard` when Hangfire is enabled and is gated by RAPS,
not IP.

---

## Access

| Surface | URL | Auth |
|---|---|---|
| Hangfire dashboard | `/scheduler/dashboard` | Cookie auth (CAS) + RAPS permission `SVMSecure.CATS.scheduledJobs` |
| Pause/resume API | `/api/scheduler/jobs`, `/api/scheduler/jobs/{id}/pause`, `/api/scheduler/jobs/{id}/resume` | Same RAPS permission |
| Health (liveness) | `/health` | Anonymous (Jenkins polls it) |
| Health (detail) | `/health/detail` | IP-allowlisted to `InternalAllowlist` |

`SVMSecure.CATS.scheduledJobs` is the same permission the legacy
ColdFusion VIPER scheduler (`cats/inc_scheduledTasks.cfm`) checks &mdash;
admins who already manage the legacy scheduler inherit access without a
provisioning step.

---

## Pause and resume

Hangfire has no native "paused" state, so we deregister the recurring job
and persist its definition in the `[HangFire].[SchedulerJobState]` marker
table. The marker is the **declared source of truth** for "is this job
paused?".

| Property | Behavior |
|---|---|
| Pause ordering | Marker write first, then `RemoveIfExists`. If `RemoveIfExists` throws, the API returns HTTP 202 with `deregistrationPending: true` and the reconciler completes the deregistration on its next pass. |
| Resume ordering | Re-register first, then delete the marker. A residual marker after a successful registration is healed by the reconciler. |
| Idempotency | Pause-on-already-paused returns 200 with the existing rowversion. Resume-on-already-active returns 200. |
| Concurrency | `RowVersion` (SQL Server `rowversion`) guards every write. Stale rowversion &rarr; HTTP 409. |
| System jobs | Ids starting with `__scheduler:` are refused (HTTP 403, `error: "system_job_not_pausable"`) before any write. They remain visible in the list and on the dashboard. |

---

## Operations runbook

### Heartbeat verification

| Symptom | Where to look |
|---|---|
| "Is the scheduler alive?" | `/health/detail` &mdash; the `hangfire` check reports `Healthy` (one or more servers with recent heartbeats), `Degraded` (storage reachable but no servers), or `Unhealthy` (storage error or all heartbeats > 2 minutes stale). |
| "Did this job run?" | Dashboard &rarr; Recurring Jobs &rarr; row for the id; columns show last/next execution and last state. |
| "Are workers processing?" | Dashboard &rarr; Servers panel; heartbeats refresh every 30 seconds. |

### Retrying a failed job

1. Open `/scheduler/dashboard`.
2. Failed jobs appear in the **Failed** queue.
3. Click the job &rarr; **Requeue** to retry once, or **Delete** to discard.
4. Recurring jobs that fail still trigger on their next cron schedule
   regardless &mdash; requeue is for retrying the specific failed
   instance.

### Pause / resume expectations

| Scenario | Expected outcome |
|---|---|
| Pause a running job | Marker created, registration removed, returns 200. |
| Pause when registration removal fails | Marker created, returns 202 with `deregistrationPending: true`. Reconciler finishes within an hour. |
| Pause an already-paused job | Returns 200 idempotently (no marker rewrite). |
| Resume a paused job | Registration restored, marker deleted, returns 200. |
| Resume with stale rowversion | Returns 409. Refresh and retry. |
| Pause/resume a `__scheduler:` job | Returns 403 with `error: "system_job_not_pausable"`. |

### Reconciler outcomes

The hourly `__scheduler:reconcile` job logs an outcome counter on every
pass. Look for the structured log entry:

```text
Scheduler reconciler pass: split-brain healed=N, system markers deleted=N, paused ok=N, active ok=N, lost registrations healed=N, markers=N, registrations=N
```

Non-zero `splitBrainHealed`, `systemMarkersDeleted`, or
`lostRegistrationsHealed` indicate drift was corrected this pass.
Persistent non-zero values across passes mean something keeps creating
drift &mdash; investigate before accepting it as normal.

### Pre-escalation checklist

Before paging a developer, verify in this order:

1. **Connection string** &mdash; `ConnectionStrings:VIPER` resolves and
   the SQL login has read/write on the `HangFire` schema.
2. **Permission grant** &mdash; the user holds
   `SVMSecure.CATS.scheduledJobs` (check RAPS).
3. **Server heartbeat** &mdash; `/health/detail` returns `hangfire`
   `Healthy`. If `Degraded` (no servers), the worker process is down or
   not started; confirm `Hangfire:Enabled=true` and check application
   startup logs for `"Hangfire is enabled but ConnectionStrings:VIPER is
   empty"` or DDL errors.
4. **Recent deploys** &mdash; a job that disappeared after a deploy and
   has not yet been re-registered will be picked up by the next reconciler
   pass (within an hour). Force-trigger by restarting the app or by
   triggering `__scheduler:reconcile` from the dashboard.
5. **Marker drift** &mdash; query
   `SELECT * FROM [HangFire].[SchedulerJobState]`. Markers with a
   `__scheduler:` id should not exist; the reconciler deletes them as a
   safety net but their presence indicates an attempted protected-prefix
   pause.

---

## Related code

| Concern | Location |
|---|---|
| Hangfire wiring (DI, dashboard mount, schema bootstrap) | `web/Classes/Scheduler/HangfireExtensions.cs` |
| Dashboard auth filter | `web/Classes/Scheduler/HangfireDashboardAuthorizationFilter.cs` |
| Per-job logging filter | `web/Classes/Scheduler/HangfireJobLoggingFilter.cs` |
| Health check | `web/Classes/HealthChecks/HangfireHealthCheck.cs` |
| Pause/resume API | `web/Areas/Scheduler/Controllers/JobsController.cs` |
| Service layer (pause/resume/reconcile) | `web/Areas/Scheduler/Services/SchedulerJobsService.cs` |
| Job abstraction | `web/Areas/Scheduler/Services/IScheduledJob.cs`, `ScheduledJobAttribute.cs`, `ScheduledJobDiscovery.cs`, `ScheduledJobRunner.cs` |
| Marker table entity + DDL | `web/Areas/Scheduler/Models/Entities/SchedulerJobState.cs`, `web/Classes/Scheduler/SchedulerSchemaInitializer.cs` |
