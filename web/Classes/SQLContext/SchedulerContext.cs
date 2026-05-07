// ReSharper disable UnusedAutoPropertyAccessor.Global
// EF Core sets DbSet properties via reflection at context construction;
// ReSharper cannot see that usage.
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Scheduler.Models.Entities;

namespace Viper.Classes.SQLContext;

public partial class VIPERContext
{
    /* Scheduler */
    public virtual DbSet<SchedulerJobState> SchedulerJobStates { get; set; }

    partial void OnModelCreatingScheduler(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SchedulerJobState>(entity =>
        {
            entity.ToTable("SchedulerJobState", "HangFire");

            entity.HasKey(e => e.RecurringJobId);

            entity.Property(e => e.RecurringJobId)
                .HasMaxLength(200);

            entity.Property(e => e.Cron)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.Queue)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.TimeZoneId)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.JobTypeName)
                .IsRequired()
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.Property(e => e.SerializedArgs)
                .IsRequired();

            entity.Property(e => e.PausedAt)
                .HasColumnType("datetime2");

            entity.Property(e => e.PausedBy)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
        });
    }
}
