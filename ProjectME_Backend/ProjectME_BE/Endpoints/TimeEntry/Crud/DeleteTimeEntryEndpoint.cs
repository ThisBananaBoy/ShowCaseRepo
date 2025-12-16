using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;

namespace ProjectME_BE.Endpoints.TimeEntry.Crud;

public class DeleteTimeEntryEndpoint : Endpoint<DeleteTimeEntryRequest>
{
    private readonly IUserContextService _userContext;
    private readonly ILogger<DeleteTimeEntryEndpoint> _logger;

    public DeleteTimeEntryEndpoint(
        IUserContextService userContext,
        ILogger<DeleteTimeEntryEndpoint> logger
    )
    {
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Delete("/time-entries/{id}");
        // Protected endpoint - requires authentication
    }

    public override async Task HandleAsync(DeleteTimeEntryRequest req, CancellationToken ct)
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
        // _timeEntryRepository.Delete(timeEntry);
        // await _timeEntryRepository.SaveChangesAsync(ct);
        // await Send.NoContentAsync(ct);

        _logger.LogWarning("DeleteTimeEntryEndpoint called but TimeEntry domain model not yet implemented");
        await Send.ErrorsAsync(501, ct); // Not Implemented
    }
}

public class DeleteTimeEntryRequest
{
    public Guid Id { get; set; }
}
