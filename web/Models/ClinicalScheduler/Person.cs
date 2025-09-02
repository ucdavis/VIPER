namespace Viper.Models.ClinicalScheduler
{
    /// <summary>
    /// Represents a person in the Clinical Scheduler system.
    /// This class maps to the <c>vPerson</c> database view and is used to encapsulate
    /// identifying and display information for individuals within the scheduling context.
    /// </summary>
    public class Person
    {
        /// <summary>
        /// The MothraId (unique identifier) for this person.
        /// </summary>
        public string IdsMothraId { get; set; } = null!;

        /// <summary>
        /// The person's full display name.
        /// </summary>
        public string PersonDisplayFullName { get; set; } = null!;

        /// <summary>
        /// The person's last name for display purposes.
        /// </summary>
        public string PersonDisplayLastName { get; set; } = null!;

        /// <summary>
        /// The person's first name for display purposes.
        /// </summary>
        public string PersonDisplayFirstName { get; set; } = null!;

        /// <summary>
        /// The person's email address (optional).
        /// </summary>
        public string? IdsMailId { get; set; } = null;
    }
}
