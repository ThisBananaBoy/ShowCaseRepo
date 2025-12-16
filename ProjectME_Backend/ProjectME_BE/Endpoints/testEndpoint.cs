using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using ProjectME_BE.Domain.Model.User;

namespace ProjectME_BE.Endpoints;

public class TestEndpoint : EndpointWithoutRequest<string>
{
    public override void Configure()
    {
        Get("/test");
        // AllowAnonymous(); <-- Entfernt = Bearer Token erforderlich!
       Roles("User");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync("Hello World", ct);
    }
}
