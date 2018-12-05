using kin_stellar_stats.Database.Models;
using Kin.Horizon.Api.Poller.Database.StellarObjectWrappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Kin.Horizon.Api.Poller.Database
{
    public class ManagementContext : DbContext
    {
        public ManagementContext(DbContextOptions<ManagementContext> options) : base(options)
        {
            this.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
           // this.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql("server=localhost;database=kin_stats;uid=root;pwd=password");
            }

        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Pagination>()
                .HasIndex(c => c.CursorType).IsUnique();

            builder.Entity<Pagination>()
                .HasKey(c => c.CursorType).Metadata
                .IsPrimaryKey();




            base.OnModelCreating(builder);
        }

        public DbSet<FlattenedOperation> FlattenedOperation { get; set; }
        public DbSet<FlattenPaymentOperation> FlattenPaymentOperation { get; set; }
        public DbSet<FlattenCreateAccountOperation> FlattenCreateAccountOperation { get; set; }
       // public DbSet<KinAccount> KinAccounts { get; set; }
        public DbSet<Pagination> Paginations { get; set; }
    }
}
