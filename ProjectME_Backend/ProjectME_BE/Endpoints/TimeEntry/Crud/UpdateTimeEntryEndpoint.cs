using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;

namespace ProjectME_BE.Endpoints.TimeEntry.Crud;

public class UpdateTimeEntryEndpoint : Endpoint<UpdateTimeEntryRequest, TimeEntryResponse>
{
    private readonly IUserContextService _userContext;
    private readonly ILogger<UpdateTimeEntryEndpoint> _logger;

    public UpdateTimeEntryEndpoint(
        IUserContextService userContext,
        ILogger<UpdateTimeEntryEndpoint> logger
    )
    {
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Put("/time-entries/{id}");
        // Protected endpoint - requires authentication
    }

    public override async Task HandleAsync(UpdateTimeEntryRequest req, CancellationToken ct)
    {
        var userId = _userContext.GetUserId();
        if (!userId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // TODO: Implement TimeEntry domain model and repository
        // var timeEntry = await _timeEntryRepository.GetByIdAsync(req.Id, ct);
        // if (timeEntry == null || timeEntry.UserId != userId.Value) { await Send.NotFoundAsync(ct); return; }
        // // Update timeEntry properties
        // _timeEntryRepository.Update(timeEntry);
        // await _timeEntryRepository.SaveChangesAsync(ct);
        // var response = new TimeEntryResponse { ... };
        // await Send.OkAsync(response, ct);

        _logger.LogWarning("UpdateTimeEntryEndpoint called but TimeEntry domain model not yet implemented");
        await Send.ErrorsAsync(501, ct); // Not Implemented
    }
}

public class UpdateTimeEntryRequest
{
    public Guid Id { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? TaskId { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
}
