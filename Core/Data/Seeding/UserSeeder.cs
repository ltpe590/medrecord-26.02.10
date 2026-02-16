using Core.Entities;
using Core.Data.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Core.Data.Seeding;

/// <summary>
/// Seeds default users into the database (e.g., admin account).
/// </summary>
public static class UserSeeder
{
    /// <summary>
    /// Seeds the admin user if it doesn't already exist.
    /// </summary>
    public static async Task SeedAdminUserAsync(ApplicationDbContext context, IUserStore<AppUser> userStore, IPasswordHasher<AppUser> passwordHasher)
    {
        const string adminUsername = "admin";
        const string adminPassword = "Admin123!";
        const int adminSpecialtyProfileId = 1;

        // Check if admin already exists
        var adminExists = await context.Users.AnyAsync(u => u.UserName == adminUsername);
        if (adminExists)
            return;

        // Create admin user
        var adminUser = new AppUser
        {
            UserName = adminUsername,
            NormalizedUserName = adminUsername.ToUpper(),
            Email = "admin@medrecord.local",
            NormalizedEmail = "admin@medrecord.local".ToUpper(),
            EmailConfirmed = true,
            PhoneNumber = null,
            PhoneNumberConfirmed = false,
            IsActive = true,
            SpecialtyProfileId = adminSpecialtyProfileId,
            CreatedAt = DateTime.UtcNow,
            HasFingerprintEnrolled = false,
            FingerprintTemplate = Array.Empty<byte>()
        };

        // Hash password and set it
        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, adminPassword);
        adminUser.SecurityStamp = Guid.NewGuid().ToString();

        // Add to context and save
        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
    }
}