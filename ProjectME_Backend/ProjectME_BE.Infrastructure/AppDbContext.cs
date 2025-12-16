using Microsoft.EntityFrameworkCore;
using ProjectME_BE.Domain.Common;

namespace ProjectME_BE.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations from current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global configurations
        // foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        // {
        //     // Configure BaseEntity properties
        //     if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
        //     {
        //         modelBuilder
        //             .Entity(entityType.ClrType)
        //             .Property("CreatedAt")
        //             .HasDefaultValueSql("now()");
        //     }
        // }
    }
}
