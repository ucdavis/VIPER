using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class AaudUser
{
    public int AaudUserId { get; set; }

    public string ClientId { get; set; } = null!;

    public string MothraId { get; set; } = null!;

    public string? LoginId { get; set; }

    public string? MailId { get; set; }

    public string? SpridenId { get; set; }

    public string? Pidm { get; set; }

    public string? EmployeeId { get; set; }

    public int? VmacsId { get; set; }

    public string? VmcasId { get; set; }

    public string? UnexId { get; set; }

    public int? MivId { get; set; }

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string DisplayLastName { get; set; } = null!;

    public string DisplayFirstName { get; set; } = null!;

    public string? DisplayMiddleName { get; set; }

    public string DisplayFullName { get; set; } = null!;

    public bool CurrentStudent { get; set; }

    public bool FutureStudent { get; set; }

    public bool CurrentEmployee { get; set; }

    public bool FutureEmployee { get; set; }

    public int? StudentTerm { get; set; }

    public int? EmployeeTerm { get; set; }

    public string? PpsId { get; set; }

    public string? StudentPKey { get; set; }

    public string? EmployeePKey { get; set; }

    public int Current { get; set; }

    public int Future { get; set; }

    public string? IamId { get; set; }

    public bool? Ross { get; set; }

    public DateTime? Added { get; set; }

    public ExampleComment? ExampleComment { get; set; }
}
