using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;

namespace ProjectME_BE.Endpoints.Activity.Crud;

public class UpdateActivityEndpoint : Endpoint<UpdateActivityRequest, ActivityResponse>
{
    private readonly IUserContextService _userContext;
    private readonly ILogger<UpdateActivityEndpoint> _logger;

    public UpdateActivityEndpoint(
        IUserContextService userContext,
        ILogger<UpdateActivityEndpoint> logger
    )
    {
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Put("/activities/{id}");
        // Protected endpoint - requires authentication
    }

    public override async Task HandleAsync(UpdateActivityRequest req, CancellationToken ct)
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
        // // Update activity properties
        // _activityRepository.Update(activity);
        // await _activityRepository.SaveChangesAsync(ct);
        // var response = new ActivityResponse { ... };
        // await Send.OkAsync(response, ct);

        _logger.LogWarning("UpdateActivityEndpoint called but Activity domain model not yet implemented");
        await Send.ErrorsAsync(501, ct); // Not Implemented
    }
}

public class UpdateActivityRequest
{
    public Guid Id { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? MilestoneId { get; set; }
    public string? ActivityType { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? Description { get; set; }
    public string? MetadataJson { get; set; }
}
