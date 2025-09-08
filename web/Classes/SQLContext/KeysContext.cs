using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Viper.Models.Keys;

namespace Viper.Classes.SQLContext;

public partial class KeysContext : DbContext
{

    public KeysContext()
        : base()
    {
    }

    public KeysContext(DbContextOptions<KeysContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Building> Buildings { get; set; }

    public virtual DbSet<Disposition> Dispositions { get; set; }

    public virtual DbSet<Import> Imports { get; set; }

    public virtual DbSet<Import2> Import2s { get; set; }

    public virtual DbSet<Import3> Import3s { get; set; }

    public virtual DbSet<Key> Keys { get; set; }

    public virtual DbSet<KeyAssignment> KeyAssignments { get; set; }

    public virtual DbSet<KeyBuilding> KeyBuildings { get; set; }

    public virtual DbSet<KeyManager> KeyManagers { get; set; }

    public virtual DbSet<VwUserinfo> VwUserinfos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Building>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Disposition>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Import>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("Import");

            entity.Property(e => e.Building)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Comments)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CutNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DateAssigned).HasColumnType("datetime");
            entity.Property(e => e.DateReturned).HasColumnType("datetime");
            entity.Property(e => e.Department)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.KeyNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Location)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.Room)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Import2>(entity =>
        {
            entity.HasKey(e => e.ImportId);

            entity.ToTable("Import2");

            entity.Property(e => e.ImportId)
                .ValueGeneratedNever()
                .HasColumnName("ImportID");
            entity.Property(e => e.Comments)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CutNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Disposition)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.KeyNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.MothraId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("MothraID");
        });

        modelBuilder.Entity<Import3>(entity =>
        {
            entity.HasKey(e => e.ImportId);

            entity.ToTable("Import3");

            entity.Property(e => e.ImportId).HasColumnName("ImportID");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.CutNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Disposition)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.DispositionDate).HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.KeyNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.MothraId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("MothraID");
        });

        modelBuilder.Entity<Key>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("PK_Key");

            entity.Property(e => e.KeyId).HasColumnName("KeyID");
            entity.Property(e => e.AccessDescription)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.BuildingMaster).HasDefaultValueSql("((0))");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.Deleted).HasDefaultValueSql("((0))");
            entity.Property(e => e.Grandmaster).HasDefaultValueSql("((0))");
            entity.Property(e => e.KeyNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ManagedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Notes)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.Restricted).HasDefaultValueSql("((0))");
            entity.Property(e => e.RestrictedContact)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Submaster).HasDefaultValueSql("((0))");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<KeyAssignment>(entity =>
        {
            entity.ToTable("KeyAssignment");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AdHocName)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.AssignedTo)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.CutNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DispositionBy)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.DispositionDate).HasColumnType("datetime");
            entity.Property(e => e.DueDate).HasColumnType("datetime");
            entity.Property(e => e.IssuedBy)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.IssuedDate).HasColumnType("datetime");
            entity.Property(e => e.KeyId).HasColumnName("KeyID");
            entity.Property(e => e.RequestDate).HasColumnType("datetime");
            entity.Property(e => e.RequestedBy)
                .HasMaxLength(9)
                .IsUnicode(false);
        });

        modelBuilder.Entity<KeyBuilding>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BuildingId).HasColumnName("BuildingID");
            entity.Property(e => e.KeyId).HasColumnName("KeyID");
        });

        modelBuilder.Entity<KeyManager>(entity =>
        {
            entity.Property(e => e.KeyManagerId).HasColumnName("KeyManagerID");
            entity.Property(e => e.KeyId).HasColumnName("KeyID");
            entity.Property(e => e.ManagerId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ManagerID");
        });

        modelBuilder.Entity<VwUserinfo>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_userinfo");

            entity.Property(e => e.AccessDescription)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.AdHocName)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.AssignedTo)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.CutNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DispositionBy)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.DispositionDate).HasColumnType("datetime");
            entity.Property(e => e.DueDate).HasColumnType("datetime");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.IssuedBy)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.IssuedBy1)
                .HasMaxLength(91)
                .IsUnicode(false)
                .HasColumnName("issued_by");
            entity.Property(e => e.IssuedDate).HasColumnType("datetime");
            entity.Property(e => e.KeyId).HasColumnName("KeyID");
            entity.Property(e => e.KeyNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ManagedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Notes)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.RequestDate).HasColumnType("datetime");
            entity.Property(e => e.RequestedBy)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.RestrictedContact)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
