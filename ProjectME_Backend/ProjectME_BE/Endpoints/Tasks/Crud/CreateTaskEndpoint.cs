using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;
using ProjectME_BE.Domain.Model.Task;
using ProjectME_BE.Domain.Primitives;

namespace ProjectME_BE.Endpoints.Tasks.Crud;

public class CreateTaskEndpoint : Endpoint<CreateTaskRequest, TaskResponse>
{
    private readonly IRepository<TaskAggregate> _taskRepository;
    private readonly IUserContextService _userContext;
    private readonly ILogger<CreateTaskEndpoint> _logger;

    public CreateTaskEndpoint(
        IRepository<TaskAggregate> taskRepository,
        IUserContextService userContext,
        ILogger<CreateTaskEndpoint> logger
    )
    {
        _taskRepository = taskRepository;
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/tasks");
        // Protected endpoint - requires authentication
    }

    public override async Task HandleAsync(CreateTaskRequest req, CancellationToken ct)
    {
        var userId = _userContext.GetUserId();
        if (!userId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            var task = new TaskAggregate(
                userId.Value,
                req.ProjectId,
                req.MilestoneId,
                req.AssignedUserId,
                req.Name,
                req.Status,
                req.Priority,
                req.StartTime,
                req.EndTime,
                req.DueDate,
                req.CompletedAt
            );

            _taskRepository.Add(task);
            await _taskRepository.SaveChangesAsync(ct);

            var response = new TaskResponse
            {
                Id = task.Id,
                UserId = task.UserId,
                ProjectId = task.ProjectId,
                MilestoneId = task.MilestoneId,
                AssignedUserId = task.AssignedUserId,
                Name = task.Name,
                Status = task.Status,
                Priority = task.Priority,
                StartTime = task.StartTime,
                EndTime = task.EndTime,
                DueDate = task.DueDate,
                CompletedAt = task.CompletedAt
            };

            await Send.OkAsync(response, ct);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid task data");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create task");
            await Send.ErrorsAsync(500, ct);
            return;
        }
    }
}

public class CreateTaskRequest
{
    public Guid? ProjectId { get; set; }
    public Guid? MilestoneId { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Status { get; set; }
    public int? Priority { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
