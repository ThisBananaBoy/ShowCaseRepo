namespace ProjectME_BE.Application.Services.UserContext;

public interface IUserContextService
{
    Guid? GetUserId();
    string? GetUserEmail();
    List<string> GetUserRoles();
    bool IsAuthenticated();
}
