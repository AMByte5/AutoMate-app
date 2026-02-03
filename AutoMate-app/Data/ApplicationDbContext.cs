using AutoMate_app.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AutoMate_app.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<MechanicProfile> MechanicProfiles { get; set; }
        public DbSet<ServiceType> ServiceTypes { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ServiceRequest relationships
            builder.Entity<ServiceRequest>()
                .HasOne(sr => sr.ServiceType)
                .WithMany()
                .HasForeignKey(sr => sr.ServiceTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ServiceRequest>()
                .HasOne(sr => sr.Client)
                .WithMany()
                .HasForeignKey(sr => sr.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ServiceRequest>()
                .HasOne(sr => sr.Mechanic)
                .WithMany()
                .HasForeignKey(sr => sr.MechanicId)
                .OnDelete(DeleteBehavior.SetNull);

            // Review relationships
            builder.Entity<Review>()
                .HasOne(r => r.ServiceRequest)
                .WithMany()
                .HasForeignKey(r => r.ServiceRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne(r => r.Client)
                .WithMany()
                .HasForeignKey(r => r.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserProfile>()
        .HasOne(p => p.User)
        .WithOne()
        .HasForeignKey<UserProfile>(p => p.UserId)
        .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserProfile>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            builder.Entity<MechanicProfile>()
                .HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<MechanicProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MechanicProfile>()
                .HasIndex(p => p.UserId)
                .IsUnique();




            // Indexes for performance
            builder.Entity<ServiceRequest>()
                .HasIndex(sr => sr.Status);

            builder.Entity<ServiceRequest>()
                .HasIndex(sr => sr.CreatedAt);

            builder.Entity<ServiceRequest>()
                .HasIndex(sr => sr.ClientId);

            builder.Entity<ServiceRequest>()
                .HasIndex(sr => sr.MechanicId);

            builder.Entity<Review>()
                .HasIndex(r => r.ServiceRequestId);
        }
    }
}