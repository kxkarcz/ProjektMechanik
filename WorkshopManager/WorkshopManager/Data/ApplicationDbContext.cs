using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WorkshopManager.Models;
using Microsoft.AspNetCore.Identity;

namespace WorkshopManager.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // stałe id, aby HasData działało poprawnie
            const string ADMIN_ID = "7176bd3e-6f85-4f3d-bb84-1dacf488e9aa";
            const string MECHANIK_ID = "8a9c2f33-1d24-4c83-9d92-5ebf9f8327b2";
            const string RECEPCJONISTA_ID = "c3d5e621-4a6b-4f60-9c24-2a7e3f9d6f30";

            builder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = ADMIN_ID,
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                },
                new IdentityRole
                {
                    Id = MECHANIK_ID,
                    Name = "Mechanik",
                    NormalizedName = "MECHANIK"
                },
                new IdentityRole
                {
                    Id = RECEPCJONISTA_ID,
                    Name = "Recepcjonista",
                    NormalizedName = "RECEPSJONISTA"
                }
                );
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<ServiceOrder> ServiceOrders { get; set; }
        public DbSet<ServiceTask> ServiceTasks { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Part> Parts { get; set; }
        public DbSet<UsedPart> UsedParts { get; set; }
    }
}