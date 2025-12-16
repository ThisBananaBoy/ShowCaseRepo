using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;

namespace ProjectME_BE.Endpoints.Activity.Crud;

public class CreateActivityEndpoint : Endpoint<CreateActivityRequest, ActivityResponse>
{
    private readonly IUserContextService _userContext;
    private readonly ILogger<CreateActivityEndpoint> _logger;

    public CreateActivityEndpoint(
        IUserContextService userContext,
        ILogger<CreateActivityEndpoint> logger
    )
    {
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/activities");
        // Protected endpoint - requires authentication
    }

    public override async Task HandleAsync(CreateActivityRequest req, CancellationToken ct)
    {
        var userId = _userContext.GetUserId();
        if (!userId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // TODO: Implement Activity domain model and repository
        // var activity = new ActivityAggregate(userId.Value, ...);
        // _activityRepository.Add(activity);
        // await _activityRepository.SaveChangesAsync(ct);
        // var response = new ActivityResponse { ... };
        // await Send.OkAsync(response, ct);

        _logger.LogWarning("CreateActivityEndpoint called but Activity domain model not yet implemented");
        await Send.ErrorsAsync(501, ct); // Not Implemented
    }
}

public class CreateActivityRequest
{
    public Guid? ProjectId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? MilestoneId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
}
