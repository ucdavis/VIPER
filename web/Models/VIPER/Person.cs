﻿using Viper.Models.Students;

namespace Viper.Models.VIPER
{
    public partial class Person
    {
        public int PersonId { get; set; }
        public string ClientId { get; set; } = null!;
        public string? IamId { get; set; }
        public string MothraId { get; set; } = null!;
        public string? LoginId { get; set; }
        public string? MailId { get; set; }
        public string? SpridenId { get; set; }
        public string? EmployeeId { get; set; }
        public string? PpsId { get; set; }
        public int? VmacsId { get; set; }
        public string? VmcasId { get; set; }
        public string? UnexId { get; set; }
        public int? MivId { get; set; }
        public string LastName { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public bool CurrentStudent { get; set; }
        public bool FutureStudent { get; set; }
        public bool CurrentEmployee { get; set; }
        public bool FutureEmployee { get; set; }
        public int Current { get; set; }
        public int Future { get; set; }
        public int? StudentTerm { get; set; }
        public int? EmployeeTerm { get; set; }
        public bool? Ross { get; set; }
        public DateTime? Added { get; set; }
        public DateTime? Inactivated { get; set; }

        public AaudStudent? StudentInfo { get; set; }
        public IEnumerable<AaudStudent>? StudentHistory { get; set; }
    }
}
