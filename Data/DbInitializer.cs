using Microsoft.AspNetCore.Identity;
using CarInfoManagementSystem.Models;

namespace CarInfoManagementSystem.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            context.Database.EnsureCreated();

            // Create roles if they don't exist
            string[] roles = { "Administrator", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Create admin user if it doesn't exist
            var adminEmail = "admin@cims.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrator");
                }
            }

            // Check if we have any car types
            if (!context.CarTypes.Any())
            {
                var carTypes = new CarType[]
                {
                    new CarType { Type = "Hatchback" },
                    new CarType { Type = "Sedan" },
                    new CarType { Type = "SUV" }
                };
                context.CarTypes.AddRange(carTypes);
            }

            // Check if we have any transmission types
            if (!context.CarTransmissionTypes.Any())
            {
                var transmissionTypes = new CarTransmissionType[]
                {
                    new CarTransmissionType { Name = "Manual" },
                    new CarTransmissionType { Name = "Automatic" },
                    new CarTransmissionType { Name = "CVT" },
                    new CarTransmissionType { Name = "DCT" }
                };
                context.CarTransmissionTypes.AddRange(transmissionTypes);
            }

            // Check if we have any manufacturers
            if (!context.Manufacturers.Any())
            {
                var manufacturers = new Manufacturer[]
                {
                    new Manufacturer 
                    { 
                        Name = "Sample Manufacturer",
                        ContactPerson = "John Doe",
                        RegisteredOffice = "123 Main St, Sample City"
                    }
                };
                context.Manufacturers.AddRange(manufacturers);
            }

            await context.SaveChangesAsync();
        }
    }
}