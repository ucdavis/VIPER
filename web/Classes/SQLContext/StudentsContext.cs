using Microsoft.EntityFrameworkCore;
using Viper.Models.Students;
using Viper.Areas.Students.Models.Entities;

namespace Viper.Classes.SQLContext;

public partial class VIPERContext : DbContext
{
    public DbSet<AaudStudent> AaudStudents { get; set; }
    public DbSet<StudentClassYear> StudentClassYears { get; set; }
    public DbSet<ClassYearLeftReason> ClassYearLeftReasons { get; set; }
    public DbSet<CareerOption> CareerOptions { get; set; }
    public DbSet<SpeciesOption> SpeciesOptions { get; set; }
    public DbSet<PostGradOption> PostGradOptions { get; set; }
    public DbSet<CareerSelection> CareerSelections { get; set; }

    partial void OnModelCreatingStudents(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StudentClassYear>(entity =>
        {
            entity.ToTable("StudentClassYear", "students");
            entity.HasOne(e => e.Student).WithMany().HasForeignKey(e => e.PersonId);
            entity.HasOne(e => e.ClassYearLeftReason).WithMany().HasForeignKey(e => e.LeftReason);
            entity.HasOne(e => e.AddedByPerson).WithMany().HasForeignKey(e => e.AddedBy);
            entity.HasOne(e => e.UpdatedByPerson).WithMany().HasForeignKey(e => e.UpdatedBy);
        });

        modelBuilder.Entity<ClassYearLeftReason>(entity =>
        {
            entity.ToTable("ClassYearLeftReason", "students");
        });

        modelBuilder.Entity<AaudStudent>(entity =>
        {
            entity.ToTable("vwStudents", "students");
            entity.HasKey(e => new { e.TermCode, e.SpridenId });
        });

        modelBuilder.Entity<CareerOption>(entity =>
        {
            entity.HasKey(e => e.CareerOptionId);
            entity.ToTable("CareerOption", "students");

            entity.Property(e => e.CareerOptionId).HasColumnName("CareerOptionId");
            // IsUnicode(false) on every string column: these are varchar in the schema, and without
            // it EF sends nvarchar parameters, which SQL Server resolves by converting the column
            // rather than the parameter - turning index seeks into scans.
            entity.Property(e => e.Career).HasColumnName("Career").HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.IsOther).HasColumnName("IsOther");
            entity.HasIndex(e => e.Career)
                .IsUnique();
        });

        modelBuilder.Entity<SpeciesOption>(entity =>
        {
            entity.HasKey(e => e.SpeciesOptionId);
            entity.ToTable("SpeciesOption", "students");

            entity.Property(e => e.SpeciesOptionId).HasColumnName("SpeciesOptionId");
            entity.Property(e => e.Species).HasColumnName("Species").HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.IsOther).HasColumnName("IsOther");
            entity.HasIndex(e => e.Species)
                .IsUnique();
        });

        modelBuilder.Entity<PostGradOption>(entity =>
        {
            entity.HasKey(e => e.PostGradOptionId);
            entity.ToTable("PostGradOption", "students");

            entity.Property(e => e.PostGradOptionId).HasColumnName("PostGradOptionId");
            entity.Property(e => e.PostGrad).HasColumnName("PostGrad").HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.IsOther).HasColumnName("IsOther");
            entity.HasIndex(e => e.PostGrad)
                .IsUnique();
        });

        modelBuilder.Entity<CareerSelection>(entity =>
        {
            entity.HasKey(e => e.CareerSelectionId);
            entity.ToTable("CareerSelection", "students");

            entity.Property(e => e.CareerSelectionId).HasColumnName("CareerSelectionId");
            entity.Property(e => e.Pidm).HasColumnName("Pidm");
            entity.Property(e => e.DateAdded).HasColumnName("DateAdded");
            entity.Property(e => e.DateModified).HasColumnName("DateModified");
            entity.Property(e => e.Career).HasColumnName("Career");
            // See the CareerOption block above for why every string column sets IsUnicode(false).
            entity.Property(e => e.CareerOther).HasColumnName("CareerOther").HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.FirstSpecies).HasColumnName("FirstSpecies");
            entity.Property(e => e.FirstSpeciesOther).HasColumnName("FirstSpeciesOther").HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.SecondSpecies).HasColumnName("SecondSpecies");
            entity.Property(e => e.SecondSpeciesOther).HasColumnName("SecondSpeciesOther").HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.PostGrad).HasColumnName("PostGrad");
            entity.Property(e => e.ShortTermStatement).HasColumnName("ShortTermStatement").HasMaxLength(5000).IsUnicode(false);
            entity.Property(e => e.LongTermStatement).HasColumnName("LongTermStatement").HasMaxLength(5000).IsUnicode(false);
            entity.Property(e => e.FacultyMothraId).HasColumnName("FacultyMothraId").HasMaxLength(8).IsUnicode(false);
            // Restrict, not EF's default ClientSetNull for an optional key: deleting an option that
            // is in use must fail, rather than silently clearing the answer from any selection
            // that happens to be tracked at the time.
            entity.HasOne(e => e.CareerOption)
                .WithMany()
                .HasForeignKey(e => e.Career)
                .HasPrincipalKey(e => e.CareerOptionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.FirstSpeciesOption)
                .WithMany()
                .HasForeignKey(e => e.FirstSpecies)
                .HasPrincipalKey(e => e.SpeciesOptionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.SecondSpeciesOption)
                .WithMany()
                .HasForeignKey(e => e.SecondSpecies)
                .HasPrincipalKey(e => e.SpeciesOptionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.PostGradOption)
                .WithMany()
                .HasForeignKey(e => e.PostGrad)
                .HasPrincipalKey(e => e.PostGradOptionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.Pidm)
                .IsUnique();
        });
    }
}
