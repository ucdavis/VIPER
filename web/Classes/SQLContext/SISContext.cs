using Microsoft.EntityFrameworkCore;
using Viper.Models.SIS;

namespace Viper.Classes.SQLContext;

public class SISContext : DbContext
{
    public SISContext(DbContextOptions<SISContext> options) : base(options)
    {
    }

    public DbSet<StudentDesignation> StudentDesignations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (HttpHelper.Settings != null)
        {
            optionsBuilder.UseSqlServer(HttpHelper.Settings["ConnectionStrings:SIS"]);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StudentDesignation>(entity =>
        {
            entity.ToTable("studentDesignation", "dbo");
            entity.HasKey(e => e.DesignationId);
        });
    }
}
