using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Viper.Models.CTS;

namespace Viper.Classes.SQLContext;

public partial class CtsContext : DbContext
{
    public CtsContext(DbContextOptions<CtsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Competency> Competencies { get; set; }

    public virtual DbSet<Domain> Domains { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (HttpHelper.Settings != null)
        {
            optionsBuilder.UseSqlServer(HttpHelper.Settings["ConnectionStrings:VIPER"]);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Competency>(entity =>
        {
            entity.ToTable("Competency", "cts");

            entity.Property(e => e.Description).IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .IsUnicode(false);

            entity.HasOne(d => d.Domain).WithMany(p => p.Competencies)
                .HasForeignKey(d => d.DomainId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Competency_Domain");

            entity.HasOne(d => d.Parent).WithMany(p => p.Children)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK_Competency_Competency");
        });

        modelBuilder.Entity<Domain>(entity =>
        {
            entity.ToTable("Domain", "cts");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
