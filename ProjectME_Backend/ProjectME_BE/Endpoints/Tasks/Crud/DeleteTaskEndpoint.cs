using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;
using ProjectME_BE.Domain.Model.Task;
using ProjectME_BE.Domain.Primitives;

namespace ProjectME_BE.Endpoints.Tasks.Crud;

public class DeleteTaskEndpoint : Endpoint<DeleteTaskRequest>
{
    private readonly IRepository<TaskAggregate> _taskRepository;
    private readonly IUserContextService _userContext;
    private readonly ILogger<DeleteTaskEndpoint> _logger;

    public DeleteTaskEndpoint(
        IRepository<TaskAggregate> taskRepository,
        IUserContextService userContext,
        ILogger<DeleteTaskEndpoint> logger
    )
    {
        _taskRepository = taskRepository;
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Delete("/tasks/{id}");
        // Protected endpoint - requires authentication
    }

    public override async Task HandleAsync(DeleteTaskRequest req, CancellationToken ct)
    {
        var userId = _userContext.GetUserId();
        if (!userId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var task = await _taskRepository.GetByIdAsync(req.Id, ct);
        if (task == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (task.UserId != userId.Value)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            _taskRepository.Delete(task);
            await _taskRepository.SaveChangesAsync(ct);
            await Send.NoContentAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete task");
            await Send.ErrorsAsync(500, ct);
            return;
        }
    }
}

public class DeleteTaskRequest
{
    public Guid Id { get; set; }
}
