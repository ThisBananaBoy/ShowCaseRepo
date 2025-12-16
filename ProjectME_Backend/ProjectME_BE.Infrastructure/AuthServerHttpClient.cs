using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectME_BE.Infrastructure;

public static class AuthServerHttpClient
{
    public static IServiceCollection AddAuthServerClient(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var baseUrl = configuration["AuthServer:BaseUrl"];
        var timeout = configuration.GetValue("AuthServer:Timeout", 30);
        var apiToken = configuration["AuthServer:ApiToken"];

        services.AddHttpClient(
            "AuthServer",
            client =>
            {
                client.BaseAddress = new Uri(
                    baseUrl
                        ?? throw new InvalidOperationException(
                            "AuthServer:BaseUrl is not configured"
                        )
                );
                client.Timeout = TimeSpan.FromSeconds(timeout);
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                // Add Bearer Token if configured (for backend-to-auth-server authentication)
                if (!string.IsNullOrWhiteSpace(apiToken))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken);
                }
            }
        );

        return services;
    }
}
