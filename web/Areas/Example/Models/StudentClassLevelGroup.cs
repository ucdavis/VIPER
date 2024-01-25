using Viper.Models.AAUD;

namespace Viper.Areas.Example.Models
{
    public class StudentClassLevelGroup
    {
        public string Pidm { get; set; } = null!;
        public string IamId { get; set; } = null!;
        public string MailId { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string MiddleName { get; set; } = null!;

        public Student? Student { get; set; }
        public Studentgrp? Studentgrp { get; set; }
    }
}
