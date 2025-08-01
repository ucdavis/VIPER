using Microsoft.EntityFrameworkCore;
using Viper.Classes;
using Viper.Models.CTS;

namespace Viper.Classes.SQLContext;

public class ClinicalSchedulerContext : DbContext
{
    public ClinicalSchedulerContext(DbContextOptions<ClinicalSchedulerContext> options) : base(options)
    {
    }

    public virtual DbSet<Rotation> Rotations { get; set; }
    public virtual DbSet<Service> Services { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (HttpHelper.Settings != null)
        {
            optionsBuilder.UseSqlServer(HttpHelper.Settings["ConnectionStrings:VIPER"]);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Rotation>(entity =>
        {
            entity.HasKey(e => e.RotId);
            entity.ToTable("vwRotation", schema: "cts");
            entity.Property(e => e.SubjectCode).IsRequired(false);
            entity.Property(e => e.CourseNumber).IsRequired(false);
            entity.HasOne(e => e.Service).WithMany(s => s.Rotations)
               .HasForeignKey(e => e.ServiceId)
               .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId);
            entity.ToTable("vwService", schema: "cts");
        });
    }
}