using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;
using ProjectME_BE.Domain.Model.RecurringTask;
using ProjectME_BE.Domain.Primitives;

namespace ProjectME_BE.Endpoints.RecurringTask.Crud;

public class DeleteRecurringTaskEndpoint : Endpoint<DeleteRecurringTaskRequest>
{
    private readonly IRepository<RecurringTaskAggregate> _recurringTaskRepository;
    private readonly IUserContextService _userContext;
    private readonly ILogger<DeleteRecurringTaskEndpoint> _logger;

    public DeleteRecurringTaskEndpoint(
        IRepository<RecurringTaskAggregate> recurringTaskRepository,
        IUserContextService userContext,
        ILogger<DeleteRecurringTaskEndpoint> logger
    )
    {
        _recurringTaskRepository = recurringTaskRepository;
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Delete("/recurring-tasks/{id}");
        // Protected endpoint - requires authentication
    }

    public override async Task HandleAsync(DeleteRecurringTaskRequest req, CancellationToken ct)
    {
        var userId = _userContext.GetUserId();
        if (!userId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var recurringTask = await _recurringTaskRepository.GetByIdAsync(req.Id, ct);
        if (recurringTask == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (recurringTask.UserId != userId.Value)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            _recurringTaskRepository.Delete(recurringTask);
            await _recurringTaskRepository.SaveChangesAsync(ct);
            await Send.NoContentAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete recurring task");
            await Send.ErrorsAsync(500, ct);
            return;
        }
    }
}

public class DeleteRecurringTaskRequest
{
    public Guid Id { get; set; }
}
