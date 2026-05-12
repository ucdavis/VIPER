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
        await roleViews.UpdateRoles(modBy: context.ModBy, debugOnly: false, ct: ct);
    }
}
```

### 2. Naming rules

| Field | Rule |
|---|---|
| `id` | `area:job-name` (e.g. `raps:role-refresh`). |
| `cron` | Five-field Hangfire cron (`m h dom mon dow`). |
| `TimeZoneId` | Defaults to `Pacific Standard Time`. UC Davis runs Windows; IANA aliases like `America/Los_Angeles` also work. |

### 3. Stamping audit rows

Background jobs run with no HTTP context, so `UserHelper.GetCurrentUser()`
is **not available**. Every job receives a `ScheduledJobContext` whose
`ModBy` property is the audit actor for this run — pass it through to
your service layer; do not derive it inside the job.

`ModBy` is `"__sched"` (7 chars; the legacy `tblRoleMembers.ModBy`
column is `varchar(8)`, so the stamp is shortened to fit while staying
distinct from the existing `"__system"` convention). Existing audit
queries can filter on `WHERE ModBy = '__sched'` to isolate
scheduler-driven changes from human-driven changes.

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

---

## Configuration

All settings live in `appsettings.{Environment}.json` (or AWS SSM
parameters in deployed environments).

| Key | Purpose | Default |
|---|---|---|
| `Hangfire:Enabled` | Master switch. When `false`, no scheduler wiring runs and the dashboard is unreachable. | `true` |
| `Hangfire:AutoSchedule` | When `false`, recurring jobs register with `Cron.Never` so cron never fires. The worker still runs and the dashboard still mounts, so operators can fire jobs via "Trigger now" or `BackgroundJob.Enqueue`. Local dev sets this `false` to require manual triggering. | `true` |
| `ConnectionStrings:VIPER` | The database that hosts Hangfire's tables. Required when `Hangfire:Enabled=true`. | n/a |
| `IPAddressAllowlistConfiguration:InternalAllowlist` | Source-IP gate for `/health/detail` and the HealthChecks UI. Add SVM infra ranges + your office subnet. | localhost only |

The dashboard does **not** read this config; it is always mounted at
`/scheduler/dashboard` when Hangfire is enabled and is gated by RAPS,
not IP.

---

## Access

| Surface | URL | Auth |
|---|---|---|
| Hangfire dashboard | `/scheduler/dashboard` | Cookie auth (CAS) + RAPS permission `SVMSecure.CATS.scheduledJobs` |
| Health (liveness) | `/health` | Anonymous (Jenkins polls it) |
| Health (detail) | `/health/detail` | IP-allowlisted to `InternalAllowlist` |

`SVMSecure.CATS.scheduledJobs` is the same permission the legacy
ColdFusion VIPER scheduler (`cats/inc_scheduledTasks.cfm`) checks &mdash;
admins who already manage the legacy scheduler inherit access without a
provisioning step.

### Dashboard add-ons

Two Hangfire dashboard plugins enrich the operator experience:

- **Hangfire.Console** &mdash; per-job console output appears inline on the
  job's detail page. Jobs call `context.WriteLine(...)` on the
  `ScheduledJobContext` they receive; the output is captured in storage
  and rendered in the dashboard.
- **Hangfire.Heartbeat** &mdash; CPU, memory, and uptime metrics for each
  registered worker are shown on the dashboard's Servers page (refreshed
  every 30 s). Useful for spotting a stuck worker before users do.

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
   empty"`.
4. **Recent deploys** &mdash; a job that disappeared after a deploy is
   re-registered on the next app startup (idempotent `AddOrUpdate`).
   Restart the app to force it.

---

## Related code

| Concern | Location |
|---|---|
| Hangfire wiring (DI, dashboard mount) | `web/Classes/Scheduler/HangfireExtensions.cs` |
| Dashboard auth filter | `web/Classes/Scheduler/HangfireDashboardAuthorizationFilter.cs` |
| Per-job logging filter | `web/Classes/Scheduler/HangfireJobLoggingFilter.cs` |
| Health check | `web/Classes/HealthChecks/HangfireHealthCheck.cs` |
| Job abstraction | `web/Areas/Scheduler/Services/IScheduledJob.cs`, `ScheduledJobAttribute.cs`, `ScheduledJobDiscovery.cs`, `ScheduledJobRunner.cs` |
