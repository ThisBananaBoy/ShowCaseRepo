using Auth_Server.Data;
using Auth_Server.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Auth_Server.Extensions;

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
        var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();

        await seeder.SeedAsync();
    }
}
