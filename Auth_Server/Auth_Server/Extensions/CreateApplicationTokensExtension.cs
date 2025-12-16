using Auth_Server.Services;

namespace Auth_Server.Extensions;

public static class CreateApplicationTokensExtension
{
    public static void CreateApplicationTokens(this WebApplication app)
    {
        var scope = app.Services.CreateScope();
        var applicationTokenService =
            scope.ServiceProvider.GetRequiredService<IApplicationJwtService>();
        var projectME_Token = applicationTokenService.GenerateApplicationToken(
            "projectme",
            "ProjectME"
        );
        app.Logger.LogInformation("ProjectME Token: {ProjectME_Token}", projectME_Token);
    }
}
