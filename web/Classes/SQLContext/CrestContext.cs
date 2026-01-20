using Microsoft.EntityFrameworkCore;
using Viper.Models.Crest;

namespace Viper.Classes.SQLContext;

/// <summary>
/// Context for read-only access to the CREST database.
/// Used by the Effort harvest process to extract instructor-course assignments.
/// </summary>
public class CrestContext : DbContext
{
    public CrestContext(DbContextOptions<CrestContext> options) : base(options)
    {
    }

    public virtual DbSet<CrestBlock> Blocks { get; set; }
    public virtual DbSet<CrestCourseSessionOffering> CourseSessionOfferings { get; set; }
    public virtual DbSet<EdutaskOfferPerson> EdutaskOfferPersons { get; set; }
    public virtual DbSet<EdutaskPerson> EdutaskPersons { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (HttpHelper.Settings != null && !optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(HttpHelper.Settings["ConnectionStrings:CREST"]);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CrestCourseSessionOffering>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_vm_course_session_offering", schema: "dbo");

            entity.Property(e => e.CourseId).HasColumnName("courseID");
            entity.Property(e => e.EdutaskOfferId).HasColumnName("edutaskofferid");
            entity.Property(e => e.AcademicYear).HasColumnName("academicYear");
            entity.Property(e => e.Crn).HasColumnName("crn");
            entity.Property(e => e.SsaCourseNum).HasColumnName("ssaCourseNum");
            entity.Property(e => e.SessionType).HasColumnName("sessionType");
            entity.Property(e => e.SeqNumb).HasColumnName("seqNumb");
            entity.Property(e => e.FromDate).HasColumnName("fromdate");
            entity.Property(e => e.FromTime).HasColumnName("fromtime");
            entity.Property(e => e.ThruDate).HasColumnName("thrudate");
            entity.Property(e => e.ThruTime).HasColumnName("thrutime");
        });

        modelBuilder.Entity<EdutaskOfferPerson>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("tbl_EdutaskOfferPerson", schema: "dbo");

            entity.Property(e => e.EdutaskOfferId).HasColumnName("edutaskofferID");
            entity.Property(e => e.PersonId).HasColumnName("personID");
        });

        modelBuilder.Entity<EdutaskPerson>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("tbl_EdutaskPerson", schema: "dbo");

            entity.Property(e => e.EdutaskId).HasColumnName("edutaskID");
            entity.Property(e => e.PersonId).HasColumnName("personID");
            entity.Property(e => e.RoleCode).HasColumnName("rolecode");
        });

        modelBuilder.Entity<CrestBlock>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("tbl_Block", schema: "dbo");

            entity.Property(e => e.EdutaskId).HasColumnName("edutaskID");
            entity.Property(e => e.AcademicYear).HasColumnName("academicYear");
            entity.Property(e => e.SsaCourseNum).HasColumnName("ssaCourseNum");
        });
    }
}
