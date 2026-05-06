namespace Viper.Classes.Scheduler;

/// <summary>
/// Bound from the <c>Hangfire</c> configuration section. Connection string
/// lives separately under <c>ConnectionStrings:Hangfire</c> (with fallback
/// to <c>ConnectionStrings:VIPER</c> if absent) so the scheduler schema can
/// be moved to a dedicated database without touching app code.
/// </summary>
public class HangfireOptions
{
    public const string SectionName = "Hangfire";

    /// <summary>
    /// Master toggle. When false, Hangfire is not registered and no
    /// background server starts. Defaults off so each environment opts in
    /// independently.
    /// </summary>
    public bool Enabled { get; set; }
}
