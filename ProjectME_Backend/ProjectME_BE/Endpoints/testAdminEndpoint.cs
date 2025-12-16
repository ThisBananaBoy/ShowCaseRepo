using FastEndpoints;

namespace ProjectME_BE.Endpoints;

public class TestAdminEndpoint : EndpointWithoutRequest<string>
{
    public override void Configure()
    {
        Get("/test-admin");
        Roles("Admin");
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirst("sub")?.Value;
        var email = User.FindFirst("email")?.Value;

        return Send.OkAsync($"Hello {email}! Your ID: {userId}", ct);
    }
}
