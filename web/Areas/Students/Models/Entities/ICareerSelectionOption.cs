namespace Viper.Areas.Students.Models.Entities;

/// <summary>
/// A career selection dropdown option. Implemented by every option table so the catch-all row
/// can be found, sorted and protected without matching on its label, and so the tables can be
/// managed through one code path despite naming their id and label columns differently.
/// </summary>
public interface ICareerSelectionOption
{
    bool IsOther { get; }

    // Implemented explicitly on each entity, which keeps EF from mapping them as extra columns.
    // They are for in-memory use only: EF cannot translate them into a query.
    int Id { get; }
    string Label { get; set; }
}
