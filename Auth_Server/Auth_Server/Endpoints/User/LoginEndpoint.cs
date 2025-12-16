using Auth_Server.Model;
using Auth_Server.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;

namespace Auth_Server.Endpoints.User;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
}

public class LoginEndpoint(
    UserManager<UserAggregate> userManager,
    IJwtService jwtService,
    IConfiguration configuration
) : Endpoint<LoginRequest, LoginResponse>
{
    public override void Configure()
    {
        Post("/auth/login");
        Policies("ApplicationToken");
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        // 1. User finden
        var user = await userManager.FindByEmailAsync(req.Email);
        if (user == null)
        {
            ThrowError("Invalid email or password", 401);
        }

        // 2. Email bestätigt?
        // if (!user!.EmailConfirmed)
        // {
        //     ThrowError("Please confirm your email before logging in", 403);
        // }

        // 3. Password prüfen
        var isPasswordValid = await userManager.CheckPasswordAsync(user, req.Password);
        if (!isPasswordValid)
        {
            ThrowError("Invalid email or password", 401);
        }

        // 4. Rollen abrufen
        var roles = await userManager.GetRolesAsync(user);

        // 5. JWT Token generieren
        var token = jwtService.GenerateToken(user.Id.ToString(), user.Email!, roles);

        var expiresInMinutes = int.Parse(configuration["Jwt:User:ExpiresInMinutes"]!);

        await SendOkAsync(
            new LoginResponse
            {
                Token = token,
                Email = user.Email!,
                Roles = roles.ToList(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            },
            ct
        );
    }
}
