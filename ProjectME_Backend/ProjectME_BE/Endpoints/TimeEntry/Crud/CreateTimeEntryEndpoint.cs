using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;

namespace ProjectME_BE.Endpoints.TimeEntry.Crud;

public class CreateTimeEntryEndpoint : Endpoint<CreateTimeEntryRequest, TimeEntryResponse>
{
    private readonly IUserContextService _userContext;
    private readonly ILogger<CreateTimeEntryEndpoint> _logger;

    public CreateTimeEntryEndpoint(
        IUserContextService userContext,
        ILogger<CreateTimeEntryEndpoint> logger
    )
    {
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/time-entries");
        // Protected endpoint - requires authentication
    }

    public override async Task HandleAsync(CreateTimeEntryRequest req, CancellationToken ct)
    {
        var userId = _userContext.GetUserId();
        if (!userId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // TODO: Implement TimeEntry domain model and repository
        // var timeEntry = new TimeEntryAggregate(userId.Value, req.ProjectId, req.TaskId, req.StartTime, req.EndTime);
        // _timeEntryRepository.Add(timeEntry);
        // await _timeEntryRepository.SaveChangesAsync(ct);
        // var response = new TimeEntryResponse { ... };
        // await Send.OkAsync(response, ct);

        _logger.LogWarning("CreateTimeEntryEndpoint called but TimeEntry domain model not yet implemented");
        await Send.ErrorsAsync(501, ct); // Not Implemented
    }
}

public class CreateTimeEntryRequest
{
    public Guid? ProjectId { get; set; }
    public Guid? TaskId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
}
