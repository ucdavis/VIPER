namespace Viper.Areas.Scheduler.Models.DTOs.Responses;

/// <summary>
/// Counters for one reconciler pass. Surfaces drift in dashboards/logs without
/// inspecting Hangfire and the marker table directly.
/// </summary>
public class ReconcilerOutcomeDto
{
    /// <summary>Both marker present AND Hangfire registration found - registration was removed.</summary>
    public int SplitBrainHealed { get; set; }

    /// <summary>
    /// Marker absent + Hangfire registration absent for a job declared via
    /// <c>[ScheduledJob]</c> - the recurring registration was re-created.
    /// Catches drift after a failed deploy or manual dashboard removal.
    /// </summary>
    public int LostRegistrationsHealed { get; set; }

    /// <summary>Marker referenced a __scheduler:* id - illegal; row was deleted.</summary>
    public int SystemMarkersDeleted { get; set; }

    /// <summary>Marker present + no registration; the normal paused state.</summary>
    public int CorrectlyPaused { get; set; }

    /// <summary>Registration present + no marker; the normal active state.</summary>
    public int CorrectlyActive { get; set; }

    /// <summary>Total markers inspected this pass.</summary>
    public int MarkersExamined { get; set; }

    /// <summary>Total Hangfire recurring jobs inspected this pass.</summary>
    public int RegistrationsExamined { get; set; }
}
