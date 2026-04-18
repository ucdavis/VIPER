using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Students.Models;

public class ContactInfoDto
{
    public string? Name { get; set; }
    public string? Relationship { get; set; }
    public string? WorkPhone { get; set; }
    public string? HomePhone { get; set; }
    public string? CellPhone { get; set; }

    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string? Email
    {
        get => _email;
        set => _email = string.IsNullOrWhiteSpace(value) ? null : value;
    }
    private string? _email;
}
