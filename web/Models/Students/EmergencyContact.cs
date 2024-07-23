﻿namespace Viper.Models.Students
{
    public class EmergencyContact
    {
        public int EmContactId { get; set; }
        public int StdContactId { get; set; }
        public string Type { get; set; } = null!;
        public string? Name { get; set; }
        public string? Relationship { get; set; }
        public string? Work { get; set; }
        public string? Home { get; set; }
        public string? Cell { get; set; }
        public string? Email { get; set; }

        public virtual StudentContact? StudentContact { get; set; }
    }
}
