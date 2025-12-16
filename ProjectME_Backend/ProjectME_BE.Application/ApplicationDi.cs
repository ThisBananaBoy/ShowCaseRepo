using Microsoft.Extensions.DependencyInjection;
using ProjectME_BE.Application.Services;
using ProjectME_BE.Application.Services.AuthService;
using ProjectME_BE.Application.Services.UserContext;

namespace ProjectME_BE.Application;

public static class ApplicationDi
{
    public static IServiceCollection AddApplicationDi(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<NoOpEmailSender>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserContextService, UserContextService>();
        return services;
    }
}
