using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CarInfoManagementSystem.Models;

namespace CarInfoManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Car> Cars { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<CarType> CarTypes { get; set; }
        public DbSet<CarTransmissionType> CarTransmissionTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure unique constraints
            modelBuilder.Entity<Manufacturer>()
                .HasIndex(m => m.Name)
                .IsUnique();

            modelBuilder.Entity<CarType>()
                .HasIndex(ct => ct.Type)
                .IsUnique();

            modelBuilder.Entity<CarTransmissionType>()
                .HasIndex(ct => ct.Name)
                .IsUnique();
        }
    }
}