using Microsoft.EntityFrameworkCore;
using OnlineLibrary.Infrastructure.Domain.Entities;
using OnlineLibrary.Infrastructure.Security;
using System.Net;
using System.Numerics;

namespace OnlineLibrary.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static void Seed(ApplicationDbContext context)
        {
            context.Database.Migrate();

            // Seed Roles
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(
                    new Role { RoleId = Guid.NewGuid(), RoleName = "Admin" },
                    new Role { RoleId = Guid.NewGuid(), RoleName = "Librarian" },
                    new Role { RoleId = Guid.NewGuid(), RoleName = "Student" }
                );

                context.SaveChanges();
            }

            // Seed Admin User
            if (!context.Users.Any(u => u.Email == "admin@library.com"))
            {
                var adminRoleId = context.Roles
                    .Where(r => r.RoleName == "Admin")
                    .Select(r => r.RoleId)
                    .First();

                context.Users.Add(new User
                {
                    UserId = Guid.NewGuid(),
                    FullName = "Admin",
                    Email = "admin@library.com",
                    PasswordHash = PasswordHelper.HashPassword("Admin@123"),
                    Phone = "01800000000",
                    Address = "Uttara, Dhaka",
                    RoleId = adminRoleId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

                context.SaveChanges();
            }
        }
    }
}
