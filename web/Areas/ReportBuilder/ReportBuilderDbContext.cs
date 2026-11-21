using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viper.Areas.ReportBuilder.Models.Entities;
using Viper.Areas.ReportBuilder.Services;

namespace Viper.Areas.ReportBuilder;

/// <summary>
/// The Report Builder's tables in the VIPER database's [report] schema. The same schema holds
/// the views and procedures reports read; those are never mapped here, only listed by the
/// source catalog, which looks at views and procedures and so never offers these tables.
/// </summary>
public class ReportBuilderDbContext : DbContext
{
    internal const string Schema = "report";

    private const int TextLength = 500;
    private const int CodeLength = 50;
    private const int UserLength = 50;
    private const int PermissionLength = 100;
    private const string DateTimeType = "datetime";

    public ReportBuilderDbContext(DbContextOptions<ReportBuilderDbContext> options) : base(options)
    {
    }

    public virtual DbSet<ReportDefinition> Reports { get; set; }
    public virtual DbSet<ReportPermission> ReportPermissions { get; set; }
    public virtual DbSet<ReportColumn> ReportColumns { get; set; }
    public virtual DbSet<ReportParameter> ReportParameters { get; set; }
    public virtual DbSet<ReportDefinitionHistory> ReportHistory { get; set; }
    public virtual DbSet<ReportRunLog> ReportRunLog { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Every text column in [report] is varchar; matching that keeps SQL Server from
        // converting the column on every comparison.
        configurationBuilder.Properties<string>().AreUnicode(false);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ReportDefinition>(entity =>
        {
            entity.ToTable("report", Schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Slug).HasColumnName("slug").HasDefaultValueSql("newid()");
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(TextLength);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Area).HasColumnName("area").HasMaxLength(TextLength);
            entity.Property(e => e.Source).HasColumnName("source").HasMaxLength(TextLength);
            entity.Property(e => e.Paginate).HasColumnName("paginate");
            entity.Property(e => e.PageSize).HasColumnName("pageSize");
            entity.Property(e => e.AllowExcel).HasColumnName("excel");
            entity.Property(e => e.AllowPdf).HasColumnName("pdf");
            entity.Property(e => e.PdfLandscape).HasColumnName("pdfLandscape");
            entity.Property(e => e.SensitiveNotice).HasColumnName("sensitive").HasMaxLength(TextLength);
            entity.Property(e => e.OrderBy).HasColumnName("orderBy").HasMaxLength(TextLength);
            entity.Property(e => e.OrderByDescending).HasColumnName("orderByDesc");
            Timestamp(entity.Property(e => e.CreatedOn), "created").HasDefaultValueSql("getdate()");
            entity.Property(e => e.CreatedBy).HasColumnName("createdBy").HasMaxLength(UserLength);
            Timestamp(entity.Property(e => e.ModifiedOn), "modified")
                .HasDefaultValueSql("getdate()")
                .IsConcurrencyToken();
            entity.Property(e => e.ModifiedBy).HasColumnName("modifiedBy").HasMaxLength(UserLength);
            Timestamp(entity.Property(e => e.DeletedOn), "deleted");
            entity.Property(e => e.DeletedBy).HasColumnName("deletedBy").HasMaxLength(UserLength);
            entity.HasMany(e => e.Permissions).WithOne(e => e.Report).HasForeignKey(e => e.ReportId);
            entity.HasMany(e => e.Columns).WithOne(e => e.Report).HasForeignKey(e => e.ReportId);
            entity.HasMany(e => e.Parameters).WithOne(e => e.Report).HasForeignKey(e => e.ReportId);
        });

        modelBuilder.Entity<ReportPermission>(entity =>
        {
            entity.ToTable("report_permission", Schema);
            entity.HasKey(e => new { e.ReportId, e.Permission });
            entity.Property(e => e.ReportId).HasColumnName("reportId");
            entity.Property(e => e.Permission).HasColumnName("permission").HasMaxLength(PermissionLength);
        });

        modelBuilder.Entity<ReportColumn>(entity =>
        {
            entity.ToTable("report_column", Schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ReportId).HasColumnName("reportId");
            entity.Property(e => e.SourceColumn).HasColumnName("column").HasMaxLength(TextLength);
            entity.Property(e => e.Label).HasColumnName("label").HasMaxLength(TextLength);
            entity.Property(e => e.Format).HasColumnName("datatype").HasConversion<string>().HasMaxLength(CodeLength);
            entity.Property(e => e.Sortable).HasColumnName("sortable");
            entity.Property(e => e.Visible).HasColumnName("visible");
            entity.Property(e => e.DisplayOrder).HasColumnName("displayOrder");
        });

        modelBuilder.Entity<ReportParameter>(entity =>
        {
            entity.ToTable("report_parameter", Schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ReportId).HasColumnName("reportId");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(TextLength);
            entity.Property(e => e.Label).HasColumnName("label").HasMaxLength(TextLength);
            entity.Property(e => e.DisplayOrder).HasColumnName("displayOrder");
            entity.Property(e => e.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(CodeLength);
            entity.Property(e => e.Operator).HasColumnName("operator").HasConversion<string>().HasMaxLength(CodeLength);
            entity.Property(e => e.Required).HasColumnName("required");
            entity.Property(e => e.DefaultValue).HasColumnName("defaultValue").HasMaxLength(TextLength);
            entity.Property(e => e.ChoicesSource).HasColumnName("choicesSource").HasMaxLength(TextLength);
            entity.Property(e => e.ChoicesList).HasColumnName("choicesList");
            entity.Property(e => e.Binding).HasColumnName("binding").HasConversion<string>().HasMaxLength(TextLength);
        });

        modelBuilder.Entity<ReportDefinitionHistory>(entity =>
        {
            entity.ToTable("report_history", Schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ReportId).HasColumnName("reportId");
            entity.Property(e => e.Action).HasColumnName("action").HasConversion<string>().HasMaxLength(TextLength);
            entity.Property(e => e.Snapshot).HasColumnName("snapshot");
            Timestamp(entity.Property(e => e.ChangedOn), "changed").HasDefaultValueSql("getdate()");
            entity.Property(e => e.ChangedBy).HasColumnName("changedBy").HasMaxLength(UserLength);
        });

        modelBuilder.Entity<ReportRunLog>(entity =>
        {
            entity.ToTable("report_runLog", Schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ReportId).HasColumnName("reportId");
            entity.Property(e => e.Action).HasColumnName("action").HasConversion<string>().HasMaxLength(TextLength);
            entity.Property(e => e.LoginId).HasColumnName("loginID").HasMaxLength(UserLength);
            Timestamp(entity.Property(e => e.RanOn), "ran").HasDefaultValueSql("getdate()");
            entity.Property(e => e.Parameters).HasColumnName("parameters");
            entity.Property(e => e.RowsReturned).HasColumnName("rowsReturned");
            entity.Property(e => e.DurationMs).HasColumnName("duration");
            entity.Property(e => e.ErrorNumber).HasColumnName("errorNumber");
            entity.Property(e => e.ErrorMessage).HasColumnName("errorMessage").HasMaxLength(ReportLimits.MaxErrorLength);
        });
    }

    /// <summary>The [report] tables store times as datetime, not EF's default datetime2.</summary>
    private static PropertyBuilder<DateTime?> Timestamp(PropertyBuilder<DateTime?> property, string column)
    {
        return property.HasColumnName(column).HasColumnType(DateTimeType);
    }
}
