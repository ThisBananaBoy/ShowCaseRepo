using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectME_BE.Domain.Common;
using ProjectME_BE.Domain.Primitives;
using ProjectME_BE.Infrastructure.Common;
using ProjectME_BE.Infrastructure.Seeding;

namespace ProjectME_BE.Infrastructure;

public static class InfraDi
{
    public static IServiceCollection AddInfraDi(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Add DbContext with PostgreSQL
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                postgresOptions =>
                {
                    postgresOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                }
            )
        );

        // Auth Server HTTP Client
        services.AddAuthServerClient(configuration);
        
        // Database Seeder 
        services.AddScoped<DatabaseSeeder>();


        var entityTypes = typeof(BaseEntity)
            .Assembly.GetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false } && typeof(BaseEntity).IsAssignableFrom(t)
            );

        foreach (var entityType in entityTypes)
        {
            var repositoryInterface = typeof(IRepository<>).MakeGenericType(entityType);
            var repositoryImplementation = typeof(BaseRepository<>).MakeGenericType(entityType);
            services.AddScoped(repositoryInterface, repositoryImplementation);
        }

        return services;
    }
}
