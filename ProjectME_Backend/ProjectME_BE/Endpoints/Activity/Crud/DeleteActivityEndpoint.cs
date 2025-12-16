using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;

namespace ProjectME_BE.Endpoints.Activity.Crud;

public class DeleteActivityEndpoint : Endpoint<DeleteActivityRequest>
{
    private readonly IUserContextService _userContext;
    private readonly ILogger<DeleteActivityEndpoint> _logger;

    public DeleteActivityEndpoint(
        IUserContextService userContext,
        ILogger<DeleteActivityEndpoint> logger
    )
    {
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Delete("/activities/{id}");
        // Protected endpoint - requires authentication
    }

    public override async Task HandleAsync(DeleteActivityRequest req, CancellationToken ct)
    {
        var userId = _userContext.GetUserId();
        if (!userId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // TODO: Implement Activity domain model and repository
        // var activity = await _activityRepository.GetByIdAsync(req.Id, ct);
        // if (activity == null || activity.UserId != userId.Value) { await Send.NotFoundAsync(ct); return; }
        // _activityRepository.Delete(activity);
        // await _activityRepository.SaveChangesAsync(ct);
        // await Send.NoContentAsync(ct);

        _logger.LogWarning("DeleteActivityEndpoint called but Activity domain model not yet implemented");
        await Send.ErrorsAsync(501, ct); // Not Implemented
    }
}

public class DeleteActivityRequest
{
    public Guid Id { get; set; }
}
