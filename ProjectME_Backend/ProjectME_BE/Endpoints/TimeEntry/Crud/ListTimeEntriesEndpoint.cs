using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;

namespace ProjectME_BE.Endpoints.TimeEntry.Crud;

public class ListTimeEntriesEndpoint : EndpointWithoutRequest<List<TimeEntryResponse>>
{
    private readonly IUserContextService _userContext;
    private readonly ILogger<ListTimeEntriesEndpoint> _logger;

    public ListTimeEntriesEndpoint(
        IUserContextService userContext,
        ILogger<ListTimeEntriesEndpoint> logger
    )
    {
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/time-entries");
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

        // TODO: Implement TimeEntry domain model and repository
        // var timeEntries = await _timeEntryRepository.GetWhere(te => te.UserId == userId.Value).ToListAsync(ct);
        // var response = timeEntries.Select(te => new TimeEntryResponse { ... }).ToList();
        // await Send.OkAsync(response, ct);

        _logger.LogWarning("ListTimeEntriesEndpoint called but TimeEntry domain model not yet implemented");
        await Send.OkAsync(new List<TimeEntryResponse>(), ct);
    }
}

public class TimeEntryResponse
{
    public Guid Id { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
