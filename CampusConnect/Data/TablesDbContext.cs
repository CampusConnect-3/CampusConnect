using Microsoft.EntityFrameworkCore;
using CampusConnect.Models;

namespace CampusConnect.Data
{
    public class TablesDbContext : DbContext
    {
        public TablesDbContext(DbContextOptions<TablesDbContext> options) : base(options) { }

        public DbSet<user> users { get; set; }
        public DbSet<userRoles> userRoles { get; set; }
        public DbSet<roles> roles { get; set; }
        public DbSet<requestStatus> requestStatus { get; set; }
        public DbSet<requestComments> requestComments { get; set; }
        public DbSet<request> request { get; set; }
        public DbSet<category> category { get; set; }
        public DbSet<attachments> attachments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // composite PK for userRoles (roleID, userID)
            modelBuilder.Entity<userRoles>().HasKey(ur => new { ur.roleID, ur.userID });

            // configure relationships explicitly to match schema intent
            modelBuilder.Entity<request>()
                .HasOne(r => r.createdBy)
                .WithMany(u => u.requestsCreated)
                .HasForeignKey(r => r.created_by)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<request>()
                .HasOne(r => r.assignedTo)
                .WithMany(u => u.requestsAssigned)
                .HasForeignKey(r => r.assigned_to)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<request>()
                .HasOne(r => r.category)
                .WithMany(c => c.requests)
                .HasForeignKey(r => r.categoryID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<request>()
                .HasOne(r => r.status)
                .WithMany(s => s.requests)
                .HasForeignKey(r => r.statusID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<requestComments>()
                .HasOne(rc => rc.request)
                .WithMany(r => r.comments)
                .HasForeignKey(rc => rc.requestID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<requestComments>()
                .HasOne(rc => rc.creator)
                .WithMany(u => u.comments)
                .HasForeignKey(rc => rc.creatorID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<attachments>()
                .HasOne(a => a.request)
                .WithMany(r => r.attachments)
                .HasForeignKey(a => a.requestID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<attachments>()
                .HasOne(a => a.creator)
                .WithMany(u => u.attachments)
                .HasForeignKey(a => a.creatorID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}