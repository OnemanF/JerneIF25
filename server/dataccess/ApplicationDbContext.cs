using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace JerneIF25.DataAccess.Entities;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<board> boards { get; set; }

    public virtual DbSet<board_subscription> board_subscriptions { get; set; }

    public virtual DbSet<game> games { get; set; }

    public virtual DbSet<player> players { get; set; }

    public virtual DbSet<transaction> transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<board>(entity =>
        {
            entity.HasKey(e => e.id).HasName("boards_pkey");

            entity.HasIndex(e => e.game_id, "ix_boards_game").HasFilter("(is_deleted = false)");

            entity.HasIndex(e => e.player_id, "ix_boards_player").HasFilter("(is_deleted = false)");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.price_dkk).HasPrecision(10, 2);
            entity.Property(e => e.purchased_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.game).WithMany(p => p.boards)
                .HasForeignKey(d => d.game_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("boards_game_id_fkey");

            entity.HasOne(d => d.player).WithMany(p => p.boards)
                .HasForeignKey(d => d.player_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("boards_player_id_fkey");
        });

        modelBuilder.Entity<board_subscription>(entity =>
        {
            entity.HasKey(e => e.id).HasName("board_subscriptions_pkey");

            entity.HasIndex(e => new { e.player_id, e.is_active }, "ix_sub_player_active").HasFilter("(is_deleted = false)");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.started_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.player).WithMany(p => p.board_subscriptions)
                .HasForeignKey(d => d.player_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("board_subscriptions_player_id_fkey");
        });

        modelBuilder.Entity<game>(entity =>
        {
            entity.HasKey(e => e.id).HasName("games_pkey");

            entity.HasIndex(e => e.week_start, "games_week_start_key").IsUnique();

            entity.HasIndex(e => e.status, "ix_games_status").HasFilter("(is_deleted = false)");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.status)
                .HasMaxLength(10)
                .HasDefaultValueSql("'inactive'::character varying");
        });

        modelBuilder.Entity<player>(entity =>
        {
            entity.HasKey(e => e.id).HasName("players_pkey");

            entity.HasIndex(e => e.is_active, "ix_players_active").HasFilter("(is_deleted = false)");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.email).HasMaxLength(255);
            entity.Property(e => e.is_active).HasDefaultValue(false);
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.name).HasMaxLength(255);
            entity.Property(e => e.phone).HasMaxLength(50);
        });

        modelBuilder.Entity<transaction>(entity =>
        {
            entity.HasKey(e => e.id).HasName("transactions_pkey");

            entity.HasIndex(e => new { e.player_id, e.status }, "ix_tx_player_status").HasFilter("(is_deleted = false)");

            entity.HasIndex(e => e.mobilepay_ref, "uq_tx_mobilepay_ref")
                .IsUnique()
                .HasFilter("((mobilepay_ref IS NOT NULL) AND (is_deleted = false))");

            entity.Property(e => e.amount_dkk).HasPrecision(10, 2);
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.mobilepay_ref).HasMaxLength(64);
            entity.Property(e => e.requested_at).HasDefaultValueSql("now()");
            entity.Property(e => e.status)
                .HasMaxLength(16)
                .HasDefaultValueSql("'pending'::character varying");

            entity.HasOne(d => d.player).WithMany(p => p.transactions)
                .HasForeignKey(d => d.player_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("transactions_player_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
