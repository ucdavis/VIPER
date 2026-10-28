namespace Viper.Areas.Students.Models;

public class CareerDropdownOption
{
    // Not required: this type is both the response shape and part of the save body, and a save
    // never reads the label.
    public string Label { get; set; } = string.Empty;
    public int? Value { get; set; }
    public bool IsOther { get; set; }
}
