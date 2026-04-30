using Microsoft.EntityFrameworkCore;
using DashboardAPI.Models;

namespace DashboardAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User>            Users           { get; set; }
        public DbSet<Dataset>         Datasets        { get; set; }
        public DbSet<Dashboard>       Dashboards      { get; set; }
        public DbSet<Widget>          Widgets         { get; set; }
        public DbSet<Insight>         Insights        { get; set; }
        public DbSet<DataSource>      DataSources     { get; set; }
        public DbSet<DashboardShare>  DashboardShares { get; set; } 
        public DbSet<DashboardVersion> DashboardVersions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Widget → Dashboard
            modelBuilder.Entity<Widget>()
                .HasOne(w => w.Dashboard)
                .WithMany(d => d.Widgets)
                .HasForeignKey(w => w.DashboardId)
                .OnDelete(DeleteBehavior.Cascade);

            // DataSource → User
            modelBuilder.Entity<DataSource>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // DashboardShare → Dashboard
            modelBuilder.Entity<DashboardShare>()
                .HasOne(s => s.Dashboard)
                .WithMany()
                .HasForeignKey(s => s.DashboardId)
                .OnDelete(DeleteBehavior.Cascade);

            // DashboardShare → User (destinataire, nullable)
            modelBuilder.Entity<DashboardShare>()
                .HasOne(s => s.SharedWithUser)
                .WithMany()
                .HasForeignKey(s => s.SharedWithUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Enum permissions comme entier
            modelBuilder.Entity<DashboardShare>()
                .Property(s => s.Permission)
                .HasConversion<int>();

            // DataSource enum
            modelBuilder.Entity<DataSource>()
                .Property(d => d.Type)
                .HasConversion<int>();
        }
    }
}