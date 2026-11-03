using Microsoft.EntityFrameworkCore;
using Viper.Areas.ReportBuilder.Models.Entities;

namespace Viper.Areas.ReportBuilder;

/// <summary>
/// The Report Builder's own tables, in the VIPER database's [reportbuilder] schema. Report data
/// itself comes from the [reports] schema and is never mapped here.
/// </summary>
public class ReportBuilderDbContext : DbContext
{
    internal const string Schema = "reportbuilder";

    /// <summary>The allow-listed schema report views and procedures live in.</summary>
    internal const string SourceSchema = "reports";

    private const int EnumLength = 30;
    private const int NameLength = 128;
    private const int LabelLength = 200;
    private const int UserLength = 50;
    private const int AreaLength = 50;

    public ReportBuilderDbContext(DbContextOptions<ReportBuilderDbContext> options) : base(options)
    {
    }

    public virtual DbSet<ReportDefinition> ReportDefinitions { get; set; }
    public virtual DbSet<ReportPermission> ReportPermissions { get; set; }
    public virtual DbSet<ReportColumn> ReportColumns { get; set; }
    public virtual DbSet<ReportParameter> ReportParameters { get; set; }
    public virtual DbSet<ReportDefinitionHistory> ReportDefinitionHistory { get; set; }
    public virtual DbSet<ReportRunLog> ReportRunLog { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ReportDefinition>(entity =>
        {
            entity.ToTable("ReportDefinition", Schema);
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Slug).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(LabelLength);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Area).HasMaxLength(AreaLength);
            entity.Property(e => e.SourceName).HasMaxLength(NameLength);
            entity.Property(e => e.SourceKind).HasConversion<string>().HasMaxLength(EnumLength);
            entity.Property(e => e.DefaultSortColumn).HasMaxLength(NameLength);
            entity.Property(e => e.SensitiveNotice).HasMaxLength(1000);
            entity.Property(e => e.CreatedBy).HasMaxLength(UserLength);
            entity.Property(e => e.ModifiedBy).HasMaxLength(UserLength);
            entity.HasMany(e => e.Permissions).WithOne(e => e.ReportDefinition).HasForeignKey(e => e.ReportDefinitionId);
            entity.HasMany(e => e.Columns).WithOne(e => e.ReportDefinition).HasForeignKey(e => e.ReportDefinitionId);
            entity.HasMany(e => e.Parameters).WithOne(e => e.ReportDefinition).HasForeignKey(e => e.ReportDefinitionId);
        });

        modelBuilder.Entity<ReportPermission>(entity =>
        {
            entity.ToTable("ReportPermission", Schema);
            entity.HasKey(e => new { e.ReportDefinitionId, e.Permission });
            entity.Property(e => e.Permission).HasMaxLength(100);
        });

        modelBuilder.Entity<ReportColumn>(entity =>
        {
            entity.ToTable("ReportColumn", Schema);
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ReportDefinitionId, e.SourceColumn }).IsUnique();
            entity.Property(e => e.SourceColumn).HasMaxLength(NameLength);
            entity.Property(e => e.Label).HasMaxLength(LabelLength);
            entity.Property(e => e.Format).HasConversion<string>().HasMaxLength(EnumLength);
        });

        modelBuilder.Entity<ReportParameter>(entity =>
        {
            entity.ToTable("ReportParameter", Schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(NameLength);
            entity.Property(e => e.Label).HasMaxLength(LabelLength);
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(EnumLength);
            entity.Property(e => e.Operator).HasConversion<string>().HasMaxLength(EnumLength);
            entity.Property(e => e.DefaultValue).HasMaxLength(500);
            entity.Property(e => e.ChoicesSource).HasMaxLength(NameLength);
            entity.Property(e => e.Binding).HasConversion<string>().HasMaxLength(EnumLength);
        });

        modelBuilder.Entity<ReportDefinitionHistory>(entity =>
        {
            entity.ToTable("ReportDefinitionHistory", Schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasConversion<string>().HasMaxLength(EnumLength);
            entity.Property(e => e.ChangedBy).HasMaxLength(UserLength);
        });

        modelBuilder.Entity<ReportRunLog>(entity =>
        {
            entity.ToTable("ReportRunLog", Schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasConversion<string>().HasMaxLength(EnumLength);
            entity.Property(e => e.LoginId).HasMaxLength(UserLength);
        });
    }
}
