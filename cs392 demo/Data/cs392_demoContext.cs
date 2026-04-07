using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using cs392_demo.models;
using cs392_demo.viewModels;


namespace cs392_demo.Data
{
    public class cs392_demoContext : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<AppUser>
    {
        public cs392_demoContext(DbContextOptions<cs392_demoContext> options)
            : base(options)
        {
        }

        public DbSet<Business> Business { get; set; } = default!;
        public DbSet<TestModelClass> TestModelClass { get; set; } = default!;
        public DbSet<Inventory_Location> Inventory_Location { get; set; } = default!;
        public DbSet<Inventory_Activity_Log> Inventory_Activity_Log { get; set; } = default!;
        public DbSet<Stock> Stock { get; set; } = default!;
        public DbSet<ManagerInvitation> ManagerInvitation { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Business -> AppUser (one-to-many)
            modelBuilder.Entity<AppUser>()
                .HasOne(u => u.Business)
                .WithMany(b => b.Users)
                .HasForeignKey(u => u.BusinessId)
                .OnDelete(DeleteBehavior.SetNull);

            // Business -> Inventory_Location (one-to-many)
            modelBuilder.Entity<Inventory_Location>()
                .HasOne(l => l.Business)
                .WithMany(b => b.Locations)
                .HasForeignKey(l => l.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            // Business -> Stock (one-to-many)
            modelBuilder.Entity<Stock>()
                .HasOne(s => s.Business)
                .WithMany()
                .HasForeignKey(s => s.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inventory_Location>()
                .HasIndex(l => new { l.BusinessId, l.location_id });

            modelBuilder.Entity<Stock>()
                .HasIndex(s => new { s.BusinessId, s.Location_Stock_ID });

            modelBuilder.Entity<Stock>()
                .HasIndex(s => new { s.BusinessId, s.Stock_ID })
                .IsUnique();

            modelBuilder.Entity<Inventory_Activity_Log>()
                .HasIndex(l => new { l.BusinessId, l.Stock_ID_Log });

            modelBuilder.Entity<Inventory_Activity_Log>()
                .HasOne(l => l.Business)
                .WithMany()
                .HasForeignKey(l => l.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ensure InviteCode is unique
            modelBuilder.Entity<Business>()
                .HasIndex(b => b.Invite_Code)
                .IsUnique();

            // Ensure manager invitation tokens are unique
            modelBuilder.Entity<ManagerInvitation>()
                .HasIndex(i => i.Token)
                .IsUnique();
        }
    }
}