using Microsoft.EntityFrameworkCore;
using RideMatchingSystem.Models;

namespace RideMatchingSystem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Driver> Drivers => Set<Driver>();

    public DbSet<Rider> Riders => Set<Rider>();

    public DbSet<Ride> Rides => Set<Ride>();

    public DbSet<RideDriverRejection> RideDriverRejections =>
        Set<RideDriverRejection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Driver>()
            .HasIndex(x => new
            {
                x.Status,
                x.LastLocationUpdatedAt
            });

        modelBuilder.Entity<Ride>()
            .HasIndex(x => new
            {
                x.Status,
                x.CreatedAt
            });

        modelBuilder.Entity<RideDriverRejection>()
            .HasIndex(x => new
            {
                x.RideId,
                x.DriverId
            })
            .IsUnique();

        modelBuilder.Entity<Ride>()
            .HasOne(x => x.Rider)
            .WithMany()
            .HasForeignKey(x => x.RiderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ride>()
            .HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}