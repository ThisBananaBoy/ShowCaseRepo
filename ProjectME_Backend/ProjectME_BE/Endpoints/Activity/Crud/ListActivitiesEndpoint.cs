using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;

namespace ProjectME_BE.Endpoints.Activity.Crud;

public class ListActivitiesEndpoint : EndpointWithoutRequest<List<ActivityResponse>>
{
    private readonly IUserContextService _userContext;
    private readonly ILogger<ListActivitiesEndpoint> _logger;

    public ListActivitiesEndpoint(
        IUserContextService userContext,
        ILogger<ListActivitiesEndpoint> logger
    )
    {
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/activities");
        // Protected endpoint - requires authentication
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = _userContext.GetUserId();
        if (!userId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // TODO: Implement Activity domain model and repository
        // var activities = await _activityRepository.GetWhere(a => a.UserId == userId.Value).ToListAsync(ct);
        // var response = activities.Select(a => new ActivityResponse { ... }).ToList();
        // await Send.OkAsync(response, ct);

        _logger.LogWarning("ListActivitiesEndpoint called but Activity domain model not yet implemented");
        await Send.OkAsync(new List<ActivityResponse>(), ct);
    }
}

public class ActivityResponse
{
    public Guid Id { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? MilestoneId { get; set; }
    public Guid UserId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
