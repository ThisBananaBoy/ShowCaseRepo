namespace Auth_Server.Services;

public interface IApplicationJwtService
{
    string GenerateApplicationToken(string applicationId, string applicationName);
}
