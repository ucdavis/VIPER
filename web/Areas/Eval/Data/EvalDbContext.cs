using Microsoft.EntityFrameworkCore;

namespace Viper.Areas.Eval.Data;

/// <summary>
/// Entity Framework DbContext for the Eval database.
/// </summary>
public class EvalDbContext : DbContext
{
    public EvalDbContext(DbContextOptions<EvalDbContext> options) : base(options)
    {
    }
}
