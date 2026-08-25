using Microsoft.EntityFrameworkCore;
using Viper.Areas.Personnel.Models;

namespace Viper.Areas.Personnel;

/// <summary>
/// Entity Framework DbContext for the Personnel phone numbers system.
/// All tables are in the [phones] schema in the VIPER database.
/// </summary>
public class PhonesDbContext : DbContext
{
    public PhonesDbContext(DbContextOptions<PhonesDbContext> options) : base(options)
    {
    }

    // Core data tables
    public virtual DbSet<PhonePerson> PhonePerson { get; set; }
    public virtual DbSet<SVMSection> SVMSection { get; set; }
    public virtual DbSet<SVMUnit> SVMUnit { get; set; }
    public virtual DbSet<SVMUnitPerson> SVMUnitPerson { get; set; }
    public virtual DbSet<SVMFrequentNumber> SVMFrequentNumber { get; set; }
    public virtual DbSet<PhoneList> PhoneList { get; set; }
    public virtual DbSet<PhoneListUnit> PhoneListUnit { get; set; }
    public virtual DbSet<PhoneListUnitPerson> PhoneListUnitPerson { get; set; }

    // Read-only cross-schema reference (users schema in same database)
    public virtual DbSet<ViperPerson> ViperPerson { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PhonePerson (phones.Person)
        modelBuilder.Entity<PhonePerson>(entity =>
        {
            entity.HasKey(e => e.PersonIam);
            entity.ToTable("Person", schema: "phones");

            entity.Property(e => e.PersonIam).HasColumnName("PersonIam");
            entity.Property(e => e.Phone).HasColumnName("Phone").HasMaxLength(25);
            entity.Property(e => e.DirectPhone).HasColumnName("DirectPhone").HasMaxLength(25);
            entity.Property(e => e.Office).HasColumnName("Office").HasMaxLength(100);
            entity.Property(e => e.ModifiedDate).HasColumnName("ModifiedDate");
            entity.Property(e => e.ModifiedBy).HasColumnName("ModifiedBy");

            // Cross-schema FK to users.Person
            entity.HasOne(e => e.ViperPerson)
                .WithMany()
                .HasForeignKey(e => e.PersonIam)
                .HasPrincipalKey(e => e.IamId)
                .IsRequired(true);

            entity.HasOne(e => e.ViperModPerson)
                .WithMany()
                .HasForeignKey(e => e.ModifiedBy)
                .HasPrincipalKey(e => e.IamId);
        });

        // SVMSection (phones.SVMSection)
        modelBuilder.Entity<SVMSection>(entity =>
        {
            entity.HasKey(e => e.SectionId);
            entity.ToTable("SVMSection", schema: "phones");

            entity.Property(e => e.SectionId).HasColumnName("SectionId");
            entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(100);
            entity.Property(e => e.IncludeAbbrv).HasColumnName("IncludeAbbrv");
            entity.Property(e => e.UnitName).HasColumnName("UnitName").HasMaxLength(50);
            entity.Property(e => e.DirectorTitle).HasColumnName("DirectorTitle").HasMaxLength(50);
            entity.Property(e => e.SortOrder).HasColumnName("SortOrder");
        });

        // SVMUnit (phones.SVMUnit)
        modelBuilder.Entity<SVMUnit>(entity =>
        {
            entity.HasKey(e => e.UnitId);
            entity.ToTable("SVMUnit", schema: "phones");

            entity.Property(e => e.UnitId).HasColumnName("UnitId");
            entity.Property(e => e.SectionId).HasColumnName("SectionId");
            entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(100);
            entity.Property(e => e.Fax).HasColumnName("Fax").HasMaxLength(25);
            entity.Property(e => e.Abbrv).HasColumnName("Abbrv").HasMaxLength(20);
            entity.Property(e => e.SortOrder).HasColumnName("SortOrder");
            entity.Property(e => e.ModifiedDate).HasColumnName("ModifiedDate");
            entity.Property(e => e.ModifiedBy).HasColumnName("ModifiedBy").HasMaxLength(10);

            entity.HasOne(e => e.Section)
                .WithMany(s => s.Units)
                .HasForeignKey(e => e.SectionId);

            entity.HasOne(e => e.ViperModPerson)
                .WithMany()
                .HasForeignKey(e => e.ModifiedBy)
                .HasPrincipalKey(e => e.IamId);
        });

        // SVMUnitPerson (phones.SVMUnitPerson)
        modelBuilder.Entity<SVMUnitPerson>(entity =>
        {
            entity.HasKey(e => e.UnitPersonId);
            entity.ToTable("SVMUnitPerson", schema: "phones");

            entity.Property(e => e.UnitPersonId).HasColumnName("UnitPersonId");
            entity.Property(e => e.UnitId).HasColumnName("UnitId");
            entity.Property(e => e.PersonIam).HasColumnName("PersonIam").HasMaxLength(10);
            entity.Property(e => e.Office).HasColumnName("Office").HasMaxLength(50);
            entity.Property(e => e.PosType).HasColumnName("PosType").HasMaxLength(25);
            entity.Property(e => e.Interim).HasColumnName("Interim").HasMaxLength(10);
            entity.Property(e => e.ModifiedDate).HasColumnName("ModifiedDate");
            entity.Property(e => e.ModifiedBy).HasColumnName("ModifiedBy").HasMaxLength(10);
            entity.Property(e => e.IsActive).HasColumnName("IsActive");

            entity.HasOne(e => e.Unit)
                .WithMany(s => s.UnitPersons)
                .HasForeignKey(e => e.UnitId);

            entity.HasOne(e => e.Person)
                .WithMany(s => s.UnitPersons)
                .HasForeignKey(e => e.PersonIam);

            entity.HasOne(e => e.ViperModPerson)
                .WithMany()
                .HasForeignKey(e => e.ModifiedBy)
                .HasPrincipalKey(e => e.IamId);
        });

        // SVMFrequentNumber (phones.SVMFrequentNumber)
        modelBuilder.Entity<SVMFrequentNumber>(entity =>
        {
            entity.HasKey(e => e.NumberId);
            entity.ToTable("SVMFrequentNumber", schema: "phones");

            entity.Property(e => e.NumberId).HasColumnName("NumberId");
            entity.Property(e => e.Label).HasColumnName("Label").HasMaxLength(100);
            entity.Property(e => e.Phone).HasColumnName("Phone").HasMaxLength(25);
            entity.Property(e => e.SortOrder).HasColumnName("SortOrder");
            entity.Property(e => e.ModifiedDate).HasColumnName("ModifiedDate");
            entity.Property(e => e.ModifiedBy).HasColumnName("ModifiedBy").HasMaxLength(10);
            entity.Property(e => e.IsActive).HasColumnName("IsActive");

            entity.HasOne(e => e.ViperModPerson)
                .WithMany()
                .HasForeignKey(e => e.ModifiedBy)
                .HasPrincipalKey(e => e.IamId);
        });

        // PhoneList (phones.PhoneList)
        modelBuilder.Entity<PhoneList>(entity =>
        {
            entity.HasKey(e => e.PhoneListId);
            entity.ToTable("PhoneList", schema: "phones");

            entity.Property(e => e.Code).HasColumnName("Code").HasMaxLength(20);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(100);
            entity.Property(e => e.MaintainRole).HasColumnName("MaintainRole").HasMaxLength(100);
        });

        // PhoneListUnit (phones.PhoneListUnit)
        modelBuilder.Entity<PhoneListUnit>(entity =>
        {
            entity.HasKey(e => e.PhoneListUnitId);
            entity.ToTable("PhoneListUnit", schema: "phones");

            entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(100);
            entity.Property(e => e.PhoneListId).HasColumnName("PhoneListId");
            entity.Property(e => e.SortOrder).HasColumnName("SortOrder");

            entity.HasOne(e => e.PhoneList)
                .WithMany(s => s.PhoneListUnits)
                .HasForeignKey(e => e.PhoneListId);
        });

        // PhoneListUnitPerson (phones.PhoneListUnitPerson)
        modelBuilder.Entity<PhoneListUnitPerson>(entity =>
        {
            entity.HasKey(e => e.PhoneListUnitPersonId);
            entity.ToTable("PhoneListUnitPerson", schema: "phones");

            entity.Property(e => e.PhoneListUnitId).HasColumnName("PhoneListUnitId");
            entity.Property(e => e.PersonIam).HasColumnName("PersonIam");
            entity.Property(e => e.ListFirst).HasColumnName("ListFirst");
            entity.Property(e => e.IsActive).HasColumnName("IsActive");
            entity.Property(e => e.ModifiedBy).HasColumnName("ModifiedBy").HasMaxLength(10);
            entity.Property(e => e.ModifiedDate).HasColumnName("ModifiedDate");

            entity.HasOne(e => e.PhoneListUnit)
                .WithMany(s => s.PhoneListUnitPersons)
                .HasForeignKey(e => e.PhoneListUnitId);

            entity.HasOne(e => e.Person)
                .WithMany(s => s.PhoneListUnitPersons)
                .HasForeignKey(e => e.PersonIam);

            entity.HasOne(e => e.ViperModPerson)
                .WithMany()
                .HasForeignKey(e => e.ModifiedBy)
                .HasPrincipalKey(e => e.IamId);
        });

        // ViperPerson (users.Person) - read-only cross-schema reference
        modelBuilder.Entity<ViperPerson>(entity =>
        {
            entity.HasKey(e => e.PersonId);
            entity.ToTable("Person", schema: "users");

            entity.Property(e => e.FirstName).HasMaxLength(30);
            entity.Property(e => e.LastName).HasMaxLength(60);
            entity.Property(e => e.FullName).HasMaxLength(91);
            entity.Property(e => e.IamId).HasMaxLength(10);
            entity.Property(e => e.MailId);
            entity.Property(e => e.CurrentEmployee);
        });
    }
}
