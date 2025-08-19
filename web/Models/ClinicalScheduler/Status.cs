namespace Viper.Models.ClinicalScheduler
{
    /// <summary>
    /// Represents the configuration status for a specific graduation year, including scheduling windows,
    /// stream CRNs, and various flags for default and publishing settings.
    /// </summary>
    public class Status
    {
        /// <summary>
        /// The graduation year this status applies to.
        /// </summary>
        public int GradYear { get; set; }

        /// <summary>
        /// The date when scheduling opens for this graduation year.
        /// </summary>
        public DateTime OpenDate { get; set; }

        /// <summary>
        /// The date when scheduling closes for this graduation year.
        /// </summary>
        public DateTime CloseDate { get; set; }

        /// <summary>
        /// The number of weeks in the scheduling period.
        /// </summary>
        public int NumWeeks { get; set; }

        /// <summary>
        /// The date when external request submissions open.
        /// </summary>
        public DateTime ExtRequestOpen { get; set; }

        /// <summary>
        /// The deadline for external request submissions.
        /// </summary>
        public DateTime ExtRequestDeadline { get; set; }

        /// <summary>
        /// The date when external request submissions may reopen.
        /// </summary>
        public DateTime ExtRequestReopen { get; set; }

        /// <summary>
        /// The CRN (Course Reference Number) for the SA (Stream A) stream.
        /// </summary>
        public string SAStreamCrn { get; set; } = string.Empty;

        /// <summary>
        /// The CRN (Course Reference Number) for the LA (Stream B) stream.
        /// </summary>
        public string LAStreamCrn { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether this graduation year is the default.
        /// </summary>
        public bool DefaultGradYear { get; set; }

        /// <summary>
        /// Indicates whether this year is the default selection year for scheduling.
        /// </summary>
        public bool DefaultSelectionYear { get; set; }

        /// <summary>
        /// Indicates whether the schedule for this graduation year is published.
        /// </summary>
        public bool PublishSchedule { get; set; }
    }
}
