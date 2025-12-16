using System.Text.Json;
using Auth_Server.Model;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;

namespace Auth_Server.Endpoints.User;

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class RegisterEndpoint(UserManager<UserAggregate> userManager)
    : Endpoint<RegisterRequest, RegisterResponse>
{
    public override void Configure()
    {
        Post("/auth/register");
        Policies("ApplicationToken");
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {

        Console.WriteLine("RegisterEndpoint" + JsonSerializer.Serialize(req));
        // 1. Email-Validierung (zusätzlich zur automatischen Identity-Validierung)
        if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
        {
            ThrowError("Invalid email format");
        }

        // 2. Password-Validierung (Identity prüft automatisch Länge/Komplexität aus Program.cs)
        if (string.IsNullOrWhiteSpace(req.Password))
        {
            ThrowError("Password is required");
        }

        // 3. User erstellen
        var user = new UserAggregate { UserName = req.Email, Email = req.Email };

        var result = await userManager.CreateAsync(user, req.Password);

        if (!result.Succeeded)
        {
            // Identity-Validierungsfehler zurückgeben
            foreach (var error in result.Errors)
            {
                ThrowError(error.Description);
            }
        }

        // 4. Default-Rolle "User" zuweisen
        await userManager.AddToRoleAsync(user, "User");

        // 5. Email-Bestätigung senden
        var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink =
            $"http://localhost:5173/confirmEmail?userId={user.Id}&code={Uri.EscapeDataString(code)}";
        // await emailSender.SendConfirmationLinkAsync(user, req.Email, confirmationLink);

        await SendOkAsync(
            new RegisterResponse
            {
                UserId = user.Id,
                Email = req.Email,
                Message = "User registered successfully. Please confirm your email.",
            },
            cancellation: ct
        );
    }
}
