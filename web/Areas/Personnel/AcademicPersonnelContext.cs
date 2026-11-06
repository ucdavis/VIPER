using Microsoft.EntityFrameworkCore;

namespace Viper.Areas.Personnel;

/// <summary>
/// The AcademicPersonnel database behind the Employee Information System (EIS). It maps no
/// tables: EIS reads it only through the legacy <c>usp_eis_*</c> stored procedures with
/// <c>Database.SqlQuery</c>, so the pages show what the ColdFusion EIS shows while both run.
/// </summary>
public class AcademicPersonnelContext : DbContext
{
    public AcademicPersonnelContext(DbContextOptions<AcademicPersonnelContext> options) : base(options)
    {
    }
}
