using Microsoft.EntityFrameworkCore;
using Viper.Models.Dictionary;

namespace Viper.Classes.SQLContext;

/// <summary>
/// Context for read-only access to the dictionary database.
/// Contains reference data like title codes and department mappings.
/// </summary>
public class DictionaryContext : DbContext
{
    public DictionaryContext(DbContextOptions<DictionaryContext> options) : base(options)
    {
    }

    public virtual DbSet<DvtTitle> Titles { get; set; }
    public virtual DbSet<DvtSvmUnit> SvmUnits { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (HttpHelper.Settings != null && !optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(HttpHelper.Settings["ConnectionStrings:Dictionary"]);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DvtTitle>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("dvtTitle", schema: "dbo");

            entity.Property(e => e.Code).HasColumnName("dvtTitle_Code");
            entity.Property(e => e.Name).HasColumnName("dvtTitle_name");
            entity.Property(e => e.Abbreviation).HasColumnName("dvtTitle_Abbrv");
            entity.Property(e => e.JobGroupId).HasColumnName("dvtTitle_JobGroupID");
            entity.Property(e => e.JobGroupName).HasColumnName("dvtTitle_JobGroup_Name");
        });

        modelBuilder.Entity<DvtSvmUnit>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("dvtSVMUnit", schema: "dbo");

            entity.Property(e => e.Code).HasColumnName("dvtSVMUnit_code");
            entity.Property(e => e.SimpleName).HasColumnName("dvtSVMUnit_name_simple");
            entity.Property(e => e.ParentId).HasColumnName("dvtSvmUnit_Parent_ID");
        });
    }
}
