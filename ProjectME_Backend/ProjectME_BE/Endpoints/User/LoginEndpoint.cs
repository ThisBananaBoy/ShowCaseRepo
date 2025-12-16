using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.AuthService;
using ProjectME_BE.Domain.DTOs.Auth;

namespace ProjectME_BE.Endpoints.User;

public class LoginEndpoint : Endpoint<LoginRequest, LoginResponse>
{
    private readonly IAuthService _authService;
    private readonly ILogger<LoginEndpoint> _logger;

    public LoginEndpoint(
        IAuthService authService,
        ILogger<LoginEndpoint> logger
    )
    {
        _authService = authService;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/user/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        // Step 1: Authenticate with AuthServer
        LoginResponse? authResponse;
        try
        {
            authResponse = await _authService.PostAsync<LoginRequest, LoginResponse>(
                "/auth/login",
                req,
                ct
            );

            if (authResponse == null)
            {
                await Send.ErrorsAsync(401, ct); // Unauthorized
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate user with AuthServer: {Email}", req.Email);
            await Send.ErrorsAsync(401, ct); // Unauthorized
            return;
        }

        // Step 2: Get user from JWT token claims or ensure user exists in Backend
        // Note: AuthServer returns Token with user info, we need to extract UserId from token
        // For now, we'll need to get the user by email or handle it differently
        // Since LoginResponse doesn't have UserId, we need to get it from the token or create a lookup
        try
        {
            // TODO: Extract UserId from JWT token claims if needed
            // For now, we'll pass through the AuthServer response directly
            // The frontend will use the token for subsequent requests

            await Send.OkAsync(authResponse, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process login response. Email: {Email}",
                req.Email
            );
            await Send.ErrorsAsync(500, ct);
            return;
        }
    }
}

