using Core.Data.Context;
using Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace Core.Data.Seeding
{
    /// <summary>
    /// Seeds the default admin user on first startup if it doesn't exist.
    /// Called from WebApi Program.cs.
    /// </summary>
    public static class UserSeeder
    {
        public static async Task SeedAdminUserAsync(
            ApplicationDbContext db,
            IUserStore<AppUser>   userStore,
            IPasswordHasher<AppUser> passwordHasher)
        {
            if (db.Users.Any()) return;

            var user = new AppUser
            {
                UserName           = "admin",
                NormalizedUserName = "ADMIN",
                Email              = "admin@medrecord.local",
                NormalizedEmail    = "ADMIN@MEDRECORD.LOCAL",
                EmailConfirmed     = true,
                SecurityStamp      = Guid.NewGuid().ToString(),
            };

            user.PasswordHash = passwordHasher.HashPassword(user, "Admin@123");

            db.Users.Add(user);
            await db.SaveChangesAsync();
            Console.WriteLine("[UserSeeder] Admin user created.");
        }
    }
}
