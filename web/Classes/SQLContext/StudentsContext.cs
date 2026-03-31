using Microsoft.EntityFrameworkCore;
using Viper.Models.Students;

namespace Viper.Classes.SQLContext;

public partial class VIPERContext : DbContext
{
    public DbSet<AaudStudent> AaudStudents { get; set; }
    public DbSet<StudentClassYear> StudentClassYears { get; set; }
    public DbSet<ClassYearLeftReason> ClassYearLeftReasons { get; set; }

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
    }
}
