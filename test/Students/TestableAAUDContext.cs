using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.test.Students;

/// <summary>
/// AAUDContext subclass that maps keyless views as tables with keys,
/// enabling InMemory provider to store test data.
/// </summary>
internal class TestableAAUDContext : AAUDContext
{
    public TestableAAUDContext(DbContextOptions<AAUDContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<VwDvmStudentsMaxTerm>(entity =>
        {
            entity.HasKey(e => e.IdsMothraId);
            entity.ToTable("VwDvmStudentsMaxTerm");
        });
    }
}
