using Microsoft.EntityFrameworkCore;
using Viper.Areas.Students.Models.Entities;
using Viper.Models.SIS;

namespace Viper.Classes.SQLContext;

public class SISContext : DbContext
{
    public SISContext(DbContextOptions<SISContext> options) : base(options)
    {
    }

    public DbSet<StudentDesignation> StudentDesignations { get; set; }
    public DbSet<StudentContact> StudentContacts { get; set; }
    public DbSet<StudentEmergencyContact> StudentEmergencyContacts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StudentDesignation>(entity =>
        {
            entity.ToTable("studentDesignation", "dbo");
            entity.HasKey(e => e.DesignationId);
        });

        modelBuilder.Entity<StudentContact>(entity =>
        {
            entity.ToTable("studentContact", "dbo");
            entity.HasKey(e => e.StdContactId);
            entity.Property(e => e.StdContactId).HasColumnName("stdContact_id");
            entity.Property(e => e.Pidm).HasColumnName("pidm");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.City).HasColumnName("city");
            entity.Property(e => e.Zip).HasColumnName("zip");
            entity.Property(e => e.HomePhone).HasColumnName("home");
            entity.Property(e => e.CellPhone).HasColumnName("cell");
            entity.Property(e => e.ContactPermanent).HasColumnName("contact_permanent");
            entity.Property(e => e.LastUpdated).HasColumnName("lastUpdated");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedBy");
        });

        modelBuilder.Entity<StudentEmergencyContact>(entity =>
        {
            entity.ToTable("emergencyContact", "dbo");
            entity.HasKey(e => e.EmContactId);
            entity.Property(e => e.EmContactId).HasColumnName("emContact_id");
            entity.Property(e => e.StdContactId).HasColumnName("stdContact_id");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Relationship).HasColumnName("relationship");
            entity.Property(e => e.WorkPhone).HasColumnName("work");
            entity.Property(e => e.HomePhone).HasColumnName("home");
            entity.Property(e => e.CellPhone).HasColumnName("cell");
            entity.Property(e => e.Email).HasColumnName("email");

            entity.HasOne(e => e.StudentContact)
                .WithMany(c => c.EmergencyContacts)
                .HasForeignKey(e => e.StdContactId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
