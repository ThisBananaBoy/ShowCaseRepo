using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.AuthService;
using ProjectME_BE.Domain.DTOs.Auth;
using ProjectME_BE.Domain.Model.User;
using ProjectME_BE.Domain.Primitives;

namespace ProjectME_BE.Endpoints.User;

public class RegisterEndpoint : Endpoint<RegisterRequest, RegisterResponse>
{
    private readonly IAuthService _authService;
    private readonly IRepository<UserAggregate> _userRepository;
    private readonly ILogger<RegisterEndpoint> _logger;

    public RegisterEndpoint(
        IAuthService authService,
        IRepository<UserAggregate> userRepository,
        ILogger<RegisterEndpoint> logger
    )
    {
        _authService = authService;
        _userRepository = userRepository;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/user/register");
        AllowAnonymous();
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        // Step 1: Register user in AuthServer
        RegisterResponse? authResponse;
        try
        {
            authResponse = await _authService.PostAsync<RegisterRequest, RegisterResponse>(
                "/auth/register",
                req,
                ct
            );
            if (authResponse == null)
            {
                _logger.LogWarning(
                    "Auth Server returned null response for registration: {Email}",
                    req.Email
                );
                await Send.ErrorsAsync(500, ct);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP error when registering user in AuthServer: {Email}. Status: {StatusCode}",
                req.Email,
                ex.Data.Contains("StatusCode") ? ex.Data["StatusCode"] : "Unknown"
            );
            await Send.ErrorsAsync(400, ct);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register user in AuthServer: {Email}", req.Email);
            await Send.ErrorsAsync(500, ct);
            return;
        }

        // Step 2: Create user in Backend (only if AuthServer registration succeeded)
        try
        {
            // Check if user already exists
            var existingUser = await _userRepository.GetFirstOrDefaultAsync(
                u => u.AuthUserId == authResponse.UserId,
                ct
            );

            if (existingUser != null)
            {
                _logger.LogWarning(
                    "User with AuthUserId {AuthUserId} already exists",
                    authResponse.UserId
                );
                await Send.ErrorsAsync(409, ct); // Conflict
                return;
            }

            // Create new user in backend
            var user = new UserAggregate(authResponse.UserId);
            _userRepository.Add(user);
            await _userRepository.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Successfully created user in backend: {UserId}, AuthUserId: {AuthUserId}",
                user.Id,
                authResponse.UserId
            );

            await Send.OkAsync(
                new RegisterResponse
                {
                    UserId = user.Id,
                    Email = authResponse.Email,
                    Message = "User registered successfully",
                },
                ct
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create user in backend after AuthServer registration. AuthUserId: {AuthUserId}. Attempting compensation.",
                authResponse.UserId
            );

            // Step 3: Compensation - Delete user from AuthServer if backend creation failed
            try
            {
                await _authService.DeleteAsync($"/auth/user/{authResponse.UserId}", ct);
                _logger.LogInformation(
                    "Compensation: Deleted user from AuthServer: {AuthUserId}",
                    authResponse.UserId
                );
            }
            catch (Exception compensationEx)
            {
                _logger.LogError(
                    compensationEx,
                    "CRITICAL: Failed to delete user from AuthServer during compensation. AuthUserId: {AuthUserId}. Manual cleanup required!",
                    authResponse.UserId
                );
            }

            await Send.ErrorsAsync(500, ct);
            return;
        }
    }
}
