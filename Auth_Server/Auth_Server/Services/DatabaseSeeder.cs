using Auth_Server.Model;
using Microsoft.AspNetCore.Identity;

namespace Auth_Server.Services;

public class DatabaseSeeder(
    UserManager<UserAggregate> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    ILogger<DatabaseSeeder> logger
) : IDatabaseSeeder
{
    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedAdminUserAsync();
    }

    private async Task SeedRolesAsync()
    {
        string[] roles = { "Admin", "User", "PaidUser" };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                if (result.Succeeded)
                {
                    logger.LogInformation("✅ Role '{RoleName}' created", roleName);
                }
                else
                {
                    logger.LogError(
                        "❌ Failed to create role '{RoleName}': {Errors}",
                        roleName,
                        string.Join(", ", result.Errors.Select(e => e.Description))
                    );
                }
            }
        }
    }

    private async Task SeedAdminUserAsync()
    {
        var adminEmail = "admin@projectme.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new UserAggregate
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation("✅ Admin user created: {Email} / Admin123!", adminEmail);
            }
            else
            {
                logger.LogError(
                    "❌ Failed to create admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description))
                );
            }
        }
    }
}
