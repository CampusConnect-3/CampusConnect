using Microsoft.EntityFrameworkCore;
using CampusConnect.Models;

namespace CampusConnect.Data
{
    public class TablesDbContext : DbContext
    {
        public TablesDbContext(DbContextOptions<TablesDbContext> options) : base(options) { }

        public DbSet<User> users { get; set; } = default!;
        public DbSet<userRoles> userRoles { get; set; } = default!;

        // NOTE: If this "Roles" type conflicts with CampusConnect.Constants.Roles (enum),
        // keep this DbSet fully qualified or rename the model class.
        public DbSet<CampusConnect.Models.Roles> roles { get; set; } = default!;

        public DbSet<RequestStatus> requestStatus { get; set; } = default!;
        public DbSet<requestComments> requestComments { get; set; } = default!;
        public DbSet<Request> request { get; set; } = default!;
        public DbSet<Category> category { get; set; } = default!;
        public DbSet<Attachments> attachments { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // composite PK for userRoles (roleID, userID)
            modelBuilder.Entity<userRoles>()
                .HasKey(ur => new { ur.roleID, ur.userID });

            // -----------------------------
            // Request relationships
            // -----------------------------

            modelBuilder.Entity<Request>()
                .HasOne(r => r.createdBy)
                .WithMany(u => u.requestsCreated)
                .HasForeignKey(r => r.created_by)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Request>()
                .HasOne(r => r.assignedTo)
                .WithMany(u => u.requestsAssigned)
                .HasForeignKey(r => r.assigned_to)
                .OnDelete(DeleteBehavior.SetNull);

            // IMPORTANT: Pair Request.category <-> Category.requests
            modelBuilder.Entity<Request>()
                .HasOne(r => r.category)
                .WithMany(c => c.requests)
                .HasForeignKey(r => r.categoryID)
                .OnDelete(DeleteBehavior.Restrict);

            // IMPORTANT: Pair Request.status <-> RequestStatus.requests
            modelBuilder.Entity<Request>()
                .HasOne(r => r.status)
                .WithMany(s => s.requests)
                .HasForeignKey(r => r.statusID)
                .OnDelete(DeleteBehavior.SetNull);

            // -----------------------------
            // Comments relationships
            // -----------------------------
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

            // -----------------------------
            // Attachments relationships
            // -----------------------------
            modelBuilder.Entity<Attachments>()
                .HasOne(a => a.request)
                .WithMany(r => r.attachments)
                .HasForeignKey(a => a.requestID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Attachments>()
                .HasOne(a => a.creator)
                .WithMany(u => u.attachments)
                .HasForeignKey(a => a.creatorID)
                .OnDelete(DeleteBehavior.Restrict);

            // -----------------------------
            // Uniqueness constraints
            // -----------------------------
            modelBuilder.Entity<User>()
                .HasIndex(u => u.IdentityUserId)
                .IsUnique()
                .HasFilter("[IdentityUserId] IS NOT NULL");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.username)
                .IsUnique();
        }
    }
}