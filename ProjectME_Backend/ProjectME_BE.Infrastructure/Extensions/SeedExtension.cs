using Microsoft.Extensions.DependencyInjection;
using ProjectME_BE.Infrastructure.Seeding;

namespace ProjectME_BE.Infrastructure.Extensions;

public static class SeedExtension
{
    /// <summary>
    /// Seeds roles and admin user. Call this after ApplyMigrations().
    /// Creates a scope internally to resolve scoped services (UserManager, RoleManager).
    /// </summary>
    public static async Task SeedDatabaseAsync(this IServiceProvider services)
    {
        // Scope erstellen für scoped services (UserManager, RoleManager, DbContext)
        using var scope = services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        
        await seeder.SeedAsync();
    }
}

