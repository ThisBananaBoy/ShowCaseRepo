using Auth_Server.Data;
using Microsoft.EntityFrameworkCore;

namespace Auth_Server.Extensions;

public static class MigrationExtension
{
    public static async Task ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
