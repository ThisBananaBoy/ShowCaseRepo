using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ProjectME_BE.Application.Services.UserContext;

public class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetUserId()
    {
        var userIdString = GetClaimValue("sub") ?? GetClaimValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return null;
        }
        return userId;
    }

    public string? GetUserEmail()
    {
        return GetClaimValue("email") ?? GetClaimValue(ClaimTypes.Email);
    }

    public List<string> GetUserRoles()
    {
        var roles = new List<string>();
        var roleClaim = GetClaimValue("role") ?? GetClaimValue(ClaimTypes.Role);
        if (!string.IsNullOrEmpty(roleClaim))
        {
            roles.Add(roleClaim);
        }

        // Check for "roles" claim (array)
        var rolesClaim = _httpContextAccessor.HttpContext?.User.FindAll("roles");
        if (rolesClaim != null)
        {
            roles.AddRange(rolesClaim.Select(c => c.Value));
        }

        return roles.Distinct().ToList();
    }

    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
    }

    private string? GetClaimValue(string claimType)
    {
        return _httpContextAccessor.HttpContext?.User.FindFirst(claimType)?.Value;
    }
}
