using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WellnessPlatform.Models;

namespace WellnessPlatform.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        IDbContextFactory<AppDbContext> factory,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<int>> roleManager)
    {
        await using var db =
            await factory.CreateDbContextAsync();

        await db.Database.MigrateAsync();

        if (!await db.AvailabilitySettings.AnyAsync())
        {
            db.AvailabilitySettings.Add(new AvailabilitySettings
            {
                Id = 1,
                TimeZoneId = TimeZoneInfo.Local.Id,
                SlotIntervalMinutes = 30,
                MaxConcurrentAppointments = 1
            });
        }

        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            if (await db.BusinessHours.AnyAsync(hours =>
                    hours.DayOfWeek == day))
            {
                continue;
            }

            db.BusinessHours.Add(new BusinessHours
            {
                DayOfWeek = day,
                IsOpen = false
            });
        }

        await db.SaveChangesAsync();

        foreach (var roleName in Enum.GetNames<UserRole>())
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var roleResult = await roleManager.CreateAsync(
                new IdentityRole<int>(roleName));

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create role '{roleName}': " +
                    string.Join("; ", roleResult.Errors.Select(error => error.Description)));
            }
        }

        // =========================================================
        // PERMISSIONS
        // =========================================================

        var permissionData = new[]
        {
            ("appointments.view", "View appointments"),
            ("appointments.manage", "Manage appointments"),
            ("clients.records.view", "View authorized client records"),
            ("clients.records.edit", "Edit authorized client records"),
            ("clients.actionplans.manage", "Manage action plans for assigned clients"),
            ("services.manage", "Manage services"),
            ("schedules.manage", "Manage schedules"),
            ("images.manage", "Manage images"),
            ("payments.collect", "Record facility payments"),
            ("payments.manage", "Manage payments and refunds"),
            ("reports.view", "View reports"),
            ("audit.view", "View audit history"),
            ("users.manage", "Manage accounts"),
            ("permissions.manage", "Manage permissions"),
            ("privacy.manage", "Manage privacy requests"),
            ("system.manage", "Manage system configuration")
        };

        foreach (var item in permissionData)
        {
            var existingPermission =
                await db.Permissions
                    .FirstOrDefaultAsync(x =>
                        x.Key == item.Item1);

            if (existingPermission is null)
            {
                db.Permissions.Add(
                    new Permission
                    {
                        Key = item.Item1,
                        Name = item.Item2
                    });
            }
            else
            {
                existingPermission.Name = item.Item2;
            }
        }

        await db.SaveChangesAsync();

        // =========================================================
        // CLIENT
        // =========================================================

        var client = await EnsureDemoUserAsync(
            userManager,
            "client@demo.local",
            "Demo Client",
            "Client123!",
            UserRole.Client);

        // =========================================================
        // STAFF
        // =========================================================

       var existingDemoStaff = await userManager.FindByEmailAsync(
            "staff@demo.local");

        var isNewDemoStaff = existingDemoStaff is null;

        var staff = await EnsureDemoUserAsync(
            userManager,
            "staff@demo.local",
            "Demo Staff",
            "Staff123!",
            UserRole.Staff);

        // =========================================================
        // ADMIN
        // =========================================================

        var admin = await EnsureDemoUserAsync(
            userManager,
            "admin@demo.local",
            "Demo Administrator",
            "Admin123!",
            UserRole.Admin);

        var users = await userManager.Users.ToListAsync();
        foreach (var user in users)
        {
            await EnsureUserRoleAsync(userManager, user);
        }

        // =========================================================
        // STAFF PERMISSIONS
        // =========================================================

        // =========================================================
// DEFAULT PERMISSIONS FOR A NEW DEMO STAFF ACCOUNT
// Do not overwrite permissions managed by an Admin.
// =========================================================

var staffPermissionKeys = new[]
{
        "appointments.view",
        "appointments.manage",
        "clients.records.view",
        "clients.records.edit",
        "clients.actionplans.manage",
        "payments.collect"
};

if (isNewDemoStaff)
{
    foreach (var key in staffPermissionKeys)
    {
        var permission = await db.Permissions
            .FirstOrDefaultAsync(x => x.Key == key);

        if (permission is null)
        {
            continue;
        }

        db.UserPermissions.Add(new UserPermission
        {
            UserId = staff.Id,
            PermissionId = permission.Id
        });
    }

    await db.SaveChangesAsync();
}

        foreach (var key in staffPermissionKeys)
        {
            var permission =
                await db.Permissions
                    .FirstOrDefaultAsync(x =>
                        x.Key == key);

            if (permission is null)
                continue;

            var exists =
                await db.UserPermissions.AnyAsync(x =>
                    x.UserId == staff.Id &&
                    x.PermissionId == permission.Id);

            if (!exists)
            {
                db.UserPermissions.Add(
                    new UserPermission
                    {
                        UserId = staff.Id,
                        PermissionId = permission.Id
                    });
            }

        }

        // =========================================================
        // ADMIN PERMISSIONS
        // =========================================================

        var allPermissions =
            await db.Permissions.ToListAsync();

        foreach (var permission in allPermissions)
        {
            var exists =
                await db.UserPermissions.AnyAsync(x =>
                    x.UserId == admin.Id &&
                    x.PermissionId == permission.Id);

            if (!exists)
            {
                db.UserPermissions.Add(
                    new UserPermission
                    {
                        UserId = admin.Id,
                        PermissionId = permission.Id
                    });
            }
        }

        await db.SaveChangesAsync();

        // =========================================================
        // SERVICES
        // =========================================================

        if (!await db.Services.AnyAsync())
        {
            db.Services.AddRange(
                new ServiceItem
                {
                    Name = "Wellness Consultation",
                    Description =
                        "Initial wellness consultation.",
                    Price = 1500m,
                    DurationMinutes = 60,
                    Active = true
                },

                new ServiceItem
                {
                    Name = "Progress Review",
                    Description =
                        "Review goals and wellness progress.",
                    Price = 1000m,
                    DurationMinutes = 45,
                    Active = true
                },

                new ServiceItem
                {
                    Name = "Wellness Coaching",
                    Description =
                        "Guided wellness coaching session.",
                    Price = 1200m,
                    DurationMinutes = 60,
                    Active = true
                }
            );

            await db.SaveChangesAsync();
        }
    }

    private static async Task<AppUser> EnsureDemoUserAsync(
        UserManager<AppUser> userManager,
        string email,
        string fullName,
        string password,
        UserRole role)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new AppUser
            {
                Email = email,
                UserName = email,
                FullName = fullName,
                Role = role,
                EmailConfirmed = true,
                Active = true,
                LockoutEnabled = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(user, password);
            EnsureSucceeded(createResult, $"create demo user '{email}'");
        }
        else
        {
            user.UserName = email;
            user.Email = email;
            user.FullName = fullName;
            user.Role = role;
            user.EmailConfirmed = true;
            user.Active = true;
            user.LockoutEnabled = true;

            var updateResult = await userManager.UpdateAsync(user);
            EnsureSucceeded(updateResult, $"update demo user '{email}'");
        }

        return user;
    }

    private static async Task EnsureUserRoleAsync(
        UserManager<AppUser> userManager,
        AppUser user)
    {
        var currentRoles = await userManager.GetRolesAsync(user);
        var expectedRole = user.Role.ToString();
        var outdatedRoles = currentRoles
            .Where(currentRole => currentRole != expectedRole)
            .ToArray();

        if (outdatedRoles.Length > 0)
        {
            var removeResult =
                await userManager.RemoveFromRolesAsync(user, outdatedRoles);
            EnsureSucceeded(removeResult, $"update roles for user '{user.Id}'");
        }

        if (!currentRoles.Contains(expectedRole))
        {
            var addResult =
                await userManager.AddToRoleAsync(user, expectedRole);
            EnsureSucceeded(addResult, $"assign role to user '{user.Id}'");
        }
    }

    private static void EnsureSucceeded(
        IdentityResult result,
        string operation)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not {operation}: " +
                string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}
