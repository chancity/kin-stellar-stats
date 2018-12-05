using Microsoft.EntityFrameworkCore;

namespace Kin.Horizon.Api.Poller.Database
{
    public partial class KinstatsContext : DbContext
    {
        public KinstatsContext()
        {
        }

        public KinstatsContext(DbContextOptions<KinstatsContext> options)
            : base(options)
        {
        }

        public virtual DbSet<ActiveWallet> ActiveWallet { get; set; }
        public virtual DbSet<App> App { get; set; }
        public virtual DbSet<AppInfo> AppInfo { get; set; }
        public virtual DbSet<AppStats> AppStats { get; set; }
        public virtual DbSet<OverallStats> OverallStats { get; set; }
        public virtual DbSet<Pagination> Pagination { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {

            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ActiveWallet>(entity =>
            {
                entity.HasKey(e => new { e.Year, e.Day, e.Address });

                entity.ToTable("active_wallet");

                entity.HasIndex(e => e.Address)
                    .HasName("address");

                entity.Property(e => e.Year).HasColumnName("year");

                entity.Property(e => e.Day).HasColumnName("day");

                entity.Property(e => e.Address)
                    .IsRequired()
                    .HasColumnName("address")
                    .HasColumnType("varchar(75)");
            });

            modelBuilder.Entity<App>(entity =>
            {
                entity.ToTable("app");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.AppId)
                    .IsRequired()
                    .HasColumnName("app_id")
                    .HasColumnType("varchar(20)");

                entity.Property(e => e.FriendlyName)
                    .HasColumnName("friendly_name")
                    .HasColumnType("varchar(50)");
            });

            modelBuilder.Entity<AppInfo>(entity =>
            {
                entity.HasKey(e => e.AppId);

                entity.ToTable("app_info");

                entity.Property(e => e.AppId)
                    .HasColumnName("app_id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.AppStore)
                    .HasColumnName("app_store")
                    .HasColumnType("varchar(255)");

                entity.Property(e => e.Description)
                    .HasColumnName("description")
                    .HasColumnType("varchar(255)");

                entity.Property(e => e.GooglePlay)
                    .HasColumnName("google_play")
                    .HasColumnType("varchar(255)");

                entity.Property(e => e.Website)
                    .HasColumnName("website")
                    .HasColumnType("varchar(255)");

                entity.HasOne(d => d.App)
                    .WithOne(p => p.AppInfo)
                    .HasForeignKey<AppInfo>(d => d.AppId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_app_info_app");
            });

            modelBuilder.Entity<AppStats>(entity =>
            {
                entity.HasKey(e => new { e.Year, e.Day, e.AppId });

                entity.ToTable("app_stats");

                entity.HasIndex(e => e.AppId)
                    .HasName("FK_app_stats_app");

                entity.Property(e => e.Year)
                    .HasColumnName("year");

                entity.Property(e => e.Day)
                    .HasColumnName("day");

                entity.Property(e => e.ActiveUsers)
                    .HasColumnName("active_users")
                    .HasColumnType("bigint(20)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.AppId)
                    .HasColumnName("app_id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CreatedWallets)
                    .HasColumnName("created_wallets")
                    .HasColumnType("bigint(20)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Operations)
                    .HasColumnName("operations")
                    .HasColumnType("bigint(20)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.PaymentVolume)
                    .HasColumnName("payment_volume")
                    .HasColumnType("bigint(20)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Payments)
                    .HasColumnName("payments")
                    .HasColumnType("bigint(20)")
                    .HasDefaultValueSql("'0'");

                entity.HasOne(d => d.App)
                    .WithMany(p => p.AppStats)
                    .HasForeignKey(d => d.AppId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_app_stats_app");
            });

            modelBuilder.Entity<OverallStats>(entity =>
            {
                entity.HasKey(e => e.AppId);

                entity.ToTable("overall_stats");

                entity.HasIndex(e => e.AppId)
                    .HasName("app_id")
                    .IsUnique();

                entity.Property(e => e.AppId)
                    .HasColumnName("app_id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.ActiveUsers)
                    .HasColumnName("active_users")
                    .HasColumnType("bigint(20)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.CreatedWallets)
                    .HasColumnName("created_wallets")
                    .HasColumnType("bigint(20)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Operations)
                    .HasColumnName("operations")
                    .HasColumnType("bigint(20)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.PaymentVolume)
                    .HasColumnName("payment_volume")
                    .HasColumnType("bigint(20)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Payments)
                    .HasColumnName("payments")
                    .HasColumnType("bigint(20)")
                    .HasDefaultValueSql("'0'");

                entity.HasOne(d => d.App)
                    .WithOne(p => p.OverallStats)
                    .HasForeignKey<OverallStats>(d => d.AppId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_overall_stats_app");
            });

            modelBuilder.Entity<Pagination>(entity =>
            {
                entity.HasKey(e => e.CursorType);

                entity.ToTable("pagination");

                entity.HasIndex(e => e.CursorType)
                    .HasName("cursor_type")
                    .IsUnique();

                entity.Property(e => e.CursorType)
                    .HasColumnName("cursor_type")
                    .HasColumnType("varchar(45)");

                entity.Property(e => e.CursorId)
                    .HasColumnName("cursor_id")
                    .HasDefaultValueSql("'0'");
            });
        }
    }
}
